using UnityEngine;
using UnityEngine.UI;

public class ConcretoBoss : MonoBehaviour
{
    public enum BossState { Idle, Attack }

    [Header("Refs")]
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform player;
    [SerializeField] Animator excavatorAnimator;
    [SerializeField] Animator concretoAnimator;
    [SerializeField] Image hpFill;
    [SerializeField] Transform throwPoint;
    [SerializeField] LayerMask groundMask;

    [Header("Health")]
    [SerializeField] int maxHP = 200;
    [SerializeField] float furiousThreshold = 0.35f;

    [Header("Cycle")]
    [SerializeField] float idleBetweenAttacksMin = 1.0f;
    [SerializeField] float idleBetweenAttacksMax = 1.8f;
    [SerializeField] float preOrderLeadTime = 3.0f;
    [SerializeField] float attackDuration = 0.35f;
    [SerializeField] int volleysPerAttack = 1;

    [Header("Debris Throw (Ballistic)")]
    [SerializeField] GameObject debrisPrefab;
    [SerializeField] int debrisPerVolley = 1;
    [SerializeField] float debrisSpeedMin = 8f;
    [SerializeField] float debrisSpeedMax = 12f;
    [SerializeField] float flightTimeMin = 0.55f;
    [SerializeField] float flightTimeMax = 1.10f;
    [SerializeField] float aimNoiseAngle = 6f;
    [SerializeField] float debrisLife = 6f;

    [Header("On Hit Ground -> Spawn")]
    [SerializeField] GameObject standardEnemyPrefab;
    [SerializeField] float spawnChance = 0.35f;
    [SerializeField] public Vector2 spawnOffset = new Vector2(0f, 0.1f);

    [Header("On Hit Player -> Stun")]
    [SerializeField] float playerStunTime = 5f;
    [SerializeField] Vector2 playerKnockback = new Vector2(6f, 8f);

    [Header("Anim Triggers")]
    [SerializeField] string EXC_Idle = "Idle";
    [SerializeField] string EXC_Attack = "Attack";
    [SerializeField] string CNC_Idle = "Idle";
    [SerializeField] string CNC_Talking = "Talking";
    [SerializeField] string CNC_Order = "Order";
    [SerializeField] string CNC_Hurt = "Hurt";
    [SerializeField] string CNC_Furious = "Furious";
    [SerializeField] string CNC_Bored = "Bored";

    [Header("Debug")]
    [SerializeField] bool debugLogs = false;

    public BossState State { get; private set; } = BossState.Idle;
    public int HP { get; private set; }

