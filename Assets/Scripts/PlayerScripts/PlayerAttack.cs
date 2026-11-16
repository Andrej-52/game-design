using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Input")]
    public KeyCode attackKey = KeyCode.Space;
    public float attackCooldown = 0.8f;   // set to roughly the length of the whip animation

    [Header("Damage")]
    public int damage = 1;
    public LayerMask enemyMask = ~0;

    [Header("Hit box (in front of the player)")]
    public Vector3 hitBoxSize = new Vector3(2.5f, 2f, 4f);
    public float hitBoxOffset = 1.5f;
    public float hitBoxYOffset = -0.5f;   // negative = lower the hit box

    // Fired when a swing starts / when a swing hits at least one enemy (used by PlayerSounds)
    public System.Action OnSwing;
    public System.Action OnHitEnemy;

    Animator animator;
    PlayerAnimation playerAnimation;
    float lastAttackTime = -10f;
    readonly HashSet<EnemyHealth> hitThisSwing = new HashSet<EnemyHealth>();

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        playerAnimation = GetComponent<PlayerAnimation>();
    }

    void Update()
    {
        if (Input.GetKeyDown(attackKey) && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            hitThisSwing.Clear();
            animator.SetTrigger("Whip");
            OnSwing?.Invoke();
        }
    }

    // Called from the "WhipHit" animation event (see PlayerAnimationEvents)
    public void DoHit()
    {
        Collider[] hits = Physics.OverlapBox(
            HitCenter(), hitBoxSize * 0.5f, Quaternion.identity,
            enemyMask, QueryTriggerInteraction.Collide
        );

        bool hitSomething = false;

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform))
                continue;

            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();

            // each enemy can only be hit once per swing
            if (enemy == null || !hitThisSwing.Add(enemy))
                continue;

            enemy.TakeDamage(damage);
            hitSomething = true;
        }

        if (hitSomething)
            OnHitEnemy?.Invoke();
    }

    Vector3 HitCenter()
    {
        float dir = playerAnimation != null ? playerAnimation.Facing : 1f;
        return transform.position + new Vector3(dir * hitBoxOffset, hitBoxYOffset, 0f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(HitCenter(), hitBoxSize);
    }
}
