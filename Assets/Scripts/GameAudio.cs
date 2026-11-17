using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameAudio
{
    const string MasterKey = "volume_master";
    const string SoundKey = "volume_sound";
    const string MusicKey = "volume_music";
    const string MutedKey = "volume_muted";

    static float master = 0.8f;
    static float sound = 1f;
    static float music = 0.7f;
    static bool muted;

    public static event System.Action Changed;

    public static float Master
    {
        get => master;
        set { master = Mathf.Clamp01(value); Save(); }
    }

    public static float Sound
    {
        get => sound;
        set { sound = Mathf.Clamp01(value); Save(); }
    }

    public static float Music
    {
        get => music;
        set { music = Mathf.Clamp01(value); Save(); }
    }

    public static bool Muted
    {
        get => muted;
        set { muted = value; Save(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        master = PlayerPrefs.GetFloat(MasterKey, 0.8f);
        sound = PlayerPrefs.GetFloat(SoundKey, 1f);
        music = PlayerPrefs.GetFloat(MusicKey, 0.7f);
        muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        Apply();

        // sounds on the shared sources must not carry over into the next scene
        // (e.g. the game over sound after pressing restart / back to menu)
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopSounds();
    }

    public static void StopSounds()
    {
        if (sfxSource != null) sfxSource.Stop();
        if (SoundManager.Instance != null) SoundManager.Instance.Stop();
    }

    static void Save()
    {
        PlayerPrefs.SetFloat(MasterKey, master);
        PlayerPrefs.SetFloat(SoundKey, sound);
        PlayerPrefs.SetFloat(MusicKey, music);
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        Apply();
        Changed?.Invoke();
    }

    static void Apply()
    {
        AudioListener.volume = muted ? 0f : master;
    }

    static AudioSource sfxSource;
    static AudioClip alarmRing;

    // the alarm clock ring (Assets/Resources/AlarmRing.wav), used by the intro and by the watch when time is running out
    public static AudioClip AlarmRing
    {
        get
        {
            if (alarmRing == null) alarmRing = Resources.Load<AudioClip>("AlarmRing");
            return alarmRing;
        }
    }

    public static void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        if (sfxSource == null)
        {
            GameObject go = new GameObject("GameAudio SFX");
            Object.DontDestroyOnLoad(go);
            sfxSource = go.AddComponent<AudioSource>();
        }

        sfxSource.PlayOneShot(clip, volume * sound);
    }
}
