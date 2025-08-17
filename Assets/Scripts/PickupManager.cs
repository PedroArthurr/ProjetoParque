using UnityEngine;
using System;
using System.Collections;

public class PickupManager : MonoBehaviour
{
    public static PickupManager Instance { get; private set; }

    [SerializeField] Transform player;
    [SerializeField] string playerTag = "Player";
    [SerializeField] UICollectTargetRegistry uiRegistry;
    [SerializeField] float binsAutoHideDelay = 2f;

    public event Action<ItemType, int> OnPicked;
    public event Action<string> OnMessage;

    Transform playerHand;
    int flyInProgress;
    float keepVisibleUntil;
    Coroutine visibilityCo;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (!player)
        {
            var p = GameObject.FindWithTag(playerTag);
            if (p) player = p.transform;
        }
        if (!uiRegistry) uiRegistry = FindObjectOfType<UICollectTargetRegistry>(true);
        playerHand = player ? player.Find("HoldItemPoint") : null;
    }

    public bool TryPickup(ItemPickup item)
    {
        if (!player || !item) return false;

        float d = Vector2.Distance(player.position, item.transform.position);
        if (d > item.Range) { OnMessage?.Invoke("Too far"); return false; }

        bool ok = InventoryManager.Instance && InventoryManager.Instance.AddItem(item.Type, item.Amount);
        if (!ok) { OnMessage?.Invoke("Cannot pickup"); return false; }

        Sprite icon = null;
        var sr = item.GetComponentInChildren<SpriteRenderer>();
        if (sr) icon = sr.sprite;

        ShowBinsHold();
        StartFly(icon, item.transform.position, item.Type);

        OnPicked?.Invoke(item.Type, item.Amount);
        Destroy(item.gameObject);
        return true;
    }

    void ShowBinsHold()
    {
        var a = uiRegistry ? uiRegistry.binsAnimator : null;
        if (!a) return;
        a.ResetTrigger("Hide");
        a.SetTrigger("Show");
        keepVisibleUntil = Mathf.Max(keepVisibleUntil, Time.time + binsAutoHideDelay);
        EnsureVisibilityLoop();
    }

    void StartFly(Sprite icon, Vector3 worldStart, ItemType type)
    {
        if (!uiRegistry || !uiRegistry.canvas) return;
        var fly = UIFlyToSlotMB.Instance ?? FindObjectOfType<UIFlyToSlotMB>(true);
        if (!fly) return;

        var slot = uiRegistry.GetTarget(type);
        if (!slot) return;

        var bez = uiRegistry ? uiRegistry.GetAuthor(type) : null;

        flyInProgress++;

        fly.Fly(icon, worldStart, playerHand, slot, uiRegistry.canvas,
                0.4f, 0.3f, 1.50f, .4f, 0.2f,
                onArrived: () =>
                {
                    flyInProgress = Mathf.Max(0, flyInProgress - 1);
                    keepVisibleUntil = Time.time + binsAutoHideDelay;
                    EnsureVisibilityLoop();
                },
                bezier: bez,
                spinDeg: 240f,
                strictBezier: true);
    }

    void EnsureVisibilityLoop()
    {
        if (visibilityCo == null) visibilityCo = StartCoroutine(VisibilityLoop());
    }

    IEnumerator VisibilityLoop()
    {
        while (Time.time < keepVisibleUntil || flyInProgress > 0) yield return null;
        var a = uiRegistry ? uiRegistry.binsAnimator : null;
        if (a)
        {
            a.ResetTrigger("Show");
            a.SetTrigger("Hide");
        }
        visibilityCo = null;
    }
}
