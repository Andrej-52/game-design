using UnityEngine;
using UnityEngine.SceneManagement;

// Hurts the player and puts him back where he stood a moment ago (used by the bus).
// Falls back to restarting the scene if the player has no PlayerHealth.
public class RestartOnTouch : MonoBehaviour
{
    public int damage = 1;

    [Header("Sound")]
    public AudioClip hitSound;   // e.g. a honk for the bus
    [Range(0f, 1f)] public float hitSoundVolume = 1f;

    AudioSource audioSource;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayHitSound();

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

    void PlayHitSound()
    {
        if (hitSound == null)
            return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        audioSource.PlayOneShot(hitSound, hitSoundVolume * GameAudio.Sound);
    }
}