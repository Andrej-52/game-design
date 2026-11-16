using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The game over screen: the screen fades to black, a blood red title slowly appears (Dark Souls style),
// and the player can restart the level or go back to the main menu.
// Add it with Tools > Towelman > Add Game Over Screen. It is built from code when the scene starts.
public class GameOverUI : MonoBehaviour
{
    public PlayerHealth player;

    [Header("Set by the setup menu (same look as the main menu)")]
    public Sprite buttonSprite;
    public TMP_FontAsset font;

    [Header("Text")]
    public string title = "YOU DIDN'T MAKE IT TO WORK";
    public string subtitleTimeUp = "The clock ran out. Your boss noticed.";
    public string subtitleHurt = "The towel was not enough.";

    [Header("Timing")]
    public string menuScene = "MenuScene";
    public float showDelay = 1.4f;     // lets the death animation play first

    static readonly Color Blood = new Color(0.60f, 0.05f, 0.06f);
    static readonly Color DarkRed = new Color(0.36f, 0.04f, 0.05f);
    static readonly Color Bone = new Color(0.92f, 0.82f, 0.76f);

    CanvasGroup dim, banner, lower;
    TextMeshProUGUI titleText, subtitleText;
    bool shown, ready;

    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        // buttons need an EventSystem (the tutorial scene doesn't have one)
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        Build();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerHealth>();
        }

        if (player == null)
        {
            Debug.LogWarning("GameOverUI: no PlayerHealth found on the Player.");
            enabled = false;
        }
    }

    void Update()
    {
        if (!shown && player.IsDead)
        {
            shown = true;
            StartCoroutine(Show());
        }

        if (ready)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return)) Restart();
            if (Input.GetKeyDown(KeyCode.Escape)) BackToStart();
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }

    IEnumerator Show()
    {
        LevelTimer timer = FindFirstObjectByType<LevelTimer>();
        subtitleText.text = timer != null && timer.TimeIsUp ? subtitleTimeUp : subtitleHurt;

        yield return new WaitForSecondsRealtime(showDelay);

        dim.blocksRaycasts = true;
        yield return Fade(dim, 1f, 1.2f);

        // the title fades in while the letters slowly drift apart
        float t = 0f;
        const float dur = 2.2f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            banner.alpha = Mathf.Min(1f, k * 2f);
            titleText.characterSpacing = Mathf.Lerp(0f, 10f, 1f - (1f - k) * (1f - k));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.3f);

        lower.interactable = true;
        lower.blocksRaycasts = true;
        yield return Fade(lower, 1f, 0.8f);
        ready = true;
    }

    static IEnumerator Fade(CanvasGroup g, float to, float duration)
    {
        float from = g.alpha, t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        g.alpha = to;
    }

    // ---------- building the screen ----------

    void Build()
    {
        RectTransform root = (RectTransform)transform;

        // black screen
        RectTransform dimRt = Rect("Dim", root);
        Stretch(dimRt);
        Image dimImg = dimRt.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.93f);
        dim = Group(dimRt);
        dim.alpha = 0f;               // invisible until the player dies
        dim.blocksRaycasts = false;

        // dark band with thin red lines for the title
        RectTransform bannerRt = Rect("Banner", root);
        bannerRt.anchorMin = new Vector2(0f, 0.5f);
        bannerRt.anchorMax = new Vector2(1f, 0.5f);
        bannerRt.anchoredPosition = new Vector2(0f, 120f);
        bannerRt.sizeDelta = new Vector2(0f, 220f);
        Image bannerImg = bannerRt.gameObject.AddComponent<Image>();
        bannerImg.color = new Color(0f, 0f, 0f, 0.85f);
        bannerImg.raycastTarget = false;
        Line(bannerRt, 1f);
        Line(bannerRt, 0f);

        RectTransform titleRt = Rect("Title", bannerRt);
        Stretch(titleRt);
        titleRt.offsetMin = new Vector2(120f, 0f);
        titleRt.offsetMax = new Vector2(-120f, 0f);
        titleText = Text(titleRt, title, 96f, Blood, 0f);
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 40f;
        titleText.fontSizeMax = 96f;
        banner = Group(bannerRt);
        banner.alpha = 0f;

        // subtitle + buttons
        RectTransform lowerRt = Rect("Lower", root);
        Stretch(lowerRt);
        lower = Group(lowerRt);
        lower.alpha = 0f;
        lower.interactable = false;
        lower.blocksRaycasts = false;

        RectTransform subRt = Rect("Subtitle", lowerRt);
        subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.5f);
        subRt.anchoredPosition = new Vector2(0f, -45f);
        subRt.sizeDelta = new Vector2(1200f, 60f);
        subtitleText = Text(subRt, subtitleHurt, 36f, new Color(0.62f, 0.50f, 0.47f), 3f);

        MakeButton(lowerRt, "RESTART", new Vector2(0f, -150f), Restart);
        MakeButton(lowerRt, "BACK TO START", new Vector2(0f, -250f), BackToStart);
    }

    void MakeButton(RectTransform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rt = Rect(label + " Button", parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(460f, 84f);

        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = buttonSprite;
        img.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = Color.white;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.normalColor = DarkRed;
        colors.highlightedColor = new Color(0.70f, 0.10f, 0.10f);
        colors.selectedColor = DarkRed;
        colors.pressedColor = new Color(0.25f, 0.02f, 0.03f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform textRt = Rect("Text", rt);
        Stretch(textRt);
        Text(textRt, label, 34f, Bone, 3f).raycastTarget = false;
    }

    void Line(RectTransform banner, float anchorY)
    {
        RectTransform line = Rect("Line", banner);
        line.anchorMin = new Vector2(0f, anchorY);
        line.anchorMax = new Vector2(1f, anchorY);
        line.sizeDelta = new Vector2(0f, 3f);
        line.anchoredPosition = Vector2.zero;
        Image img = line.gameObject.AddComponent<Image>();
        img.color = DarkRed;
        img.raycastTarget = false;
    }

    TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color, float spacing)
    {
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = spacing;
        tmp.raycastTarget = false;
        return tmp;
    }

    static CanvasGroup Group(RectTransform rt)
    {
        return rt.gameObject.AddComponent<CanvasGroup>();
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
