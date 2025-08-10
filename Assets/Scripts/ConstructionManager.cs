using UnityEngine;

public class ConstructionManager : MonoBehaviour
{
    public static ConstructionManager Instance { get; private set; }

    [SerializeField] Camera cam;
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] InventoryManager inventory;
    [SerializeField] LayerMask buildSpotLayer;
    [SerializeField] BuildRecipe[] recipes;

    [SerializeField] Color validColor = new Color(0f, 1f, 0f, 0.6f);
    [SerializeField] Color onSpotNoCostColor = new Color(1f, 0.92f, 0.16f, 0.6f);
    [SerializeField] Color invalidColor = new Color(1f, 0f, 0f, 0.6f);

    [SerializeField] float spotPickRadius = 0.2f;
    [SerializeField] bool debugLogs = true;

    BuildRecipe current;
    GameObject previewGO;
    SpriteRenderer previewSR;
    bool building;
    float nextLayerLog;

    void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
        if (!cam) cam = Camera.main;
        if (!inventory) inventory = InventoryManager.Instance;
    }

    void Update()
    {
        if (!building || current == null) return;

        var spot = GetSpotUnderMouse();
        bool canAfford = CanAfford(current);
        bool valid = spot && !spot.IsOccupied && canAfford;

        UpdatePreview(spot ? spot.Position : MouseWorld(), valid, spot && !spot.IsOccupied, canAfford);

        if (debugLogs && Time.time >= nextLayerLog)
        {
            LogMouseLayers();
            nextLayerLog = Time.time + 0.25f;
        }

        if (Input.GetMouseButtonDown(0) && valid)
        {
            if (inventory.TryConsumeBulk(current.costs))
            {
                spot.Place(current.prefab);
                EndBuild();
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) CancelBuild();
    }

    public void BeginBuildByIndex(int index)
    {
        if (index < 0 || index >= (recipes?.Length ?? 0)) return;
        BeginBuild(recipes[index]);
    }

    public void BeginBuild(BuildRecipe recipe)
    {
        if (!recipe || !recipe.prefab) return;
        current = recipe;
        building = true;
        if (playerMovement) playerMovement.BlockMovement();
        CreatePreviewFrom(recipe.prefab);
        if (debugLogs) Debug.Log($"[Build] Begin {recipe.name}");
    }

    public void CancelBuild() => EndBuild();

    void EndBuild()
    {
        if (debugLogs) Debug.Log("[Build] End");
        building = false;
        current = null;
        if (playerMovement) playerMovement.UnblockMovement();
        if (previewGO) Destroy(previewGO);
        previewGO = null;
        previewSR = null;
    }

    BuildSpot GetSpotUnderMouse()
    {
        Vector2 p = MouseWorld();
        var hits = Physics2D.OverlapCircleAll(p, spotPickRadius, buildSpotLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            var s = hits[i].GetComponentInParent<BuildSpot>();
            if (s)
            {
                if (debugLogs) Debug.Log($"[Build] Spot hit: {hits[i].name} (occupied={s.IsOccupied})");
                return s;
            }
        }
        return null;
    }

    Vector2 MouseWorld()
    {
        Vector3 m = Input.mousePosition; m.z = 10f;
        return cam ? (Vector2)cam.ScreenToWorldPoint(m) : (Vector2)m;
    }

    bool CanAfford(BuildRecipe r)
    {
        if (!inventory || r.costs == null) return false;
        for (int i = 0; i < r.costs.Length; i++)
            if (!inventory.Has(r.costs[i].type, r.costs[i].amount)) return false;
        return true;
    }

    void CreatePreviewFrom(GameObject prefab)
    {
        if (previewGO) Destroy(previewGO);
        previewGO = new GameObject("BuildPreview");
        previewSR = previewGO.AddComponent<SpriteRenderer>();

        var src = prefab.GetComponentInChildren<SpriteRenderer>(true);
        if (src)
        {
            previewSR.sprite = src.sprite;
            previewSR.sortingLayerID = src.sortingLayerID;
            previewSR.sortingOrder = src.sortingOrder + 1;
            previewGO.transform.rotation = src.transform.rotation;
            previewGO.transform.localScale = src.transform.lossyScale;
        }
        else
        {
            previewGO.transform.localScale = prefab.transform.localScale;
        }

        previewSR.color = invalidColor;
    }

    void UpdatePreview(Vector3 pos, bool valid, bool onSpotAndFree, bool canAfford)
    {
        if (!previewGO) return;
        previewGO.transform.position = pos;

        if (valid)
        {
            previewSR.color = validColor;
            if (debugLogs) Debug.Log("[Build] VALID: on spot and can afford");
        }
        else if (onSpotAndFree && !canAfford)
        {
            previewSR.color = onSpotNoCostColor;
            if (debugLogs) Debug.Log("[Build] On spot, but not enough materials");
        }
        else
        {
            previewSR.color = invalidColor;
            if (debugLogs) Debug.Log("[Build] INVALID: no spot or occupied");
        }
    }

    void LogMouseLayers()
    {
        Vector2 p = MouseWorld();
        var all = Physics2D.OverlapPointAll(p);
        if (all.Length == 0)
        {
            Debug.Log("[Build] Mouse over: nothing");
            return;
        }

        for (int i = 0; i < all.Length; i++)
        {
            var col = all[i];
            int layer = col.gameObject.layer;
            string lname = LayerMask.LayerToName(layer);
            bool inMask = (buildSpotLayer.value & (1 << layer)) != 0;
            bool hasSpot = col.GetComponentInParent<BuildSpot>() != null;
            Debug.Log($"[Build] Under mouse -> {col.name} | Layer {layer}:{lname} | InBuildSpotMask={inMask} | HasBuildSpot={hasSpot}");
        }
    }
}
