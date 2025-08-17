using UnityEngine;
public class ConcretoDebris : MonoBehaviour
{
    ConcretoBoss owner;
    LayerMask groundMask;
    float life;
    float spawnChance;
    GameObject spawnPrefab;
    float stunTime;
    Vector2 knockback;
    bool debug;
    bool done;
    Rigidbody2D rb;
    float t;

    public void Init(ConcretoBoss owner, LayerMask groundMask, float life, float spawnChance, GameObject spawnPrefab, float stunTime, Vector2 knockback, bool debug)
    {
        this.owner = owner;
        this.groundMask = groundMask;
        this.life = life;
        this.spawnChance = spawnChance;
        this.spawnPrefab = spawnPrefab;
        this.stunTime = stunTime;
        this.knockback = knockback;
        this.debug = debug;
    }

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= life) Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (done) return;

        if (c.collider.CompareTag("Player"))
        {
            var prb = c.collider.attachedRigidbody;
            var stun = prb ? prb.GetComponent<PlayerStun>() ?? prb.GetComponentInParent<PlayerStun>() : null;
            if (stun && stun.CanBeStunned) stun.ApplyStun(stunTime, knockback);
            if (debug) Debug.Log("[Debris] Hit Player -> Stun");
            done = true;
            Destroy(gameObject);
            return;
        }

        if (((1 << c.collider.gameObject.layer) & groundMask.value) != 0)
        {
            if (Random.value <= spawnChance && spawnPrefab)
            {
                Vector2 p2 = c.GetContact(0).point + owner.spawnOffset;
                Instantiate(spawnPrefab, (Vector3)p2, Quaternion.identity);
                if (debug) Debug.Log("[Debris] Hit Ground -> Spawn enemy");
            }
            done = true;
            Destroy(gameObject);
        }
    }
}