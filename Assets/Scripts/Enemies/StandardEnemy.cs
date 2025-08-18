using UnityEngine;

public class StandardEnemy : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] public Rigidbody2D rb;
    [SerializeField] public Animator animator;
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] public Transform player;
    [SerializeField] public Collider2D mainCollider;

    [Header("Layers")]
    [SerializeField] public LayerMask groundMask;
    [SerializeField] public LayerMask wallMask;

    [Header("Patrol")]
    [SerializeField] public float moveSpeed = 2.5f;
    [SerializeField] public int direction = -1;
    [SerializeField] public float edgeCheckDistance = 0.28f;
    [SerializeField] public float wallCheckDistance = 0.14f;
    [SerializeField] public float runThreshold = 0.05f;

    [Header("Stomp")]
    [SerializeField] public float stompYThreshold = -2f;
    public float StompYThreshold => stompYThreshold;
    [SerializeField] public float topHitMargin = 0.06f;
    [SerializeField] public float stunDuration = 1.2f;
    [SerializeField] public float bounceForce = 10f;

    [Header("Player Hurt")]
    [SerializeField] public float playerStunTime = 0.8f;
    [SerializeField] public float postHitCooldown = 0.35f;
    [SerializeField] public float postHitImpulseX = 3.5f;

    [Header("Flip Control")]
    [SerializeField] public float flipCooldown = 0.3f;
    [SerializeField] public float minRelSpeedToFlip = 0.2f;
    [SerializeField] public float stuckFlipTime = 0.6f;
    [SerializeField] public float stuckSpeedEps = 0.02f;

    [Header("Drop Throw")]
    [SerializeField] float dropSideSpeedMin = 1.5f;
    [SerializeField] float dropSideSpeedMax = 3.5f;
    [SerializeField] float dropUpSpeedMin = 4f;
    [SerializeField] float dropUpSpeedMax = 7f;
    [SerializeField] float dropTorqueMin = -20f;
    [SerializeField] float dropTorqueMax = 20f;
    [SerializeField] float dropGravityScale = 1f;
    [SerializeField] bool dropBiasAwayFromPlayer = true;

    [Header("VFX")]
    [SerializeField] GameObject stunIndicator;

    [Header("Debug")]
    [SerializeField] public bool debugLogs = false;
    [SerializeField] public bool debugGizmos = true;

    private bool isStunned;
    private float stunTimer;
    private float postHitTimer;
    private float flipTimer;

    private bool blockTurnUntilExit;
    private Collider2D blockCol;

    private float stuckTimer;

    private Vector2 gizFootFrom, gizFootTo, gizSideFrom, gizSideTo;
    private bool gizNoGroundAhead, gizWallAhead, gizGrounded;
    private float gizTime;

    public bool IsStunned => isStunned;

    bool executed;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!mainCollider) mainCollider = GetComponent<Collider2D>();
        if (direction == 0) direction = -1;
        if (rb) rb.freezeRotation = true;
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (stunIndicator) stunIndicator.SetActive(false);
    }

    protected virtual void Update()
    {
        if (postHitTimer > 0f) postHitTimer -= Time.deltaTime;
        if (flipTimer > 0f) flipTimer -= Time.deltaTime;

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
        }

        if (spriteRenderer) spriteRenderer.flipX = direction > 0;

        if (animator)
        {
            bool running = !isStunned && Mathf.Abs(rb.linearVelocity.x) > runThreshold;
            animator.SetBool("IsRunning", running);
            animator.SetBool("Stunned", isStunned);
        }

        if (Mathf.Abs(rb.linearVelocity.x) < stuckSpeedEps && !isStunned && postHitTimer <= 0f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckFlipTime && flipTimer <= 0f && !blockTurnUntilExit)
            {
                direction = -direction;
                flipTimer = flipCooldown;
                if (debugLogs) Debug.Log("[Enemy] Anti-stuck flip");
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;

        UpdateStunFX();
    }

    protected virtual void FixedUpdate()
    {
        if (isStunned)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Sense(out bool grounded, out bool noGroundAhead, out bool wallAhead);

        if (!blockTurnUntilExit && flipTimer <= 0f && postHitTimer <= 0f)
        {
            if (wallAhead || (grounded && noGroundAhead))
            {
                direction = -direction;
                flipTimer = flipCooldown;
                if (debugLogs) Debug.Log($"[Enemy] TurnAround wall={wallAhead} noGroundAhead={noGroundAhead}");
            }
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    void Sense(out bool grounded, out bool noGroundAhead, out bool wallAhead)
    {
        grounded = false; noGroundAhead = false; wallAhead = false;
        if (!mainCollider)
        {
            if (debugLogs) Debug.LogWarning("[Enemy] Missing mainCollider");
            return;
        }

        var b = mainCollider.bounds;
        float skin = 0.02f;

        Vector2 foot = new Vector2(b.center.x, b.min.y + skin);
        var hitG = Physics2D.Raycast(foot, Vector2.down, edgeCheckDistance, groundMask);
        grounded = hitG.collider && hitG.collider != mainCollider && !hitG.collider.isTrigger;

        float xFront = b.center.x + Mathf.Sign(direction) * (b.extents.x + skin);
        Vector2 footFront = new Vector2(xFront, b.min.y + skin);
        var hitAheadGround = Physics2D.Raycast(footFront, Vector2.down, edgeCheckDistance, groundMask);
        noGroundAhead = !(hitAheadGround.collider && hitAheadGround.collider != mainCollider && !hitAheadGround.collider.isTrigger);

        Vector2 sideFrom = new Vector2(xFront, b.center.y);
        var hitWall = Physics2D.Raycast(sideFrom, new Vector2(Mathf.Sign(direction), 0f), wallCheckDistance, wallMask);
        wallAhead = hitWall.collider && hitWall.collider != mainCollider && !hitWall.collider.isTrigger;

        gizFootFrom = footFront;
        gizFootTo = footFront + Vector2.down * edgeCheckDistance;
        gizSideFrom = sideFrom;
        gizSideTo = sideFrom + new Vector2(Mathf.Sign(direction), 0f) * wallCheckDistance;
        gizNoGroundAhead = noGroundAhead;
        gizWallAhead = wallAhead;
        gizGrounded = grounded;
        gizTime = Time.time;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.collider.CompareTag("Player")) ResolvePlayerContact(c, true);
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (c.collider.CompareTag("Player")) ResolvePlayerContact(c, false);
    }

    void OnCollisionExit2D(Collision2D c)
    {
        if (blockTurnUntilExit && c.collider == blockCol)
        {
            blockTurnUntilExit = false;
            blockCol = null;
            if (debugLogs) Debug.Log("[Enemy] Unblock turn on exit");
        }
    }

    bool CanHurtNow() => !isStunned && postHitTimer <= 0f;

    void ResolvePlayerContact(Collision2D c, bool isEnter)
    {
        var prb = c.collider.attachedRigidbody;
        if (!prb || !mainCollider) return;

        float vy = prb.linearVelocity.y;
        float enemyTop = mainCollider.bounds.max.y;
        float playerBottom = c.collider.bounds.min.y;

        Vector2 n = Vector2.zero;
        for (int i = 0; i < c.contactCount; i++) n += c.GetContact(i).normal;
        if (c.contactCount > 0) n /= c.contactCount;

        bool above = playerBottom >= enemyTop - topHitMargin;
        bool falling = vy <= stompYThreshold;
        bool topSurface = n.y > 0.25f;

        bool sideHit = Mathf.Abs(n.x) > 0.5f && playerBottom < enemyTop - 0.02f;
        if (!sideHit && c.contactCount == 0)
        {
            float horiz = c.collider.bounds.center.x - mainCollider.bounds.center.x;
            bool horizontal = Mathf.Abs(horiz) > 0.05f;
            bool belowTop = playerBottom < enemyTop - 0.02f;
            sideHit = horizontal && belowTop;
        }

        if (debugLogs)
            Debug.Log($"[Enemy-Contact] enter={isEnter} vy={vy:F2} above={above} falling={falling} n={n} top={topSurface} side={sideHit} flipCD={flipTimer:F2} postCD={postHitTimer:F2}");

        if (above && falling && topSurface)
        {
            ApplyStun(prb);
            blockTurnUntilExit = false;
            blockCol = null;
            return;
        }

        if (!sideHit || !isEnter) return;

        int dirToPlayer = (c.collider.transform.position.x - transform.position.x) >= 0f ? 1 : -1;
        float relX = Mathf.Abs(c.relativeVelocity.x);
        if (flipTimer > 0f || relX < minRelSpeedToFlip) return;

        if (!CanHurtNow())
        {
            FlipAway(dirToPlayer);
            ArmExitBlock(c.collider);
            return;
        }

        var stun = FindPlayerStunFromCollision(c);
        if (stun && stun.CanBeStunned)
        {
            stun.ApplyStun(playerStunTime, new Vector2(dirToPlayer * 8f, 6f));
            postHitTimer = postHitCooldown;
        }

        FlipAway(dirToPlayer);
        ArmExitBlock(c.collider);
    }

    void FlipAway(int dirToPlayer)
    {
        if (flipTimer > 0f) return;
        direction = -dirToPlayer;
        var v = rb.linearVelocity;
        v.x = direction * Mathf.Max(moveSpeed, postHitImpulseX);
        rb.linearVelocity = v;
        flipTimer = flipCooldown;
        if (debugLogs) Debug.Log($"[Enemy] FlipAway dir={direction} vx={v.x:F2}");
    }

    void ArmExitBlock(Collider2D col)
    {
        blockTurnUntilExit = true;
        blockCol = col;
    }

    PlayerStun FindPlayerStunFromCollision(Collision2D c)
    {
        var prb = c.collider.attachedRigidbody;
        if (!prb) return null;
        PlayerStun stun = null;
        var go = prb.gameObject;
        if (!go.TryGetComponent(out stun)) stun = go.GetComponentInParent<PlayerStun>();
        if (!stun) stun = go.transform.root.GetComponentInChildren<PlayerStun>(true);
        if (!stun) stun = c.collider.GetComponentInParent<PlayerStun>();
        if (!stun) stun = c.collider.transform.root.GetComponentInChildren<PlayerStun>(true);
        return stun;
    }

    public void ApplyStun(Rigidbody2D prb)
    {
        isStunned = true;
        stunTimer = stunDuration;
        if (prb) prb.linearVelocity = new Vector2(prb.linearVelocity.x, bounceForce);
    }

    void UpdateStunFX()
    {
        if (!stunIndicator) return;
        bool on = isStunned;
        if (stunIndicator.activeSelf != on) stunIndicator.SetActive(on);
    }

    void OnDrawGizmos()
    {
        if (!debugGizmos) return;
        var col = mainCollider ? mainCollider : GetComponent<Collider2D>();
        if (!col) return;
        var b = col.bounds;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(b.min.x, b.max.y, 0f), new Vector3(b.max.x, b.max.y, 0f));

        Color g = gizGrounded ? Color.green : new Color(0f, 1f, 0f, 0.25f);
        Color r = gizWallAhead ? Color.red : new Color(1f, 0f, 0f, 0.25f);
        Color c = gizNoGroundAhead ? Color.magenta : new Color(1f, 0f, 1f, 0.25f);

        if (Time.time - gizTime < 0.5f)
        {
            Gizmos.color = c;
            Gizmos.DrawLine(gizFootFrom, gizFootTo);
            Gizmos.color = r;
            Gizmos.DrawLine(gizSideFrom, gizSideTo);
        }

        Vector2 foot = new Vector2(b.center.x, b.min.y + 0.02f);
        Gizmos.color = g;
        Gizmos.DrawLine(foot, foot + Vector2.down * edgeCheckDistance);
    }

    public void ExecuteKill(GameObject dropPrefab, Vector2 dropOffset, float destroyDelay)
    {
        if (executed) return;
        executed = true;

        if (stunIndicator) stunIndicator.SetActive(false);

        GameObject drop = null;
        if (dropPrefab) drop = Instantiate(dropPrefab, (Vector2)transform.position + dropOffset, Quaternion.identity);

        if (animator) { animator.ResetTrigger("Die"); animator.SetTrigger("Die"); }
        if (rb) rb.linearVelocity = Vector2.zero;

        var cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
        if (rb) rb.simulated = false;

        if (drop) ThrowDropAwayFromPlayer(drop);

        Destroy(gameObject, destroyDelay);
    }

    void ThrowDropAwayFromPlayer(GameObject drop)
    {
        var drb = drop.GetComponent<Rigidbody2D>();
        if (!drb) drb = drop.AddComponent<Rigidbody2D>();
        drb.bodyType = RigidbodyType2D.Dynamic;
        drb.gravityScale = dropGravityScale;

        float sign = player ? Mathf.Sign(transform.position.x - player.position.x) : (Random.value < 0.5f ? -1f : 1f);
        float vx = sign * Random.Range(dropSideSpeedMin, dropSideSpeedMax);
        float vy = Random.Range(dropUpSpeedMin, dropUpSpeedMax);
        drb.linearVelocity = new Vector2(vx, vy);

        float tq = Random.Range(dropTorqueMin, dropTorqueMax);
        drb.AddTorque(tq, ForceMode2D.Impulse);
    }

    void ThrowDrop(GameObject drop)
    {
        var drb = drop.GetComponent<Rigidbody2D>();
        if (!drb) drb = drop.AddComponent<Rigidbody2D>();

        drb.bodyType = RigidbodyType2D.Dynamic;
        drb.gravityScale = dropGravityScale;

        float sideSign;
        if (dropBiasAwayFromPlayer && player)
            sideSign = Mathf.Sign(drop.transform.position.x - player.position.x);
        else
            sideSign = Random.value < 0.5f ? -1f : 1f;

        float vx = sideSign * Random.Range(dropSideSpeedMin, dropSideSpeedMax);
        float vy = Random.Range(dropUpSpeedMin, dropUpSpeedMax);
        drb.linearVelocity = new Vector2(vx, vy);

        float tq = Random.Range(dropTorqueMin, dropTorqueMax);
        drb.AddTorque(tq, ForceMode2D.Impulse);
    }
}
