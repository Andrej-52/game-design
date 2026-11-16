using UnityEngine;

// The finish line: put it on a trigger collider at the office entrance.
// When the player walks in, the clock stops and the victory screen shows.
// Added by Tools > Towelman > Add Victory Screen.
[RequireComponent(typeof(BoxCollider))]
public class OfficeGoal : MonoBehaviour
{
    public VictoryUI victory;
    public LevelTimer timer;

    bool reached;

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (reached) return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead) return;

        reached = true;

        float timeLeft = -1f;
        if (timer == null) timer = FindFirstObjectByType<LevelTimer>();
        if (timer != null)
        {
            timer.StopTimer();
            timeLeft = timer.Remaining;
        }

        // he stands still and nothing can hurt him now
        player.Immune = true;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;
        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        if (victory == null) victory = FindFirstObjectByType<VictoryUI>();
        if (victory != null) victory.Show(timeLeft);
        else Debug.Log("Reached the office! (add a VictoryUI with Tools > Towelman > Add Victory Screen)");
    }

    void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
