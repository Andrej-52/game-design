using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pause menu for the levels: a small pause button at the top of the screen (or Esc / P) opens
// CONTINUE, SETTINGS, RESTART and BACK TO MAIN MENU. Settings has the same volume controls as the main menu.
// Add it with Tools > Towelman > Add Pause Menu. It is built from code when the scene starts.
public class PauseUI : MonoBehaviour
{
    [Header("Set by the setup menu (same look as the main menu)")]
    public Sprite buttonSprite;
    public Sprite panelSprite;
    public TMP_FontAsset font;

    public string menuScene = "MenuScene";

    static readonly Color Navy = new Color32(0x1B, 0x2A, 0x5C, 0xFF);
    static readonly Color Blue = new Color32(0x24, 0x68, 0xC0, 0xFF);
    static readonly Color BlueHover = new Color32(0x3D, 0x86, 0xE6, 0xFF);
    static readonly Color BluePressed = new Color32(0x1A, 0x4E, 0x96, 0xFF);
    static readonly Color Yellow = new Color32(0xFF, 0xC8, 0x3D, 0xFF);
    static readonly Color Cream = new Color32(0xFD, 0xF7, 0xE6, 0xFF);
    static readonly Color Ink = new Color32(0x1A, 0x1A, 0x1F, 0xFF);

    GameObject overlay, mainPage, settingsPage;
    CanvasGroup pauseButtonGroup;
    Button muteButton;
    TextMeshProUGUI muteLabel;
    bool paused, inSettings;

    PlayerHealth player;
    VictoryUI victory;
    MonoBehaviour[] frozen;
    bool[] frozenWasOn;

