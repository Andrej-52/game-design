using UnityEngine;
using UnityEngine.SceneManagement;

// Hurts the player and puts him back where he stood a moment ago (used by the bus).
// Falls back to restarting the scene if the player has no PlayerHealth.
public class RestartOnTouch : MonoBehaviour
{
    public int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            health.HitByHazard(damage);
        }
        else
        {
            SceneManager.LoadScene(
                SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}