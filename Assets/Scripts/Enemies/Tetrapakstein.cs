using UnityEngine;

public class Tetrapakstein : StandardEnemy
{
    public enum State { Idle, Chase, Attack, Cooldown, Stunned }

    [Header("Chase/Attack")]
    [SerializeField] float chaseSpeed = 3.2f;
    [SerializeField] float detectRadius = 6f;
    [SerializeField] float attackRange = 1.4f;
    [SerializeField] float verticalAttackTolerance = 0.7f;
    [SerializeField] float attackCooldown = 0.6f;
    [SerializeField] Collider2D attackHitbox;

    [Header("Attack Timing")]
    [SerializeField] bool useEvents = false;
    [SerializeField] float windupTime = 0.25f;
    [SerializeField] float activeTime = 0.18f;
    [SerializeField] float recoverTime = 0.32f;

    [Header("Anim States")]
    [SerializeField] string idleState = "Idle";
    [SerializeField] string runState = "Run";
    [SerializeField] string attackState = "Attack";
    [SerializeField] string stunnedState = "Stunned";
    [SerializeField] int layer = 0;
    [SerializeField] float crossFade = 0.05f;

    [Header("Detection Box")]
    [SerializeField] float detectionForward = -1f;

    [Header("Attack Origin / Masks")]
    [SerializeField] Transform attackOrigin;
    [SerializeField] LayerMask playerMask;
    [SerializeField] bool requireFront = true;
    [SerializeField] bool requireCooldownOK = true;

    [Header("Facing & Lock")]
    [SerializeField] bool lockFacingDuringAttack = true;
    [SerializeField] bool freezeBodyDuringAttack = true;

    public bool AttackActive => attackActive;
    public bool AttackInProgress => attackInProgress;
    public bool UseEvents => useEvents;

    State state = State.Idle;
    float cdT, phaseT;
    bool hitThisSwing, attackActive, attackInProgress, wasStunned;
    string playing;

    Vector2 dbgBoxCenter, dbgBoxSize;
    bool dbgInFront, dbgOverlap, dbgCanAttack;
    string dbgWhy;

    Vector2 baseColOffset;
    Vector3 baseHitboxLocalPos;
    Vector3 baseOriginLocal;
    bool cachedColOffset, cachedHitboxPos, cachedOrigin;

    int attackFacingDir = 1;
    RigidbodyConstraints2D cachedConstraints;
    bool constraintsCached;

    protected override void Update()
    {
        base.Update();

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            if (!player) return;
        }