    float stateTimer;
    float attackTimer;
    bool dead;
    bool furious;
    bool orderFired;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!throwPoint)
        {
            var t = transform.Find("ThrowPoint");
            if (t) throwPoint = t;
        }
        HP = maxHP;
        UpdateHPUI();
        EnterIdle();
    }

    void Update()
    {
        if (dead) return;

        if (State == BossState.Idle)
        {
            stateTimer -= Time.deltaTime;
            if (!orderFired && stateTimer <= Mathf.Max(0.1f, preOrderLeadTime))
            {
                orderFired = true;
                PlayConcreto(CNC_Order);
            }
            if (stateTimer <= 0f) EnterAttack();
        }
        else
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) EnterIdle();
        }
    }

    void EnterIdle()
    {
        State = BossState.Idle;
        stateTimer = Random.Range(idleBetweenAttacksMin, idleBetweenAttacksMax) * (furious ? 0.8f : 1f);
        orderFired = false;
        PlayExcavator(EXC_Idle);
        PlayConcreto(Random.value < 0.2f ? CNC_Bored : CNC_Idle);
        if (debugLogs) Debug.Log("[Boss] Idle");
    }

    void EnterAttack()
    {
        State = BossState.Attack;
        attackTimer = attackDuration;
        int volleys = Mathf.Max(1, volleysPerAttack + (furious ? 1 : 0));
        PlayExcavator(EXC_Attack);
        PlayConcreto(CNC_Talking);
        for (int i = 0; i < volleys; i++) SpawnVolley();
        if (debugLogs) Debug.Log("[Boss] Attack");
    }

    void SpawnVolley()
    {
        for (int i = 0; i < Mathf.Max(1, debrisPerVolley); i++) SpawnDebris();
    }

    Vector2 ComputeBallisticV0(Vector2 p0, Vector2 p1, float T, float g)
    {
        float dx = p1.x - p0.x;
        float dy = p1.y - p0.y;
        float vx = dx / T;
        float vy = (dy + 0.5f * g * T * T) / T;
        return new Vector2(vx, vy);
    }

    Vector2 Rotate(Vector2 v, float degrees)
    {
        float a = degrees * Mathf.Deg2Rad;
        float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        return new Vector2(v.x * ca - v.y * sa, v.x * sa + v.y * ca);
    }

    void SpawnDebris()
    {
        if (!debrisPrefab || !throwPoint || !player) return;

        var go = Instantiate(debrisPrefab, throwPoint.position, Quaternion.identity);
        var rb2 = go.GetComponent<Rigidbody2D>(); if (!rb2) rb2 = go.AddComponent<Rigidbody2D>();
        if (rb2.gravityScale <= 0f) rb2.gravityScale = 1f;

        var dropCol = go.GetComponent<Collider2D>();
        var bossCols = GetComponentsInChildren<Collider2D>(true);
        if (dropCol != null && bossCols != null)
            for (int i = 0; i < bossCols.Length; i++) Physics2D.IgnoreCollision(dropCol, bossCols[i], true);

        Vector2 p0 = throwPoint.position;
        Vector2 p1 = player.position;
        float g = Mathf.Abs(Physics2D.gravity.y) * rb2.gravityScale;

        float T = Mathf.Clamp(Random.Range(flightTimeMin, flightTimeMax), 0.25f, 3f);
        Vector2 v0 = ComputeBallisticV0(p0, p1, T, g);

        int it = 0;
        while ((v0.magnitude < debrisSpeedMin || v0.magnitude > debrisSpeedMax) && it++ < 8)
        {
            T = v0.magnitude < debrisSpeedMin ? T * 0.88f : T * 1.12f;
            v0 = ComputeBallisticV0(p0, p1, T, g);
        }

        if (aimNoiseAngle > 0f) v0 = Rotate(v0, Random.Range(-aimNoiseAngle, aimNoiseAngle));
        if (v0.y < 2f) v0.y = 2f;
        if (v0.magnitude < debrisSpeedMin) v0 = v0.normalized * debrisSpeedMin;

        rb2.linearVelocity = v0;

        var d = go.GetComponent<ConcretoDebris>(); if (!d) d = go.AddComponent<ConcretoDebris>();
        d.Init(this, groundMask, debrisLife, spawnChance, standardEnemyPrefab, playerStunTime, playerKnockback, debugLogs);
    }


    public void ReceiveDamage(int amount)
    {
        if (dead) return;
        HP = Mathf.Max(0, HP - Mathf.Abs(amount));
        UpdateHPUI();
        PlayConcreto(CNC_Hurt);
        if (!furious && (float)HP / maxHP <= furiousThreshold)
        {
            furious = true;
            PlayConcreto(CNC_Furious);
        }
        if (HP <= 0) Die();
    }

    void Die()
    {
        dead = true;
        if (rb) rb.simulated = false;
        var cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
        PlayConcreto(CNC_Bored);
        PlayExcavator(EXC_Idle);
        if (debugLogs) Debug.Log("[Boss] Dead");
    }

    void UpdateHPUI()
    {
        if (hpFill) hpFill.fillAmount = Mathf.Clamp01((float)HP / maxHP);
    }

    void PlayExcavator(string trigger)
    {
        if (!excavatorAnimator || string.IsNullOrEmpty(trigger)) return;
        excavatorAnimator.ResetTrigger(trigger);
        excavatorAnimator.SetTrigger(trigger);
    }

    void PlayConcreto(string trigger)
    {
        if (!concretoAnimator || string.IsNullOrEmpty(trigger)) return;
        concretoAnimator.ResetTrigger(trigger);
        concretoAnimator.SetTrigger(trigger);
    }
    void OnDrawGizmosSelected()
    {
        if (!throwPoint) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(throwPoint.position, 0.06f);
        if (player)
        {
            Vector2 p0 = throwPoint.position;
            Vector2 p1 = player.position;
            float g = -Physics2D.gravity.y;
            float T = Mathf.Lerp(flightTimeMin, flightTimeMax, 0.5f);
            Vector2 v0 = ComputeBallisticV0(p0, p1, T, g);
            v0 = Rotate(v0, aimNoiseAngle * 0.5f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(throwPoint.position, (Vector2)throwPoint.position + v0 * 0.15f);
        }
    }
}
