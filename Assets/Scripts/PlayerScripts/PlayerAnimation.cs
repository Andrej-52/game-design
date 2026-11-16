using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator animator;
    public Transform model;
    public float turnSpeed = 15f;

    Rigidbody rb;
    PlayerMovement movement;
    float facing = 1f;

    // +1 = facing right, -1 = facing left (used by PlayerAttack)
    public float Facing => facing;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!model) model = animator.transform;
    }

    void OnEnable()
    {
        movement.OnJump += HandleJump;
    }

    void OnDisable()
    {
        movement.OnJump -= HandleJump;
    }

    void HandleJump()
    {
        animator.SetTrigger("Jump");
    }

    void Update()
    {
        Vector3 v = rb.linearVelocity;
        // face the way the player is pressing, not the way he is moving (knockback would flip him)
        float input = Input.GetAxisRaw("Horizontal");
        if (!movement.IsStunned && Mathf.Abs(input) > 0.05f) facing = Mathf.Sign(input);

        Quaternion target = Quaternion.Euler(0f, facing * 90f, 0f);
        model.localRotation = Quaternion.Slerp(model.localRotation, target, turnSpeed * Time.deltaTime);

        bool grounded = movement.IsGrounded();
        animator.SetFloat("Speed", Mathf.Abs(v.x));
        animator.SetFloat("VelY", v.y);
        animator.SetBool("Grounded", grounded);

    }
}