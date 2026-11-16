using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;

    [Header("When hit by an enemy")]
    public float invincibleTime = 1f;                       // can't be hit again for this long
    public float stunTime = 0.4f;                           // can't move for this long
    public Vector2 knockback = new Vector2(6f, 5f);         // x = away from the attacker, y = up

    [Header("When hit by a hazard (bus, pit, ...)")]
    public float respawnDelay = 0.6f;                       // how long the hurt animation plays before he is moved
    public float respawnLookback = 2f;                      // he is put back where he stood this many seconds earlier
    public float respawnInvincibleTime = 2f;                // protection after the respawn

    [Header("Game over")]
    public float restartDelay = 3f;                         // only used when the scene has no GameOverUI

    public UnityEvent onHurt;
    public UnityEvent onDeath;

    [SerializeField] int currentHealth;   // visible in the Inspector

    public int Health => currentHealth;
    public bool IsDead { get; private set; }
    public bool Immune { get; set; }     // true after reaching the office: nothing can hurt him anymore

    Rigidbody rb;
    PlayerMovement movement;
    PlayerAttack attack;
    Animator animator;
    float invincibleUntil;

    // places where he stood on solid ground, used to respawn him "right before"
    struct Spot { public float time; public Vector3 position; }
    readonly List<Spot> safeSpots = new List<Spot>();
    float nextRecordTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        animator = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (IsDead || Time.time < nextRecordTime || !movement.IsGrounded())
            return;

        nextRecordTime = Time.time + 0.2f;
        safeSpots.Add(new Spot { time = Time.time, position = transform.position });

        // only keep what we need
        while (safeSpots.Count > 0 && safeSpots[0].time < Time.time - respawnLookback - 3f)
            safeSpots.RemoveAt(0);
    }

    // Hit by an enemy: knockback
    public void TakeDamage(int amount, Vector3 sourcePosition)
    {
        if (!Hurt(amount, invincibleTime))
            return;

        float away = Mathf.Sign(transform.position.x - sourcePosition.x);
        rb.linearVelocity = new Vector3(away * knockback.x, knockback.y, 0f);
        movement.Stun(stunTime);
    }

    // Hit by a hazard: hurt, then put back where he was a moment ago
    public void HitByHazard(int amount)
    {
        if (!Hurt(amount, respawnDelay + respawnInvincibleTime))
            return;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        movement.Stun(respawnDelay);
        StartCoroutine(RespawnAfterDelay());
    }

    // returns false if the hit didn't count (already dead or protected)
    bool Hurt(int amount, float protectedFor)
    {
        if (IsDead || Immune || Time.time < invincibleUntil)
            return false;

        currentHealth -= amount;
        Debug.Log("Towelman was hit, health left: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return false;
        }

        invincibleUntil = Time.time + protectedFor;
        animator.SetTrigger("Hurt");

        StartCoroutine(Blink());
        onHurt?.Invoke();
        return true;
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (IsDead)
            yield break;

        Vector3 target = FindRespawnPosition();
        rb.position = target;
        transform.position = target;
        rb.linearVelocity = Vector3.zero;
    }

    // the latest safe spot from at least respawnLookback seconds ago (or the oldest we have)
    Vector3 FindRespawnPosition()
    {
        if (safeSpots.Count == 0)
            return transform.position;

        for (int i = safeSpots.Count - 1; i >= 0; i--)
        {
            if (safeSpots[i].time <= Time.time - respawnLookback)
                return safeSpots[i].position;
        }

        return safeSpots[0].position;
    }

    // Used by the level timer when time runs out
    public void Kill()
    {
        if (IsDead)
            return;

        currentHealth = 0;
        Die();
    }

    void Die()
    {
        IsDead = true;
        animator.SetTrigger("Die");

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        movement.enabled = false;
        if (attack != null) attack.enabled = false;

        onDeath?.Invoke();

        // with a game over screen the player picks restart / back to start, otherwise restart automatically
        if (FindFirstObjectByType<GameOverUI>() == null)
            Invoke(nameof(RestartScene), restartDelay);
    }

    void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
