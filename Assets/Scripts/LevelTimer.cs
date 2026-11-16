using UnityEngine;
using UnityEngine.Events;

// Counts down for the level. When it reaches zero the player dies (same death + restart as running out of hearts).
// Put one in every scene and set the time limit there: 600 = 10 minutes for the tutorial, something else later.
public class LevelTimer : MonoBehaviour
{
    [Tooltip("Total time for this level in seconds (600 = 10 minutes)")]
    public float timeLimitSeconds = 600f;
    public bool startOnPlay = true;

    [Tooltip("Fires once when the time runs out (the player is killed automatically)")]
    public UnityEvent onTimeUp;

    public float Remaining { get; private set; }
    public bool Running { get; private set; }
    public bool TimeIsUp { get; private set; }

    // 1 = full time left, 0 = out of time
    public float Fraction => timeLimitSeconds > 0f ? Mathf.Clamp01(Remaining / timeLimitSeconds) : 0f;

    PlayerHealth player;

    void Start()
    {
        Remaining = timeLimitSeconds;
        Running = startOnPlay;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (!Running)
            return;

        // dying some other way stops the clock
        if (player != null && player.IsDead)
        {
            Running = false;
            return;
        }

        Remaining -= Time.deltaTime;

        if (Remaining <= 0f)
        {
            Remaining = 0f;
            Running = false;
            TimeIsUp = true;

            onTimeUp?.Invoke();
            if (player != null) player.Kill();
        }
    }

    public void StartTimer() { if (!TimeIsUp) Running = true; }
    public void StopTimer() { Running = false; }   // call this when the player reaches the office

    public void AddTime(float seconds)
    {
        Remaining = Mathf.Min(Remaining + seconds, timeLimitSeconds);
    }
}
