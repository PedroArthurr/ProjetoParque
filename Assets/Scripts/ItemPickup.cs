using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemType Type;
    public int Amount = 1;
    public float Range = 1.2f;
    [SerializeField] string playerTag = "Player";
    [SerializeField] float checkInterval = 0.08f;

    Transform player;
    float t;

    void Awake()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;
    }

    void Update()
    {
        t -= Time.deltaTime;
        if (t > 0f) return;
        t = checkInterval;

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
            if (!player) return;
        }

        if ((player.position - transform.position).sqrMagnitude <= Range * Range)
            PickupManager.Instance?.TryPickup(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}

public enum ItemType { Plastic, Paper, Glass, Organic, Metal }
