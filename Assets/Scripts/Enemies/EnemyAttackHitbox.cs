using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    [SerializeField] Tetrapakstein owner;
    [SerializeField] string playerTag = "Player";

    Collider2D col;

    void Awake()
    {
        if (!owner) owner = GetComponentInParent<Tetrapakstein>();
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        int selfLayer = gameObject.layer;
        int pLayer = LayerMask.NameToLayer(playerTag);
        bool ignored = (pLayer >= 0) && Physics2D.GetIgnoreLayerCollision(selfLayer, pLayer);
        Debug.Log($"[Hitbox] layer={LayerMask.LayerToName(selfLayer)}({selfLayer}) vs {playerTag}({pLayer}) ignored={ignored}");
        if (!owner) Debug.LogError("[Hitbox] Missing owner (Tetrapakstein)");
        if (!col) Debug.LogError("[Hitbox] Missing Collider2D");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!owner) return;
        owner.TryHitPlayer(other);

        if (other.CompareTag(playerTag))
            Debug.Log($"[Hitbox] ENTER {other.name} active={owner.AttackActive} inProgress={owner.AttackInProgress} useEvents={owner.UseEvents}");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!owner) return;
        owner.TryHitPlayer(other);

        if (other.CompareTag(playerTag))
            Debug.Log($"[Hitbox] STAY {other.name} active={owner.AttackActive} inProgress={owner.AttackInProgress}");
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<Collider2D>();
        if (!c) return;

        bool active = owner && owner.AttackActive;
        Gizmos.color = active ? new Color(0.2f, 1f, 0.8f, 0.7f) : new Color(1f, 0.2f, 0.2f, 0.4f);

        var b = c.bounds;
        Vector3 p1 = new Vector3(b.min.x, b.min.y, 0f);
        Vector3 p2 = new Vector3(b.max.x, b.min.y, 0f);
        Vector3 p3 = new Vector3(b.max.x, b.max.y, 0f);
        Vector3 p4 = new Vector3(b.min.x, b.max.y, 0f);
        Gizmos.DrawLine(p1, p2); Gizmos.DrawLine(p2, p3); Gizmos.DrawLine(p3, p4); Gizmos.DrawLine(p4, p1);
    }
}
