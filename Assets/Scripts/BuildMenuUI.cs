using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class BuildMenuUI : MonoBehaviour
{
    [SerializeField] InventoryManager inventory;
    [SerializeField] ConstructionManager construction;
    [SerializeField] BuildRecipe[] recipes;
    [SerializeField] Button buttonPrefab;
    [SerializeField] Transform listRoot;
    [SerializeField] GameObject panel;
    [SerializeField] KeyCode toggleKey = KeyCode.B;

    readonly List<Button> created = new List<Button>();
    readonly Dictionary<Button, BuildRecipe> map = new Dictionary<Button, BuildRecipe>();

    void Awake()
    {
        if (!inventory) inventory = InventoryManager.Instance;
        if (!construction) construction = ConstructionManager.Instance;
        if (panel) panel.SetActive(false);
        Rebuild();
    }

    void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnItemCountChanged += OnInventoryChanged;
            inventory.OnTotalChanged += _ => OnInventoryChanged(default, 0);
        }
    }

    void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnItemCountChanged -= OnInventoryChanged;
            inventory.OnTotalChanged -= _ => OnInventoryChanged(default, 0);
        }
    }

    void Update()
    {
        if (panel && Input.GetKeyDown(toggleKey))
        {
            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf) RefreshInteractable();
        }
    }

    void OnInventoryChanged(ItemType _, int __) => RefreshInteractable();

    void Rebuild()
    {
        ClearButtons();
        if (!listRoot || !buttonPrefab || recipes == null) return;

        for (int i = 0; i < recipes.Length; i++)
        {
            var r = recipes[i];
            if (!r || !r.prefab) continue;

            var btn = Instantiate(buttonPrefab, listRoot);
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = RecipeLabel(r);

            btn.onClick.AddListener(() =>
            {
                if (CanAfford(r))
                {
                    construction.BeginBuild(r);
                    if (panel) panel.SetActive(false);
                }
            });

            created.Add(btn);
            map[btn] = r;
        }

        RefreshInteractable();
    }

    void RefreshInteractable()
    {
        for (int i = 0; i < created.Count; i++)
        {
            var btn = created[i];
            if (!btn || !map.ContainsKey(btn)) continue;
            bool ok = CanAfford(map[btn]);
            btn.gameObject.SetActive(ok);
        }
    }

    bool CanAfford(BuildRecipe r)
    {
        if (!r || r.costs == null || inventory == null) return false;
        for (int i = 0; i < r.costs.Length; i++)
            if (inventory.GetCount(r.costs[i].type) < r.costs[i].amount) return false;
        return true;
    }

    string RecipeLabel(BuildRecipe r)
    {
        var sb = new StringBuilder();
        sb.Append(string.IsNullOrEmpty(r.id) ? r.name : r.id);
        if (r.costs != null && r.costs.Length > 0)
        {
            sb.Append("  (");
            for (int i = 0; i < r.costs.Length; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(r.costs[i].type).Append(": ").Append(r.costs[i].amount);
            }
            sb.Append(")");
        }
        return sb.ToString();
    }

    void ClearButtons()
    {
        for (int i = 0; i < created.Count; i++)
            if (created[i]) Destroy(created[i].gameObject);
        created.Clear();
        map.Clear();
    }
}
