using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public class ParallaxAutoLayer : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 multiplier = new Vector2(0.5f, 0.3f);
    [SerializeField] float tileWidthOverride = 0f;
    [SerializeField] bool loopX = true;
    [SerializeField] bool autoRebaseOnBigJump = true;
    [SerializeField] float jumpThreshold = 3f;

    [SerializeField] Transform sourceTile;
    [SerializeField] Transform leftT;
    [SerializeField] Transform rightT;

    float tileW;
    Vector3 basePos, targetStart, lastTargetPos;
    bool building;

    void OnEnable()
    {
        if (!target) target = Camera.main ? Camera.main.transform : null;
        EnsureReferences();
        Rebase();
        LateUpdate();
    }

    void OnValidate()
    {
        if (!Application.isPlaying) { EnsureReferences(); Rebase(); }
    }

    void OnTransformChildrenChanged()
    {
        if (!building) EnsureReferences();
    }

    void EnsureReferences()
    {
        if (building) return;
        building = true;

        if (!sourceTile || sourceTile.GetComponent<ParallaxAutoClone>())
            sourceTile = FindFirstNonCloneChild();

        FindExistingClones(out leftT, out rightT);

        if (!leftT) leftT = CreateClone(0, "_L");
        if (!rightT) rightT = CreateClone(1, "_R");

        ComputeTileWidth();
        SnapClones();

        CleanupExtrasClones();

        building = false;
    }

    Transform FindFirstNonCloneChild()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var ch = transform.GetChild(i);
            if (!ch.GetComponent<ParallaxAutoClone>()) return ch;
        }
        return null;
    }

    void FindExistingClones(out Transform l, out Transform r)
    {
        l = r = null;
        var clones = GetComponentsInChildren<ParallaxAutoClone>(true);
        for (int i = 0; i < clones.Length; i++)
        {
            var c = clones[i];
            if (c.transform.parent != transform) continue;
            if (c.index == 0 && l == null) l = c.transform;
            else if (c.index == 1 && r == null) r = c.transform;
        }
    }

    Transform CreateClone(int index, string suffix)
    {
        if (!sourceTile) return null;
        var t = Instantiate(sourceTile, transform);
        t.name = sourceTile.name + suffix;
        var mk = t.GetComponent<ParallaxAutoClone>();
        if (!mk) mk = t.gameObject.AddComponent<ParallaxAutoClone>();
        mk.index = index;
        var nested = t.GetComponent<ParallaxAutoLayer>();
        if (nested) DestroyImmediate(nested);
        return t;
    }

    void ComputeTileWidth()
    {
        if (tileWidthOverride > 0f) { tileW = tileWidthOverride; return; }
        var sr = sourceTile ? sourceTile.GetComponent<SpriteRenderer>() : null;
        tileW = (sr ? sr.bounds.size.x : 10f);
    }

    void SnapClones()
    {
        if (!sourceTile) return;
        var p = sourceTile.localPosition;
        if (leftT) leftT.localPosition = p + Vector3.left * tileW;
        if (rightT) rightT.localPosition = p + Vector3.right * tileW;
        RenameSafe();
    }

    void RenameSafe()
    {
        if (sourceTile) sourceTile.name = TrimSuffix(sourceTile.name);
        if (leftT) leftT.name = TrimSuffix(sourceTile.name) + "_L";
        if (rightT) rightT.name = TrimSuffix(sourceTile.name) + "_R";
    }

    string TrimSuffix(string n)
    {
        if (string.IsNullOrEmpty(n)) return "Mid";
        if (n.EndsWith("_L") || n.EndsWith("_R")) return n.Substring(0, n.Length - 2);
        return n;
    }

    void CleanupExtrasClones()
    {
        var clones = GetComponentsInChildren<ParallaxAutoClone>(true);
        for (int i = 0; i < clones.Length; i++)
        {
            var t = clones[i].transform;
            if (t.parent != transform) continue;
            if (t != leftT && t != rightT) SafeDestroy(t.gameObject);
        }
    }

    void SafeDestroy(GameObject go)
    {
        if (!go) return;
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    public void Rebase()
    {
        basePos = transform.position;
        targetStart = target ? target.position : Vector3.zero;
        lastTargetPos = targetStart;

        if (sourceTile)
        {
            var p = sourceTile.position;
            if (leftT) leftT.position = new Vector3(p.x - tileW, p.y, p.z);
            if (rightT) rightT.position = new Vector3(p.x + tileW, p.y, p.z);
        }
    }

    void LateUpdate()
    {
        if (!target || !sourceTile) return;

        if (autoRebaseOnBigJump && (target.position - lastTargetPos).sqrMagnitude > jumpThreshold * jumpThreshold)
            Rebase();

        var d = target.position - targetStart;
        var pos = basePos + new Vector3(d.x * multiplier.x, d.y * multiplier.y, 0f);
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        if (loopX) DoLoopX();

        lastTargetPos = target.position;
    }

    void DoLoopX()
    {
        if (!leftT || !sourceTile || !rightT) return;

        Transform a = leftT, b = sourceTile, c = rightT;
        if (a.position.x > b.position.x) { var t = a; a = b; b = t; }
        if (b.position.x > c.position.x) { var t = b; b = c; c = t; }
        if (a.position.x > b.position.x) { var t = a; a = b; b = t; }
        leftT = a; sourceTile = b; rightT = c;

        float camX = target.position.x, midX = sourceTile.position.x;

        if (camX - midX > tileW * 0.5f)
        {
            leftT.position = new Vector3(rightT.position.x + tileW, leftT.position.y, leftT.position.z);
            var t = leftT; leftT = sourceTile; sourceTile = rightT; rightT = t;
        }
        else if (midX - camX > tileW * 0.5f)
        {
            rightT.position = new Vector3(leftT.position.x - tileW, rightT.position.y, rightT.position.z);
            var t = rightT; rightT = sourceTile; sourceTile = leftT; leftT = t;
        }
    }

    [ContextMenu("Force Rebuild")]
    void ForceRebuild()
    {
        building = false;
        EnsureReferences();
        Rebase();
    }

    public void SetTarget(Transform t) { target = t; Rebase(); }
    public void SetMultiplier(Vector2 m) { multiplier = m; }
}
