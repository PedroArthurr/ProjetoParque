using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] CapsuleCollider2D capsule;
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] LayerMask groundLayer;

    [SerializeField] float groundCheckDistance = 0.08f;
    [SerializeField] float groundedStableTime = 0.08f;

    [SerializeField] float runOnInput = 0.55f;
    [SerializeField] float runOffInput = 0.35f;
    [SerializeField] float minRunTime = 0.18f;
    [SerializeField] float minIdleTime = 0.18f;

    [SerializeField] float landingHold = 0.12f;
    [SerializeField] float minAirTime = 0.03f;
    [SerializeField] float landYVelMax = -0.02f;

    const string P_IsGrounded = "IsGrounded";
    const string P_IsRunning = "IsRunning";
    const string P_YVel = "YVel";
    const string P_Land = "Land";

    ContactFilter2D filter;
    readonly RaycastHit2D[] hits = new RaycastHit2D[2];

    bool groundedRaw, groundedStable, wasGroundedRaw;
    bool isRunningState, landingLock;
    float groundedTimer, runStateTimer, airTimer, landTimer;

    void Awake()
    {
        if (!rb) rb = GetComponentInParent<Rigidbody2D>();
        if (!capsule) capsule = GetComponentInParent<CapsuleCollider2D>();
        if (!playerMovement) playerMovement = GetComponentInParent<PlayerMovement>();
        filter.useLayerMask = true; filter.layerMask = groundLayer; filter.useTriggers = false;
    }

    bool IsGroundedRaw() => capsule && capsule.Cast(Vector2.down, filter, hits, groundCheckDistance) > 0;

    void SetBool(string n, bool v) { if (animator) animator.SetBool(n, v); }
    void SetFloat(string n, float v) { if (animator) animator.SetFloat(n, v); }
    void Trigger(string n) { if (animator) { animator.ResetTrigger(n); animator.SetTrigger(n); } }

    void Update()
    {
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
            airTimer += Time.deltaTime;
            landTimer = 0f;
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
                isRunningState = true;
                runStateTimer = minRunTime;
                SetBool(P_IsRunning, true);
            }
            else runStateTimer = Mathf.Max(0f, runStateTimer - Time.deltaTime);
        }
        else
        {
            if ((!canRun || inAbs <= runOffInput) && runStateTimer <= 0f)
            {
                isRunningState = false;
                runStateTimer = minIdleTime;
                SetBool(P_IsRunning, false);
            }
            else runStateTimer = Mathf.Max(0f, runStateTimer - Time.deltaTime);
        }

        wasGroundedRaw = groundedRaw;
    }
}
