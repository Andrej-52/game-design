using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VolumeSettingsUI : MonoBehaviour
{
    public Slider masterSlider;
    public Slider soundSlider;
    public Slider musicSlider;
    public Toggle muteToggle;

    void Awake()
    {
        if (masterSlider == null) masterSlider = Find<Slider>("MASTER Slider");
        if (soundSlider == null) soundSlider = Find<Slider>("SOUND Slider");
        if (musicSlider == null) musicSlider = Find<Slider>("MUSIC Slider");
        if (muteToggle == null) muteToggle = Find<Toggle>("Mute Toggle");

        MakeClickable(masterSlider);
        MakeClickable(soundSlider);
        MakeClickable(musicSlider);
    }

    static void MakeClickable(Slider slider)
    {
        if (slider == null)
            return;

        foreach (Graphic graphic in slider.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = true;
    }

    void OnEnable()
    {
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(GameAudio.Master);
            masterSlider.onValueChanged.AddListener(SetMaster);
        }

        if (soundSlider != null)
        {
            soundSlider.SetValueWithoutNotify(GameAudio.Sound);
            soundSlider.onValueChanged.AddListener(SetSound);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(GameAudio.Music);
            musicSlider.onValueChanged.AddListener(SetMusic);
        }

        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(GameAudio.Muted);
            muteToggle.onValueChanged.AddListener(SetMuted);
        }
    }

    void OnDisable()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMaster);
        if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(SetSound);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusic);
        if (muteToggle != null) muteToggle.onValueChanged.RemoveListener(SetMuted);
    }

    void SetMaster(float value) => GameAudio.Master = value;
    void SetSound(float value) => GameAudio.Sound = value;
    void SetMusic(float value) => GameAudio.Music = value;
    void SetMuted(bool value) => GameAudio.Muted = value;

    T Find<T>(string objectName) where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
        {
            if (component.name == objectName)
                return component;
        }
        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitFirstScene()
    {
        AutoAttach();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AutoAttach();
    }

    static void AutoAttach()
    {
        if (FindAnyObjectByType<VolumeSettingsUI>(FindObjectsInactive.Include) != null)
            return;

        foreach (Slider slider in FindObjectsByType<Slider>(FindObjectsInactive.Include))
        {
            if (slider.name != "MASTER Slider")
                continue;

            Canvas canvas = slider.GetComponentInParent<Canvas>(true);
            if (canvas != null)
                canvas.rootCanvas.gameObject.AddComponent<VolumeSettingsUI>();
            return;
        }
    }
}
