using UnityEngine;

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
}
