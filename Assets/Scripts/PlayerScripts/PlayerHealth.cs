using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;

    [Header("When hit")]
    public float invincibleTime = 1f;                       // can't be hit again for this long
    public float stunTime = 0.4f;                           // can't move for this long
    public Vector2 knockback = new Vector2(6f, 5f);         // x = away from the attacker, y = up

    public UnityEvent onHurt;
    public UnityEvent onDeath;

    [SerializeField] int currentHealth;   // visible in the Inspector

    public int Health => currentHealth;
    public bool IsDead { get; private set; }

    Rigidbody rb;
    PlayerMovement movement;
    PlayerAttack attack;
    Animator animator;
    float invincibleUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        animator = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount, Vector3 sourcePosition)
    {
        if (IsDead || Time.time < invincibleUntil)
            return;

        currentHealth -= amount;
        Debug.Log("Towelman was hit, health left: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        invincibleUntil = Time.time + invincibleTime;
        animator.SetTrigger("Hurt");

        float away = Mathf.Sign(transform.position.x - sourcePosition.x);
        rb.linearVelocity = new Vector3(away * knockback.x, knockback.y, 0f);
        movement.Stun(stunTime);

        StartCoroutine(Blink());
        onHurt?.Invoke();
    }

    void Die()
    {
        IsDead = true;
        animator.SetTrigger("Die");

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        movement.enabled = false;
        if (attack != null) attack.enabled = false;

        onDeath?.Invoke();
    }

    // Flicker while invincible
    IEnumerator Blink()
    {
        // only the Towelman model (not the old red cube)
        SkinnedMeshRenderer[] renderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
        bool visible = true;

        while (Time.time < invincibleUntil)
        {
            visible = !visible;
            foreach (SkinnedMeshRenderer r in renderers) r.enabled = visible;
            yield return new WaitForSeconds(0.1f);
        }

        foreach (SkinnedMeshRenderer r in renderers) r.enabled = true;
    }
}