        if (!attackHitbox)
        {
            var childs = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < childs.Length; i++)
            {
                if (childs[i].name.ToLower().Contains("attackhitbox"))
                {
                    attackHitbox = childs[i];
                    attackHitbox.isTrigger = true;
                    break;
                }
            }
        }
        if (attackHitbox && !cachedColOffset) { baseColOffset = attackHitbox.offset; cachedColOffset = true; }
        if (attackHitbox && !cachedHitboxPos) { baseHitboxLocalPos = attackHitbox.transform.localPosition; cachedHitboxPos = true; }
        if (attackOrigin && !cachedOrigin) { baseOriginLocal = attackOrigin.localPosition; cachedOrigin = true; }

        Vector3 origin = attackOrigin ? attackOrigin.position : transform.position;

        float dx = player.position.x - origin.x;
        float dist = Vector2.Distance(player.position, origin);

        if (!(state == State.Attack && lockFacingDuringAttack))
            direction = dx >= 0f ? 1 : -1;

        UpdateFacing();

        if (IsStunned)
        {
            if (!wasStunned) CancelAttack();
            SetState(State.Stunned);
            wasStunned = true;
            return;
        }
        else if (wasStunned)
        {
            wasStunned = false;
            cdT = 0f;
            SetState(dist <= detectRadius ? State.Chase : State.Idle);
        }

        switch (state)
        {
            case State.Idle:
                if (dist <= detectRadius) SetState(State.Chase);
                break;

            case State.Chase:
                {
                    if (dist > detectRadius * 1.2f) { SetState(State.Idle); break; }
                    dbgCanAttack = ComputeCanAttack(origin, out dbgInFront, out dbgOverlap, out dbgBoxCenter, out dbgBoxSize, out dbgWhy);
                    if (dbgCanAttack && !attackInProgress && cdT <= 0f) BeginAttack();
                    break;
                }

            case State.Attack:
                if (!useEvents)
                {
                    phaseT -= Time.deltaTime;
                    float tTotal = windupTime + activeTime + recoverTime;
                    float tRem = Mathf.Clamp(phaseT, 0f, tTotal);

                    bool shouldBeActive = tRem <= (activeTime + recoverTime) && tRem > recoverTime;
                    if (shouldBeActive != attackActive)
                    {
                        attackActive = shouldBeActive;
                        if (attackHitbox) attackHitbox.enabled = attackActive;
                    }
                    if (tRem <= 0f) Anim_AttackEnd();
                }
                break;

            case State.Cooldown:
                {
                    if (dist > detectRadius * 1.2f) { SetState(State.Idle); break; }
                    cdT -= Time.deltaTime;
                    if (cdT <= 0f)
                    {
                        bool canAtk = ComputeCanAttack(origin, out dbgInFront, out dbgOverlap, out dbgBoxCenter, out dbgBoxSize, out dbgWhy);
                        if (canAtk && !attackInProgress) BeginAttack();
                        else SetState(State.Chase);
                    }
                    break;
                }
        }
    }

    protected override void FixedUpdate()
    {
        if (IsStunned) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }

        switch (state)
        {
            case State.Chase:
            case State.Cooldown:
                rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);
                break;
            case State.Attack:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            default:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
        }
    }

    void UpdateFacing()
    {
        int currDir = (state == State.Attack && lockFacingDuringAttack) ? attackFacingDir : direction;

        if (spriteRenderer) spriteRenderer.flipX = currDir > 0;

        if (attackHitbox)
        {
            if (cachedColOffset)
            {
                var off = baseColOffset;
                off.x = Mathf.Abs(off.x) * (currDir >= 0 ? 1 : -1);
                attackHitbox.offset = off;
            }
            if (cachedHitboxPos)
            {
                var lp = baseHitboxLocalPos;
                lp.x = Mathf.Abs(lp.x) * (currDir >= 0 ? 1 : -1);
                attackHitbox.transform.localPosition = lp;
            }
        }

        if (attackOrigin && cachedOrigin)
        {
            var lp = baseOriginLocal;
            lp.x = Mathf.Abs(lp.x) * (currDir >= 0 ? 1 : -1);
            attackOrigin.localPosition = lp;
        }
    }

    void SetState(State s)
    {
        if (state == s) return;
        state = s;

        switch (s)
        {
            case State.Idle: PlayOnce(idleState); break;
            case State.Chase: PlayOnce(runState); break;
            case State.Attack: PlayOnce_ForceRestart(attackState); break;
            case State.Cooldown: PlayOnce(runState); break;
            case State.Stunned: PlayOnce(stunnedState); break;
        }
    }

    bool ComputeCanAttack(Vector3 origin, out bool inFront, out bool overlap, out Vector2 boxCenter, out Vector2 boxSize, out string why)
    {
        float dir = (direction >= 0) ? 1f : -1f;
        Vector2 baseCenter = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        float forward = (detectionForward >= 0f) ? detectionForward : (attackRange * 0.5f);

        boxCenter = baseCenter + new Vector2(dir * forward, 0f);
        boxSize = new Vector2(attackRange, verticalAttackTolerance * 2f);

        inFront = ((player.position.x - baseCenter.x) * dir) > 0f;

        overlap = false;
        var hit = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerMask);
        if (hit && (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player"))) overlap = true;

        bool cdOk = !requireCooldownOK || cdT <= 0f;
        bool ok = (!requireFront || inFront) && overlap && cdOk;

        if (!ok)
        {
            if (requireFront && !inFront) why = "not_in_front";
            else if (!overlap) why = "player_outside_box";
            else if (!cdOk) why = "cooldown";
            else why = "unknown";
        }
        else why = "ok";

        return ok;
    }

    void BeginAttack()
    {
        attackInProgress = true;
        attackActive = false;
        hitThisSwing = false;
        if (attackHitbox) attackHitbox.enabled = false;

        attackFacingDir = direction;

        if (freezeBodyDuringAttack)
        {
            if (!constraintsCached) { cachedConstraints = rb.constraints; constraintsCached = true; }
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        PlayOnce_ForceRestart(attackState);

        if (!useEvents) phaseT = windupTime + activeTime + recoverTime;
    }

    void CancelAttack()
    {
        attackInProgress = false;
        attackActive = false;
        hitThisSwing = false;
        if (attackHitbox) attackHitbox.enabled = false;
        if (freezeBodyDuringAttack && constraintsCached) rb.constraints = cachedConstraints;
    }

    public void TryHitPlayer(Collider2D other)
    {
        if (!attackActive || hitThisSwing) return;
        if (!other || !other.CompareTag("Player")) return;

        var stun = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<PlayerStun>() : other.GetComponentInParent<PlayerStun>();
        if (!stun || !stun.CanBeStunned) return;

        int dirSign = (player.position.x - transform.position.x) >= 0f ? 1 : -1;
        if (dirSign != attackFacingDir) return;

        stun.ApplyStun(playerStunTime, new Vector2(attackFacingDir * 8f, 6f));
        hitThisSwing = true;
    }

    public void Anim_AttackHitOn()
    {
        if (!useEvents || !attackInProgress) return;
        attackActive = true;
        if (attackHitbox) attackHitbox.enabled = true;
    }

    public void Anim_AttackHitOff()
    {
        if (!useEvents) return;
        attackActive = false;
        if (attackHitbox) attackHitbox.enabled = false;
    }

    public void Anim_AttackEnd()
    {
        attackActive = false;
        attackInProgress = false;
        if (attackHitbox) attackHitbox.enabled = false;
        cdT = attackCooldown;
        SetState(State.Cooldown);
        hitThisSwing = false;
        if (freezeBodyDuringAttack && constraintsCached) rb.constraints = cachedConstraints;
    }

    void PlayOnce(string stateName)
    {
        if (!animator || string.IsNullOrEmpty(stateName)) return;
        if (playing == stateName) return;
        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(layer, hash)) animator.CrossFade(hash, crossFade, layer, 0f);
        playing = stateName;
    }

    void PlayOnce_ForceRestart(string stateName)
    {
        if (!animator || string.IsNullOrEmpty(stateName)) return;
        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(layer, hash)) animator.Play(hash, layer, 0f);
        playing = stateName;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.collider.CompareTag("Player")) ResolveStompOnly(c);
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (c.collider.CompareTag("Player")) ResolveStompOnly(c);
    }

    void ResolveStompOnly(Collision2D c)
    {
        if (!mainCollider) return;
        var prb = c.collider.attachedRigidbody;
        if (!prb) return;

        float vy = prb.linearVelocity.y;
        float enemyTop = mainCollider.bounds.max.y;
        float playerBottom = c.collider.bounds.min.y;

        Vector2 n = Vector2.zero;
        for (int i = 0; i < c.contactCount; i++) n += c.GetContact(i).normal;
        if (c.contactCount > 0) n /= c.contactCount;

        bool above = playerBottom >= enemyTop - topHitMargin;
        bool falling = vy <= StompYThreshold;
        bool topSurface = n.y > 0.25f;

        if (above && falling && topSurface) ApplyStun(prb);
    }

    void OnDrawGizmos()
    {
        if (!debugGizmos) return;

        Vector2 baseCenter = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        float dir = (state == State.Attack && lockFacingDuringAttack)
                    ? (attackFacingDir >= 0 ? 1f : -1f)
                    : (direction >= 0 ? 1f : -1f);
        float forward = (detectionForward >= 0f) ? detectionForward : (attackRange * 0.5f);

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(baseCenter, detectRadius);

        Vector2 center = baseCenter + new Vector2(dir * forward, 0f);
        Vector2 size = new Vector2(attackRange, verticalAttackTolerance * 2f);

        Gizmos.color = (Application.isPlaying && dbgCanAttack) ? new Color(0.2f, 1f, 0.2f, 0.6f) : new Color(1f, 0.1f, 0.1f, 0.45f);
        Vector3 a = new Vector3(center.x + size.x * 0.5f, center.y + size.y * 0.5f);
        Vector3 b = new Vector3(center.x + size.x * 0.5f, center.y - size.y * 0.5f);
        Vector3 c = new Vector3(center.x - size.x * 0.5f, center.y - size.y * 0.5f);
        Vector3 d = new Vector3(center.x - size.x * 0.5f, center.y + size.y * 0.5f);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(baseCenter, baseCenter + Vector2.right * dir * 0.7f);
        Gizmos.DrawSphere(baseCenter + Vector2.right * dir * 0.7f, 0.04f);
    }
}
