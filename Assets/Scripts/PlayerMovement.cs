using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float groundCheckDistance = 0.15f;

    private Rigidbody rb;
    private BoxCollider boxCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
    }

    void Update()
    {
        Move();

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            Jump();
        }
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