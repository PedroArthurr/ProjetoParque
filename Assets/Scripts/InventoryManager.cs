using UnityEngine;
using System;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    readonly Dictionary<ItemType, int> counts = new Dictionary<ItemType, int>();
    int total;

    public event Action<ItemType, int> OnItemCountChanged;
    public event Action<int> OnTotalChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool AddItem(ItemType type, int amount = 1)
    {
        if (amount <= 0) return false;
        if (!counts.ContainsKey(type)) counts[type] = 0;
        counts[type] += amount;
        total += amount;
        OnItemCountChanged?.Invoke(type, counts[type]);
        OnTotalChanged?.Invoke(total);
        return true;
    }

    public bool Has(ItemType type, int amount) => GetCount(type) >= amount;

    public bool TryConsume(ItemType type, int amount)
    {
        if (amount <= 0) return false;
        if (!Has(type, amount)) return false;
        counts[type] -= amount;
        total -= amount;
        OnItemCountChanged?.Invoke(type, counts[type]);
        OnTotalChanged?.Invoke(total);
        return true;
    }

    public bool TryConsumeBulk(ItemCost[] costs)
    {
        if (costs == null) return false;
        for (int i = 0; i < costs.Length; i++)
            if (!Has(costs[i].type, costs[i].amount)) return false;

        for (int i = 0; i < costs.Length; i++)
            TryConsume(costs[i].type, costs[i].amount);

        return true;
    }

    public int GetCount(ItemType type) => counts.TryGetValue(type, out var v) ? v : 0;
    public int GetTotal() => total;
}
