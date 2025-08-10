using UnityEngine;
using System;

public class PickupManager : MonoBehaviour
{
    public static PickupManager Instance { get; private set; }

    [SerializeField] Transform player;
    [SerializeField] string playerTag = "Player";

    public event Action<ItemType, int> OnPicked;
    public event Action<string> OnMessage;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        if (!player)
        {
            var p = GameObject.FindWithTag(playerTag);
            if (p) player = p.transform;
        }
    }

    public bool TryPickup(ItemPickup item)
    {
        if (!player || !item) return false;
        float d = Vector2.Distance(player.position, item.transform.position);
        if (d > item.Range) { OnMessage?.Invoke("Too far"); return false; }

        bool ok = InventoryManager.Instance && InventoryManager.Instance.AddItem(item.Type, item.Amount);
        if (!ok) { OnMessage?.Invoke("Cannot pickup"); return false; }

        OnPicked?.Invoke(item.Type, item.Amount);
        Destroy(item.gameObject);
        return true;
    }
}
