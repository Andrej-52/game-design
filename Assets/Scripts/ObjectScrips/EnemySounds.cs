using UnityEngine;

public class EnemySounds : MonoBehaviour
{
    public AudioClip[] hurtClips;
    public AudioClip deathClip;
    [Range(0f, 1f)] public float volume = 1f;

    EnemyHealth health;

    void Awake()
    {
        health = GetComponentInParent<EnemyHealth>();
        if (health == null)
            health = GetComponentInChildren<EnemyHealth>();
    }

    void OnEnable()
    {
        if (health == null)
            return;

        health.onHurt.AddListener(PlayHurt);
        health.onDeath.AddListener(PlayDeath);
    }

    void OnDisable()
    {
        if (health == null)
            return;

        health.onHurt.RemoveListener(PlayHurt);
        health.onDeath.RemoveListener(PlayDeath);
    }

    public void PlayHurt()
    {
        if (hurtClips != null && hurtClips.Length > 0)
            GameAudio.PlaySound(hurtClips[Random.Range(0, hurtClips.Length)], volume);
    }

    public void PlayDeath()
    {
        if (deathClip != null)
            GameAudio.PlaySound(deathClip, volume);
        else
            PlayHurt();
    }
}
