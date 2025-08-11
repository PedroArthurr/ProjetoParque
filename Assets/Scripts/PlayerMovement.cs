using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 45f;
    [SerializeField] private float groundAcceleration = 600f;
    [SerializeField] private float groundDeceleration = 600f;
    [SerializeField] private float turnAcceleration = 900f;
    [SerializeField] private float airAcceleration = 280f;
    [SerializeField] private float airDeceleration = 240f;
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private bool canMove = true;
    [SerializeField] private PlayerStun stun;
    [SerializeField] private bool zeroVXWhileStunned = true;

    public float InputX { get; private set; }

    private float horizontalInput;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isGrounded;
    private bool jumpRequested;
    private bool jumpCut;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!stun) stun = GetComponent<PlayerStun>();
    }

    private void Update()
    {
        bool stunned = stun && stun.IsStunned;
        bool inputAllowed = canMove && !stunned;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        horizontalInput = inputAllowed ? Input.GetAxisRaw("Horizontal") : 0f;
        InputX = horizontalInput;

        if (isGrounded) coyoteCounter = coyoteTime; else coyoteCounter -= Time.deltaTime;

        if (inputAllowed && Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBufferTime; else jumpBufferCounter -= Time.deltaTime;
        if (inputAllowed && Input.GetButtonUp("Jump")) jumpCut = true;

        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            jumpRequested = true;
            jumpBufferCounter = 0;
            coyoteCounter = 0;
        }

        if (!inputAllowed)
        {
            jumpRequested = false;
            jumpCut = false;
        }
    }

    private void FixedUpdate()
    {
        bool stunned = stun && stun.IsStunned;
        bool inputAllowed = canMove && !stunned;

        Vector2 vel = rb.linearVelocity;

        if (!inputAllowed)
        {
            if (zeroVXWhileStunned) vel.x = 0f;
            else
            {
                float stopAccel = isGrounded ? groundDeceleration : airDeceleration;
                vel.x = Mathf.MoveTowards(vel.x, 0f, stopAccel * Time.fixedDeltaTime);
            }

            vel.y += gravity * Time.fixedDeltaTime;
            if (vel.y < 0) vel.y += gravity * (fallMultiplier - 1f) * Time.fixedDeltaTime;

            rb.linearVelocity = vel;
            return;
        }

        bool hasInput = Mathf.Abs(horizontalInput) > 0.01f;
        bool turning = hasInput && Mathf.Abs(vel.x) > 0.01f && Mathf.Sign(horizontalInput) != Mathf.Sign(vel.x);

        float targetX = horizontalInput * moveSpeed;
        float accel = isGrounded
            ? (turning ? turnAcceleration : (hasInput ? groundAcceleration : groundDeceleration))
            : (hasInput ? airAcceleration : airDeceleration);

        vel.x = Mathf.MoveTowards(vel.x, targetX, accel * Time.fixedDeltaTime);

        if (jumpRequested)
        {
            vel.y = jumpForce;
            jumpRequested = false;
            jumpCut = false;
        }

        vel.y += gravity * Time.fixedDeltaTime;

        if (vel.y < 0) vel.y += gravity * (fallMultiplier - 1f) * Time.fixedDeltaTime;

        if (jumpCut && vel.y > 0)
        {
            vel.y *= 0.5f;
            jumpCut = false;
        }

        rb.linearVelocity = vel;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    public void BlockMovement() => canMove = false;

    public void UnblockMovement() => canMove = true;
}