using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class PickupManager : MonoBehaviour
{
    public static PickupManager Instance { get; private set; }

    [SerializeField] Camera cam;
    [SerializeField] Transform player;
    [SerializeField] string playerTag = "Player";
    [SerializeField] LayerMask itemLayer = ~0;
    [SerializeField] float clickPickRadius = 0.25f;
    [SerializeField] bool blockWhenPointerOverUI = true;
    [SerializeField] bool debugLogs = true;

    public event Action<ItemType, int> OnPicked;
    public event Action<string> OnMessage;

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        if (!cam) cam = Camera.main;
        if (!player)
        {
            var p = GameObject.FindWithTag(playerTag);
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (blockWhenPointerOverUI && EventSystem.current && EventSystem.current.IsPointerOverGameObject())
        {
            if (debugLogs) Debug.Log("[Pickup] Blocked by UI");
            return;
        }

        Vector2 mw = MouseWorld();
        var hits = Physics2D.OverlapPointAll(mw, itemLayer);
        if (hits.Length == 0) hits = Physics2D.OverlapCircleAll(mw, clickPickRadius, itemLayer);

        ItemPickup best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            var ip = col.GetComponent<ItemPickup>() ?? col.GetComponentInParent<ItemPickup>();
            float dist = player ? Vector2.Distance(player.position, col.transform.position) : 0f;
            if (debugLogs)
            {
                int layer = col.gameObject.layer;
                Debug.Log($"[Pickup] Hit '{col.name}' layer={LayerMask.LayerToName(layer)} hasItem={(ip != null)} dist={dist:F2}");
            }
            if (!ip) continue;
            if (dist < bestDist) { best = ip; bestDist = dist; }
        }

        if (!best)
        {
            if (debugLogs) Debug.Log("[Pickup] No ItemPickup under cursor");
            return;
        }

        TryPickup(best);
    }

    public bool TryPickup(ItemPickup item)
    {
        if (!player || !item) return false;

        float d = Vector2.Distance(player.position, item.transform.position);
        if (d > item.Range)
        {
            OnMessage?.Invoke("Too far");
            if (debugLogs) Debug.Log($"[Pickup] Too far (d={d:F2} > {item.Range:F2})");
            return false;
        }

        bool ok = InventoryManager.Instance && InventoryManager.Instance.AddItem(item.Type, item.Amount);
        if (!ok)
        {
            OnMessage?.Invoke("Cannot pickup");
            if (debugLogs) Debug.Log("[Pickup] Inventory refused");
            return false;
        }

        if (debugLogs) Debug.Log($"[Pickup] Picked up {item.Amount} of {item.Type}");
        OnPicked?.Invoke(item.Type, item.Amount);
        Destroy(item.gameObject);
        return true;
    }

    Vector2 MouseWorld()
    {
        Vector3 m = Input.mousePosition; m.z = 10f;
        return cam ? (Vector2)cam.ScreenToWorldPoint(m) : (Vector2)m;
    }
}
