using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D capsule;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerStun playerStun;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private float groundedStableTime = 0.08f;

    [SerializeField] private float runOnInput = 0.55f;
    [SerializeField] private float runOffInput = 0.35f;
    [SerializeField] private float minRunTime = 0.18f;
    [SerializeField] private float minIdleTime = 0.18f;

    [SerializeField] private float landingHold = 0.12f;
    [SerializeField] private float minAirTime = 0.03f;
    [SerializeField] private float landYVelMax = -0.02f;

    [SerializeField] private string hurtState = "Hurt";

    private const string P_IsGrounded = "IsGrounded";
    private const string P_IsRunning = "IsRunning";
    private const string P_YVel = "YVel";
    private const string P_Land = "Land";
    private const string P_Crouch = "Crouch";
    private const string P_Hurt = "Hurt";

    private ContactFilter2D filter;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[2];

    private bool groundedRaw, groundedStable, wasGroundedRaw;
    private bool isRunningState, landingLock, subStun;
    private float groundedTimer, runStateTimer, airTimer, landTimer;

    private void Awake()
    {
        if (!rb) rb = GetComponentInParent<Rigidbody2D>();
        if (!capsule) capsule = GetComponentInParent<CapsuleCollider2D>();
        if (!playerMovement) playerMovement = GetComponentInParent<PlayerMovement>();
        if (!playerStun) playerStun = GetComponentInParent<PlayerStun>();
        filter.useLayerMask = true; filter.layerMask = groundLayer; filter.useTriggers = false;
    }

    private void OnEnable()
    { TrySubStun(); }

    private void Update()
    {
        if (!subStun) TrySubStun();

        groundedRaw = IsGroundedRaw();
        groundedTimer = groundedRaw ? groundedStableTime : Mathf.Max(0f, groundedTimer - Time.deltaTime);
        groundedStable = groundedRaw || groundedTimer > 0f;

        Vector2 v = rb ? rb.linearVelocity : Vector2.zero;
        float inAbs = playerMovement ? Mathf.Abs(playerMovement.InputX) : 0f;

        if (spriteRenderer && Mathf.Abs(playerMovement ? playerMovement.InputX : v.x) > 0.01f)
            spriteRenderer.flipX = (playerMovement ? playerMovement.InputX : v.x) < 0f;

        SetBool(P_IsGrounded, groundedStable);
        SetFloat(P_YVel, v.y);

        if (!groundedRaw)
        {
            airTimer += Time.deltaTime; landTimer = 0f;
            if (landingLock && playerMovement) { playerMovement.UnblockMovement(); landingLock = false; }
        }
        else
        {
            bool landedNow = !wasGroundedRaw && airTimer > minAirTime && v.y <= landYVelMax;
            if (landedNow)
            {
                Trigger(P_Land);
                landTimer = landingHold;
                if (playerMovement && !landingLock) { playerMovement.BlockMovement(); landingLock = true; }
            }

            if (landTimer > 0f) landTimer -= Time.deltaTime;
            else if (landingLock && playerMovement) { playerMovement.UnblockMovement(); landingLock = false; }

            airTimer = 0f;
        }

        bool canRun = groundedStable && !landingLock;
        if (!isRunningState)
        {
            if (canRun && inAbs >= runOnInput && runStateTimer <= 0f)
            {
                isRunningState = true; runStateTimer = minRunTime; SetBool(P_IsRunning, true);
            }
            else runStateTimer = Mathf.Max(0f, runStateTimer - Time.deltaTime);
        }
        else
        {
            if ((!canRun || inAbs <= runOffInput) && runStateTimer <= 0f)
            {
                isRunningState = false; runStateTimer = minIdleTime; SetBool(P_IsRunning, false);
            }
            else runStateTimer = Mathf.Max(0f, runStateTimer - Time.deltaTime);
        }

        wasGroundedRaw = groundedRaw;
    }

    private void OnDisable()
    {
        
        if (subStun && playerStun != null) playerStun.OnStunned -= OnHurt;
        subStun = false;
    }


    private void TrySubStun()
    {
        if (subStun || playerStun == null) return;
        playerStun.OnStunned += OnHurt; subStun = true;
    }

    private void OnHurt()
    {
        Debug.Log("[Anim] HURT trigger");
        if (!animator) return;
        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Hurt");
        int hash = Animator.StringToHash("Hurt");
        if (animator.HasState(0, hash)) animator.CrossFade(hash, 0.05f, 0, 0f);
    }

    public void PlayExecute()
    {
        const string P_Crouch = "Crouch";
        if (animator) { animator.ResetTrigger(P_Crouch); animator.SetTrigger(P_Crouch); }
    }


    private bool IsGroundedRaw() => capsule && capsule.Cast(Vector2.down, filter, hits, groundCheckDistance) > 0;

    private void SetBool(string n, bool v)
    { if (animator) animator.SetBool(n, v); }

    private void SetFloat(string n, float v)
    { if (animator) animator.SetFloat(n, v); }

    private void Trigger(string n)
    { if (animator) { animator.ResetTrigger(n); animator.SetTrigger(n); } }

    private void OnDrawGizmosSelected()
    {
        if (!capsule) return;
        var b = capsule.bounds;
        Vector3 from = new Vector3(b.center.x, b.min.y, 0f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(from, from + Vector3.down * groundCheckDistance);
    }
}