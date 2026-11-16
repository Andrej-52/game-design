using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicVolume : MonoBehaviour
{
    [Range(0f, 1f)] public float baseVolume = 1f;

    AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        GameAudio.Changed += Apply;
        Apply();
    }

    void OnDisable()
    {
        GameAudio.Changed -= Apply;
    }

    void Apply()
    {
        source.volume = baseVolume * GameAudio.Music;
    }
}
