using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 45f;
    [SerializeField] float groundAcceleration = 600f;
    [SerializeField] float groundDeceleration = 600f;
    [SerializeField] float turnAcceleration = 900f;
    [SerializeField] float airAcceleration = 280f;
    [SerializeField] float airDeceleration = 240f;
    [SerializeField] float jumpForce = 20f;
    [SerializeField] float gravity = -30f;
    [SerializeField] float fallMultiplier = 3f;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] bool canMove = true;

    public float InputX { get; private set; }

    float horizontalInput;
    float coyoteCounter;
    float jumpBufferCounter;
    bool isGrounded;
    bool jumpRequested;
    bool jumpCut;

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        horizontalInput = canMove ? Input.GetAxisRaw("Horizontal") : 0f;
        InputX = horizontalInput;

        if (isGrounded) coyoteCounter = coyoteTime; else coyoteCounter -= Time.deltaTime;

        if (canMove && Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBufferTime; else jumpBufferCounter -= Time.deltaTime;
        if (canMove && Input.GetButtonUp("Jump")) jumpCut = true;

        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            jumpRequested = true;
            jumpBufferCounter = 0;
            coyoteCounter = 0;
        }
    }

    void FixedUpdate()
    {
        Vector2 vel = rb.linearVelocity;

        bool hasInput = Mathf.Abs(horizontalInput) > 0.01f;
        bool turning = hasInput && Mathf.Abs(vel.x) > 0.01f && Mathf.Sign(horizontalInput) != Mathf.Sign(vel.x);

        float targetX = horizontalInput * moveSpeed;
        float accel;

        if (isGrounded)
            accel = turning ? turnAcceleration : (hasInput ? groundAcceleration : groundDeceleration);
        else
            accel = hasInput ? airAcceleration : airDeceleration;

        vel.x = Mathf.MoveTowards(vel.x, targetX, accel * Time.fixedDeltaTime);

        if (jumpRequested)
        {
            vel.y = jumpForce;
            jumpRequested = false;
            jumpCut = false;
        }

        vel.y += gravity * Time.fixedDeltaTime;

        if (vel.y < 0)
            vel.y += gravity * (fallMultiplier - 1f) * Time.fixedDeltaTime;

        if (jumpCut && vel.y > 0)
        {
            vel.y *= 0.5f;
            jumpCut = false;
        }

        rb.linearVelocity = vel;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    public void BlockMovement() => canMove = false;
    public void UnblockMovement() => canMove = true;
}
