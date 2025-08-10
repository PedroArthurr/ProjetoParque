using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] InventoryManager inventory;
    [SerializeField] TextMeshProUGUI plasticText;
    [SerializeField] TextMeshProUGUI metalText;
    [SerializeField] TextMeshProUGUI glassText;
    [SerializeField] TextMeshProUGUI paperText;
    [SerializeField] TextMeshProUGUI organicText;

    void OnEnable()
    {
        if (!inventory) inventory = InventoryManager.Instance;
        if (inventory) inventory.OnItemCountChanged += OnItemCountChanged;
        RefreshAll();
    }

    void OnDisable()
    {
        if (inventory) inventory.OnItemCountChanged -= OnItemCountChanged;
    }

    void OnItemCountChanged(ItemType type, int count)
    {
        switch (type)
        {
            case ItemType.Plastic: Set(plasticText, "Plástico", count); break;
            case ItemType.Metal: Set(metalText, "Metal", count); break;
            case ItemType.Glass: Set(glassText, "Vidro", count); break;
            case ItemType.Paper: Set(paperText, "Papel", count); break;
            case ItemType.Organic: Set(organicText, "Orgânico", count); break;
        }
    }

    void RefreshAll()
    {
        if (!inventory) return;
        Set(plasticText, "Plástico", inventory.GetCount(ItemType.Plastic));
        Set(metalText, "Metal", inventory.GetCount(ItemType.Metal));
        Set(glassText, "Vidro", inventory.GetCount(ItemType.Glass));
        Set(paperText, "Papel", inventory.GetCount(ItemType.Paper));
        Set(organicText, "Orgânico", inventory.GetCount(ItemType.Organic));
    }

    void Set(TextMeshProUGUI t, string label, int count)
    {
        if (t) t.text = $"{label}: {count:00}";
    }
}
