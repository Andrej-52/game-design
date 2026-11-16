using System.Collections;
using UnityEngine;

// Looping background animation for the main menu:
// Towelman walks, checks his watch, starts running, stumbles, gets up and runs off screen. Then it repeats.
// Everything is placed relative to the camera view, so it works at any resolution.
// The menu UI is drawn on top of him, so he runs along the bottom of the screen.
public class MenuRunner : MonoBehaviour
{
    [Header("Where")]
    public Camera cam;
    public Animator animator;
    [Tooltip("Distance in front of the camera")] public float depth = 14f;
    [Tooltip("Feet height on screen, 0 = bottom, 1 = top")] [Range(0f, 1f)] public float groundHeight = 0.12f;
    [Tooltip("How tall he is, as a fraction of the screen height")] [Range(0.05f, 0.6f)] public float heightFraction = 0.24f;

    [Header("Speed (screen widths per second)")]
    public float walkSpeed = 0.11f;
    public float runSpeed = 0.22f;
    [Tooltip("How fast he slides forward while falling")] public float stumbleSpeed = 0.08f;

    [Header("Where things happen (screen position, 0 = left edge, 1 = right edge)")]
    [Tooltip("He stops and checks his watch here, around the middle of the screen")] [Range(0f, 1f)] public float watchX = 0.5f;
    [Tooltip("He stumbles here")] [Range(0f, 1f)] public float stumbleX = 0.85f;

    [Header("Timing (seconds)")]
    public float watchTime = 1.8f;
    public float pauseBetweenLoops = 1.0f;

    const string WalkState = "Towelman_Rig|Walk";
    const string WatchState = "Towelman_Rig|Idle_Watch";
    const string RunState = "Towelman_Rig|Run";
    const string StumbleState = "Towelman_Rig|Stumble_Fall";
    const float StartX = -0.15f;
    const float EndX = 1.15f;

    float x;
    float speed;
    float feetOffset;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cam == null || animator == null) { enabled = false; return; }

        animator.applyRootMotion = false;
        Fit();
        StartCoroutine(Loop());
    }

    // scale him to the screen and work out how far the feet are below the origin
    void Fit()
    {
        float visibleHeight = 2f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Bounds b = GetBounds();
        if (b.size.y > 0.001f)
            transform.localScale *= visibleHeight * heightFraction / b.size.y;

        transform.rotation = Quaternion.identity;
        b = GetBounds();
        feetOffset = transform.position.y - b.min.y;
    }

    Bounds GetBounds()
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    void Update()
    {
        x += speed * Time.deltaTime;
        Vector3 p = cam.ViewportToWorldPoint(new Vector3(x, groundHeight, depth));
        p.y += feetOffset;
        transform.position = p;
    }

    IEnumerator Loop()
    {
        while (true)
        {
            x = StartX;
            Play(WalkState, 0f, 0.4f);
            speed = walkSpeed;
            while (x < watchX) yield return null;

            // check the watch
            speed = 0f;
            Play(WatchState, 0.15f, 0f);
            yield return new WaitForSeconds(watchTime);

            // oh no, run
            Play(RunState, 0.15f, 5f);
            speed = runSpeed;
            while (x < stumbleX) yield return null;

            // stumble, lie there a moment, get up
            Play(StumbleState, 0.1f, 0f);
            speed = stumbleSpeed;
            yield return null;
            float fall = Mathf.Max(1f, animator.GetCurrentAnimatorStateInfo(0).length);
            yield return new WaitForSeconds(fall * 0.5f);
            speed = 0f;
            yield return new WaitForSeconds(fall * 0.5f);

            // keep running off screen
            Play(RunState, 0.2f, 5f);
            speed = runSpeed;
            while (x < EndX) yield return null;

            speed = 0f;
            yield return new WaitForSeconds(pauseBetweenLoops);
        }
    }

    // "Speed" is the same parameter the player uses: between 0.1 and 0.7 = walk, above 0.7 = run.
    // Without it the controller would drop back to idle right after the walk starts.
    void Play(string state, float fade, float speedParam)
    {
        animator.SetFloat("Speed", speedParam);
        animator.SetBool("Grounded", true);
        int hash = Animator.StringToHash(state);
        if (!animator.HasState(0, hash)) return;
        if (fade <= 0f) animator.Play(hash, 0, 0f);
        else animator.CrossFadeInFixedTime(hash, fade);
    }
}
