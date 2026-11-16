using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float gravityMultiplier = 2f;
    public float groundCheckDistance = 0.15f;

    [Header("Crouch")]
    public float crouchHeightMultiplier = 0.5f;

    private Rigidbody rb;
    private BoxCollider boxCollider;

    private Vector3 originalScale;
    private Vector3 originalColliderSize;
    private Vector3 originalColliderCenter;

    private bool isCrouching;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();

        originalScale = transform.localScale;
        originalColliderSize = boxCollider.size;
        originalColliderCenter = boxCollider.center;
    }

    void Update()
    {
        Move();

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            StartCrouch();
        }

        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            StopCrouch();
        }
    }

    void FixedUpdate()
    {
        ApplyExtraGravity();
    }

    void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");

        Vector3 velocity = rb.linearVelocity;
        velocity.x = horizontal * moveSpeed;

        rb.linearVelocity = velocity;
    }

    void Jump()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;

        rb.linearVelocity = velocity;
    }

    void ApplyExtraGravity()
    {
        if (!IsGrounded())
        {
            rb.AddForce(
                Physics.gravity * (gravityMultiplier - 1f),
                ForceMode.Acceleration
            );
        }
    }

    void StartCrouch()
    {
        if (isCrouching)
            return;

        isCrouching = true;

        Vector3 scale = originalScale;
        scale.y *= crouchHeightMultiplier;
        transform.localScale = scale;

        Vector3 colliderSize = originalColliderSize;
        colliderSize.y *= crouchHeightMultiplier;
        boxCollider.size = colliderSize;

        Vector3 colliderCenter = originalColliderCenter;
        colliderCenter.y -=
            (originalColliderSize.y - colliderSize.y) / 2f;

        boxCollider.center = colliderCenter;
    }

    void StopCrouch()
    {
        if (!isCrouching)
            return;

        isCrouching = false;

        transform.localScale = originalScale;
        boxCollider.size = originalColliderSize;
        boxCollider.center = originalColliderCenter;
    }

    bool IsGrounded()
    {
        float distance =
            boxCollider.bounds.extents.y + groundCheckDistance;

        return Physics.Raycast(
            transform.position,
            Vector3.down,
            distance
        );
    }
}