    void Awake()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;     // below the game over / victory screens

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        Build();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.GetComponent<PlayerHealth>();
        victory = FindFirstObjectByType<VictoryUI>();
    }

    bool CanPause
    {
        get
        {
            if (player != null && player.IsDead) return false;
            if (victory != null && victory.IsShown) return false;
            if (IntroSequence.Playing) return false;
            return true;
        }
    }

    void Update()
    {
        // the pause button is only shown when pausing makes sense
        bool show = !paused && CanPause;
        pauseButtonGroup.alpha = show ? 1f : 0f;
        pauseButtonGroup.blocksRaycasts = show;
        pauseButtonGroup.interactable = show;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (paused) { if (inSettings) ShowPage(false); else Resume(); }
            else if (CanPause) Pause();
        }
    }

    void OnDestroy()
    {
        if (paused) Time.timeScale = 1f;
    }

    // ---------- actions ----------

    public void Pause()
    {
        if (paused) return;
        paused = true;
        Time.timeScale = 0f;

        // stop the player from jumping / whipping while the game is frozen
        frozen = new MonoBehaviour[]
        {
            FindFirstObjectByType<PlayerMovement>(), FindFirstObjectByType<PlayerAttack>(),
        };
        frozenWasOn = new bool[frozen.Length];
        for (int i = 0; i < frozen.Length; i++)
        {
            if (frozen[i] == null) continue;
            frozenWasOn[i] = frozen[i].enabled;
            frozen[i].enabled = false;
        }

        ShowPage(false);
        overlay.SetActive(true);
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;
        Time.timeScale = 1f;
        overlay.SetActive(false);

        if (frozen != null)
            for (int i = 0; i < frozen.Length; i++)
                if (frozen[i] != null) frozen[i].enabled = frozenWasOn[i];
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }

    void ShowPage(bool settings)
    {
        inSettings = settings;
        mainPage.SetActive(!settings);
        settingsPage.SetActive(settings);
    }

    void ToggleMute()
    {
        GameAudio.Muted = !GameAudio.Muted;
        RefreshMute();
    }

    void RefreshMute()
    {
        muteLabel.text = GameAudio.Muted ? "MUTE: ON" : "MUTE: OFF";
    }

    // ---------- building ----------

    void Build()
    {
        RectTransform root = (RectTransform)transform;

        // pause button, top centre (hearts are top left, the watch top right)
        RectTransform pb = Rect("Pause Button", root);
        pb.anchorMin = pb.anchorMax = new Vector2(0.5f, 1f);
        pb.pivot = new Vector2(0.5f, 1f);
        pb.anchoredPosition = new Vector2(0f, -28f);
        pb.sizeDelta = new Vector2(84f, 84f);
        Image pbImg = SpriteImage(pb, Blue);
        Button pbBtn = StyledButton(pb, pbImg);
        pbBtn.onClick.AddListener(Pause);
        pauseButtonGroup = pb.gameObject.AddComponent<CanvasGroup>();
        for (int i = 0; i < 2; i++)
        {
            RectTransform bar = Rect("Bar", pb);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.anchoredPosition = new Vector2(i == 0 ? -10f : 10f, 0f);
            bar.sizeDelta = new Vector2(12f, 34f);
            Image bi = bar.gameObject.AddComponent<Image>();
            bi.color = Color.white;
            bi.raycastTarget = false;
        }

        // the pause screen
        RectTransform ov = Rect("Overlay", root);
        Stretch(ov);
        overlay = ov.gameObject;
        Image dim = overlay.AddComponent<Image>();
        dim.color = new Color(0.07f, 0.12f, 0.30f, 0.7f);

        RectTransform card = Rect("Card", ov);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(760f, 800f);
        Image cardImg = card.gameObject.AddComponent<Image>();
        cardImg.sprite = panelSprite;
        cardImg.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        cardImg.color = panelSprite != null ? Color.white : Navy;

        // main page
        mainPage = Page("Main Page", card);
        Label(mainPage.transform, "PAUSED", 96f, Yellow, new Vector2(0f, 300f), new Vector2(680f, 120f));
        MakeButton(mainPage.transform, "CONTINUE", new Vector2(0f, 150f), Resume);
        MakeButton(mainPage.transform, "SETTINGS", new Vector2(0f, 40f), () => ShowPage(true));
        MakeButton(mainPage.transform, "RESTART", new Vector2(0f, -70f), Restart);
        MakeButton(mainPage.transform, "BACK TO MAIN MENU", new Vector2(0f, -180f), BackToMenu);
        Label(mainPage.transform, "Esc or P to continue", 26f, new Color(0.1f, 0.1f, 0.12f, 0.6f), new Vector2(0f, -300f), new Vector2(680f, 40f));

        // settings page
        settingsPage = Page("Settings Page", card);
        Label(settingsPage.transform, "SETTINGS", 80f, Yellow, new Vector2(0f, 300f), new Vector2(680f, 110f));
        VolumeRow(settingsPage.transform, "MASTER", 160f, GameAudio.Master, v => GameAudio.Master = v);
        VolumeRow(settingsPage.transform, "SOUND", 60f, GameAudio.Sound, v => GameAudio.Sound = v);
        VolumeRow(settingsPage.transform, "MUSIC", -40f, GameAudio.Music, v => GameAudio.Music = v);
        muteButton = MakeButton(settingsPage.transform, "", new Vector2(0f, -150f), ToggleMute, out muteLabel);
        RefreshMute();
        MakeButton(settingsPage.transform, "BACK", new Vector2(0f, -270f), () => ShowPage(false));

        overlay.SetActive(false);
    }

    GameObject Page(string name, RectTransform parent)
    {
        RectTransform rt = Rect(name, parent);
        Stretch(rt);
        return rt.gameObject;
    }

    void VolumeRow(Transform parent, string label, float y, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        Label(parent, label, 34f, Ink, new Vector2(-250f, y), new Vector2(200f, 50f), TextAlignmentOptions.Left);

        RectTransform sr = Rect(label + " Slider", (RectTransform)parent);
        sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
        sr.anchoredPosition = new Vector2(120f, y);
        sr.sizeDelta = new Vector2(380f, 40f);

        RectTransform bg = Rect("Background", sr);
        bg.anchorMin = new Vector2(0f, 0.3f);
        bg.anchorMax = new Vector2(1f, 0.7f);
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        SpriteImage(bg, new Color(0f, 0f, 0f, 0.35f)).raycastTarget = true;

        RectTransform fillArea = Rect("Fill Area", sr);
        fillArea.anchorMin = new Vector2(0f, 0.3f);
        fillArea.anchorMax = new Vector2(1f, 0.7f);
        fillArea.offsetMin = new Vector2(8f, 0f);
        fillArea.offsetMax = new Vector2(-8f, 0f);
        RectTransform fill = Rect("Fill", fillArea);
        fill.sizeDelta = new Vector2(10f, 0f);
        SpriteImage(fill, Yellow);

        RectTransform slide = Rect("Handle Slide Area", sr);
        Stretch(slide);
        slide.offsetMin = new Vector2(18f, 0f);
        slide.offsetMax = new Vector2(-18f, 0f);
        RectTransform handle = Rect("Handle", slide);
        handle.sizeDelta = new Vector2(36f, 0f);
        Image handleImg = SpriteImage(handle, Color.white);

        Slider slider = sr.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(onChange);
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        return MakeButton(parent, label, pos, onClick, out _);
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI text)
    {
        RectTransform rt = Rect(label + " Button", (RectTransform)parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560f, 88f);
        Image img = SpriteImage(rt, Blue);
        Button b = StyledButton(rt, img);
        b.onClick.AddListener(onClick);

        RectTransform textRt = Rect("Text", rt);
        Stretch(textRt);
        text = Text(textRt, label, 34f, Color.white, TextAlignmentOptions.Center);
        return b;
    }

    Button StyledButton(RectTransform rt, Image img)
    {
        img.color = Color.white;
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock c = b.colors;
        c.normalColor = Blue;
        c.highlightedColor = BlueHover;
        c.selectedColor = Blue;
        c.pressedColor = BluePressed;
        c.fadeDuration = 0.08f;
        b.colors = c;
        return b;
    }

    Image SpriteImage(RectTransform rt, Color color)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = buttonSprite;
        img.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        return img;
    }

    void Label(Transform parent, string text, float size, Color color, Vector2 pos, Vector2 box,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform rt = Rect("Label", (RectTransform)parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        Text(rt, text, size, color, align);
    }

    TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
    {
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
