using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public AudioClip jump;
    public AudioClip hit;
    public AudioClip click;

    AudioSource source;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
    }

    public void Play(AudioClip clip)
    {
        if (clip != null)
            source.PlayOneShot(clip);
    }

    public void PlayClick()
    {
        Play(click);
    }
}