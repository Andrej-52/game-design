using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Patrols back and forth, chases the player when he gets close, attacks, and plays hurt / death animations.
// Works for all enemies (pedestrian, old lady, bike guy): the Animator needs triggers Attack, Hurt, Die.
[RequireComponent(typeof(Rigidbody), typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float patrolDistance = 4f;      // how far from the start position it walks before turning
    public bool avoidLedges = true;
    public float turnSpeed = 12f;
    [Tooltip("Extra turn of the model. 0 if the model faces +Z, 180 if it faces -Z. Change this if the enemy walks backwards.")]
    public float yawOffset = 0f;
    [Tooltip("Works out Yaw Offset from where the eyes are (pedestrian and old lady). Turn off to set it by hand.")]
    public bool autoDetectFront = true;

    [Header("Chase")]
    public float detectRange = 6f;
    public float chaseSpeedMultiplier = 1.5f;

    [Header("Attack")]
    public float attackRange = 1.6f;
    public float attackCooldown = 1.5f;
    public float attackSpeed = 1.6f;                  // plays the attack animation faster = quicker hit
    [Range(0f, 1f)] public float hitMoment = 0.5f;   // how far into the attack animation the hit lands
    public int damage = 1;
    public Vector3 hitBoxSize = new Vector3(2f, 2f, 2f);
    public float hitBoxOffset = 1f;

    [Header("After death")]
    public float deathSpeed = 1.5f;     // plays the death animation faster
    public float lingerTime = 0.3f;     // seconds the body stays after the death animation

    Rigidbody rb;
    EnemyHealth health;
    Animator animator;
    Transform model;
    Transform pivot;   // parent of the model that we turn (the animation can't override it)
    Collider bodyCollider;
    Transform player;

    float startX;
    float facing = 1f;
    float patrolDir = 1f;
    float busyUntil;
    float nextTurnTime;
    float chaseLockUntil;
    float lastAttackTime = -10f;
    bool dead;
    Coroutine attackRoutine;

    // read from the animation clips so the AI always matches the animations
    float attackLength = 1f;
    float hurtLength = 0.8f;
    float deathLength = 1.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
        bodyCollider = GetComponent<Collider>();
        animator = GetComponentInChildren<Animator>();
        model = animator.transform;

        DetectFront();

        // start facing the way the model was placed in the scene
        facing = VisualFacing();
        patrolDir = facing;

        if (animator.avatar == null)
            Debug.LogWarning(name + ": the Animator has no Avatar. Run Tools > Towelman > Fix Avatar On Enemy Prefabs.");

        // turn a pivot above the model instead of the model itself
        pivot = new GameObject("FacingPivot").transform;
        pivot.SetParent(transform, false);
        pivot.position = model.position;
        model.SetParent(pivot, true);

        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        // the AI handles death itself (plays the animation, then removes the enemy)
        health.destroyOnDeath = false;
        if (health.onHurt == null) health.onHurt = new UnityEvent();
        if (health.onDeath == null) health.onDeath = new UnityEvent();
        health.onHurt.AddListener(OnHurt);
        health.onDeath.AddListener(OnDeath);

        // effect pieces are scaled by the animations, so the mesh bounds are wrong without this
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = true;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (Has(clip.name, "Attack")) attackLength = clip.length;
            else if (Has(clip.name, "Hurt")) hurtLength = clip.length;
            else if (Has(clip.name, "Death")) deathLength = clip.length;
        }
    }

    void Start()
    {
        startX = transform.position.x;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        Quaternion target = Quaternion.Euler(0f, facing * 90f + yawOffset, 0f);
        Quaternion wanted = target * Quaternion.Inverse(model.localRotation);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, wanted, turnSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (dead)
            return;

        Vector3 v = rb.linearVelocity;

        // attacking or hurt: stand still (the animation plays at its own speed)
        if (Time.time < busyUntil)
        {
            v.x = 0f;
            rb.linearVelocity = v;
            return;
        }

        float dir = 0f;
        float speed = walkSpeed;
        bool chasing = false;

        if (player != null)
        {
            Vector3 d = player.position - transform.position;
            bool sameHeight = Mathf.Abs(d.y) < 3f;
            float dx = Mathf.Abs(d.x);

            if (sameHeight && dx <= attackRange)
            {
                facing = Mathf.Sign(d.x);

                if (Time.time - lastAttackTime >= attackCooldown)
                    StartAttack();
                else
                    animator.speed = 0f;   // waiting for the next attack: hold the pose instead of walking in place

                v.x = 0f;
                rb.linearVelocity = v;
                return;
            }

            if (sameHeight && dx <= detectRange && Time.time >= chaseLockUntil)
            {
                dir = Mathf.Sign(d.x);

                // can't get to the player (wall or gap): give up chasing for a moment
                if (Blocked(dir))
                {
                    chaseLockUntil = Time.time + 1.5f;
                    dir = 0f;
                }
                else
                {
                    chasing = true;
                    speed *= chaseSpeedMultiplier;
                }
            }
        }

        if (!chasing)
        {
            // turn around at the end of the patrol, or when something is in the way (at most every half second)
            if (Time.time >= nextTurnTime)
            {
                bool atEnd = (transform.position.x - startX) * patrolDir > patrolDistance;
                if (atEnd || Blocked(patrolDir))
                {
                    patrolDir = -patrolDir;
                    nextTurnTime = Time.time + 0.5f;
                }
            }

            dir = Blocked(patrolDir) ? 0f : patrolDir;
        }

        if (dir != 0f) facing = dir;

        // only play the walk animation while actually moving
        animator.speed = dir != 0f ? 1f : 0f;

        v.x = dir * speed;
        rb.linearVelocity = v;
    }

    bool Blocked(float dir)
    {
        Bounds b = bodyCollider.bounds;
        Vector3 forward = new Vector3(dir, 0f, 0f);

        // wall, checked at chest height and near the feet (so low obstacles count too)
        float reach = b.extents.x + 0.15f;
        Vector3 low = new Vector3(b.center.x, b.min.y + 0.25f, b.center.z);
        if (HitSolid(Physics.RaycastAll(b.center, forward, reach, ~0, QueryTriggerInteraction.Ignore)) ||
            HitSolid(Physics.RaycastAll(low, forward, reach, ~0, QueryTriggerInteraction.Ignore)))
            return true;

        // no floor ahead
        if (avoidLedges)
        {
            Vector3 origin = new Vector3(b.center.x + dir * (b.extents.x + 0.2f), b.min.y + 0.5f, b.center.z);
            if (!HitSolid(Physics.SphereCastAll(origin, 0.2f, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore)))
                return true;
        }

        return false;
    }

    // true if any hit is level geometry (not ourselves, the player or other enemies)
    bool HitSolid(RaycastHit[] hits)
    {
        foreach (RaycastHit h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            if (h.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            return true;
        }
        return false;
    }

    void StartAttack()
    {
        float length = attackLength / attackSpeed;

        lastAttackTime = Time.time;
        busyUntil = Time.time + length;
        animator.speed = attackSpeed;
        animator.SetTrigger("Attack");
        attackRoutine = StartCoroutine(HitAfter(length * hitMoment));
    }

    IEnumerator HitAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (dead)
            yield break;

        Vector3 center = bodyCollider.bounds.center + new Vector3(facing * hitBoxOffset, 0f, 0f);
        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage, transform.position);
                break;
            }
        }
    }

    void OnHurt()
    {
        if (dead) return;

        if (attackRoutine != null) StopCoroutine(attackRoutine);
        animator.ResetTrigger("Attack");
        animator.speed = 1f;
        animator.SetTrigger("Hurt");

        busyUntil = Time.time + hurtLength;
        lastAttackTime = Time.time;   // no instant counter attack
    }

    void OnDeath()
    {
        if (dead) return;
        dead = true;

        if (attackRoutine != null) StopCoroutine(attackRoutine);
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Die");
        animator.speed = deathSpeed;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        Destroy(gameObject, deathLength / deathSpeed + lingerTime);
    }

    // Finds out which way the model's face points (eyes in front of the head), so Yaw Offset doesn't have to be guessed.
        void DetectFront()
    {
        if (!autoDetectFront) return;

        Transform head = null;
        Vector3 eyes = Vector3.zero;
        int count = 0;

        foreach (Transform t in model.GetComponentsInChildren<Transform>())
        {
            if (t.name == "Head") head = t;
            else if (Has(t.name, "EyeBone") || Has(t.name, "PupilBone") || Has(t.name, "EyesFlat")) { eyes += t.position; count++; }
        }

        if (head == null || count == 0) return;

        Vector3 local = model.InverseTransformDirection(eyes / count - head.position);
        if (Mathf.Abs(local.z) < 0.01f) return;

        yawOffset = local.z > 0f ? 0f : 180f;
    }

    // Which way the model is actually looking (+1 right, -1 left), whatever its rotation in the scene
    float VisualFacing()
    {
        Vector3 front = model.rotation * Quaternion.Euler(0f, -yawOffset, 0f) * Vector3.forward;
        return front.x >= 0f ? 1f : -1f;
    }

    static bool Has(string text, string key)
    {
        return text.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void OnDrawGizmosSelected()
    {
        Collider c = GetComponent<Collider>();
        Vector3 center = c != null ? c.bounds.center : transform.position;
        float x = Application.isPlaying ? startX : transform.position.x;

        float dir = facing;
        if (!Application.isPlaying)
        {
            Animator a = GetComponentInChildren<Animator>();
            if (a != null)
            {
                model = a.transform;
                dir = VisualFacing();
            }
        }

        // patrol range (blue), detect range (yellow), attack hit box (red)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(x - patrolDistance, center.y, 0f), new Vector3(x + patrolDistance, center.y, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(detectRange * 2f, 0.2f, 0.2f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center + new Vector3(dir * hitBoxOffset, 0f, 0f), hitBoxSize);
    }
}
