using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public float gravityMultiplier = 2f;
    public float groundCheckDistance = 0.15f;
    public LayerMask groundMask = ~0;
    public float jumpCooldown = 0.2f;
    public KeyCode jumpKey = KeyCode.W;
    public KeyCode jumpKeyAlt = KeyCode.UpArrow;

    // Fired the moment a jump actually starts (used by PlayerAnimation)
    public System.Action OnJump;

    [Header("Uneven ground")]
    [Tooltip("Steps and bumps up to this height (in world units) are walked over instead of blocking him")]
    public float stepHeight = 0.6f;
    public float stepCheckDistance = 0.12f;

    [Header("Crouch")]
    public float crouchHeightMultiplier = 0.5f;

    private Rigidbody rb;
    private BoxCollider boxCollider;

    private Vector3 originalScale;
    private Vector3 originalColliderSize;
    private Vector3 originalColliderCenter;

    private bool isCrouching;
    private float lastJumpTime = -10f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();

        originalScale = transform.localScale;
        originalColliderSize = boxCollider.size;
        originalColliderCenter = boxCollider.center;

        // no friction, so he never sticks to walls or seams between pieces of ground
        PhysicsMaterial slippery = new PhysicsMaterial("PlayerSlippery");
        slippery.dynamicFriction = 0f;
        slippery.staticFriction = 0f;
        slippery.frictionCombine = PhysicsMaterialCombine.Minimum;
        boxCollider.material = slippery;
    }

    // While stunned (after being hit) the player cannot move or jump
    float stunUntil;

    public bool IsStunned => Time.time < stunUntil;

    public void Stun(float seconds)
    {
        stunUntil = Time.time + seconds;
    }

    void Update()
    {
        if (Time.time < stunUntil)
            return;

        Move();

        if ((Input.GetKeyDown(jumpKey) || Input.GetKeyDown(jumpKeyAlt)) && CanJump())
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
        StepUp();
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

        lastJumpTime = Time.time;
        OnJump?.Invoke();
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

    // Walks over small steps and bumps: if something low blocks the way but the space above it is free,
    // he is lifted up onto it.
    void StepUp()
    {
        if (Time.time < stunUntil || isCrouching)
            return;

        float input = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(input) < 0.1f || !IsGrounded())
            return;

        Vector3 dir = new Vector3(Mathf.Sign(input), 0f, 0f);
        Bounds b = boxCollider.bounds;
        float reach = b.extents.x + stepCheckDistance;

        // something low in front of his feet?
        Vector3 feet = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);
        if (!CastSolid(feet, dir, reach, out _))
            return;

        // too tall to step over (a real wall)?
        Vector3 above = new Vector3(b.center.x, b.min.y + stepHeight + 0.05f, b.center.z);
        if (CastSolid(above, dir, reach + 0.1f, out _))
            return;

        // how high is the top of it?
        Vector3 top = new Vector3(b.center.x + dir.x * (reach + 0.05f), b.min.y + stepHeight + 0.05f, b.center.z);
        if (CastSolid(top, Vector3.down, stepHeight + 0.05f, out RaycastHit down))
        {
            float rise = down.point.y - b.min.y;
            if (rise > 0.01f && rise <= stepHeight)
                rb.position += Vector3.up * (rise + 0.02f);
        }
    }

    // nearest level geometry hit (ignores himself, triggers and enemies)
    bool CastSolid(Vector3 origin, Vector3 direction, float distance, out RaycastHit result)
    {
        result = default;
        float best = float.MaxValue;
        bool found = false;

        foreach (RaycastHit h in Physics.RaycastAll(origin, direction, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<EnemyHealth>() != null) continue;

            if (h.distance < best)
            {
                best = h.distance;
                result = h;
                found = true;
            }
        }

        return found;
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

    bool CanJump()
    {
        return Time.time - lastJumpTime > jumpCooldown && IsGrounded();
    }

    public bool IsGrounded()
    {
        // Still going up fast = in the air, even if we are close to the floor
        if (rb.linearVelocity.y > 0.1f)
            return false;

        Bounds b = boxCollider.bounds;
        Vector3 center = new Vector3(b.center.x, b.min.y - groundCheckDistance * 0.5f, b.center.z);
        Vector3 halfExtents = new Vector3(b.extents.x * 0.9f, groundCheckDistance * 0.5f, b.extents.z * 0.9f);

        Collider[] hits = Physics.OverlapBox(
            center, halfExtents, Quaternion.identity,
            groundMask, QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            // Ignore ourselves and anything on the Towelman model
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }
}
