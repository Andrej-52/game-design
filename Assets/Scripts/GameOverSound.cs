using System.Collections;
using UnityEngine;

public class GameOverSound : MonoBehaviour
{
    public AudioClip gameOverSound;
    [Range(0f, 1f)] public float volume = 1f;
    public float delay = 1.4f;
    public PlayerHealth player;

    [Header("Background music")]
    public bool stopMusic = true;
    public float musicFadeTime = 0.5f;

    bool played;

    void Start()
    {
        if (player != null)
            return;

        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
            player = found.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (played || player == null || !player.IsDead)
            return;

        played = true;

        if (stopMusic)
        {
            foreach (MusicVolume music in FindObjectsByType<MusicVolume>())
                StartCoroutine(FadeOut(music.GetComponent<AudioSource>()));
        }

        StartCoroutine(PlayAfterDelay());
    }

    IEnumerator FadeOut(AudioSource source)
    {
        if (source == null)
            yield break;

        float startVolume = source.volume;
        float time = 0f;

        while (time < musicFadeTime)
        {
            time += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, time / musicFadeTime);
            yield return null;
        }

        source.Stop();
    }

    IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delay);
        GameAudio.PlaySound(gameOverSound, volume);
    }
}
