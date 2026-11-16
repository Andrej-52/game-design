using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The victory screen shown when the player reaches the office: a card in the same style as the main menu,
// with confetti, the time left, and buttons to go back to the start or play again.
// Add it with Tools > Towelman > Add Victory Screen. It is built from code when the scene starts.
public class VictoryUI : MonoBehaviour
{
    [Header("Set by the setup menu (same look as the main menu)")]
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public TMP_FontAsset font;

    [Header("Text")]
    public string title = "YOU MADE IT!";
    public string subtitle = "Just in time for work.";

    [Header("Other")]
    public string menuScene = "MenuScene";
    public int confettiCount = 70;

    static readonly Color Navy = new Color32(0x1B, 0x2A, 0x5C, 0xFF);
    static readonly Color Blue = new Color32(0x24, 0x68, 0xC0, 0xFF);
    static readonly Color Yellow = new Color32(0xFF, 0xC8, 0x3D, 0xFF);

    class Piece { public RectTransform rect; public Vector2 vel; public float spin; }

    CanvasGroup screen;
    RectTransform card, confettiRoot;
    TextMeshProUGUI timeText;
    readonly List<Piece> confetti = new List<Piece>();
    bool shown, ready;

    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        Build();
        screen.alpha = 0f;
        screen.blocksRaycasts = false;
        screen.interactable = false;
    }

    void Update()
    {
        if (!shown) return;

        // confetti
        float h = ((RectTransform)transform).rect.height;
        foreach (Piece p in confetti)
        {
            p.rect.anchoredPosition += p.vel * Time.unscaledDeltaTime;
            p.rect.Rotate(0f, 0f, p.spin * Time.unscaledDeltaTime);
            if (p.rect.anchoredPosition.y < -h * 0.5f - 40f)
                p.rect.anchoredPosition = new Vector2(p.rect.anchoredPosition.x, h * 0.5f + 40f);
        }

        if (ready)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) BackToStart();
            if (Input.GetKeyDown(KeyCode.R)) PlayAgain();
        }
    }

    // timeLeft in seconds, or below 0 if there is no timer
    public void Show(float timeLeft)
    {
        if (shown) return;
        shown = true;

        if (timeLeft >= 0f)
        {
            int total = Mathf.FloorToInt(timeLeft);
            timeText.text = "TIME LEFT  " + (total / 60) + ":" + (total % 60).ToString("00");
        }
        else timeText.text = "";

        StartCoroutine(ShowRoutine());
    }

    public void BackToStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator ShowRoutine()
    {
        yield return new WaitForSecondsRealtime(0.6f);

        screen.blocksRaycasts = true;
        screen.interactable = true;

        float t = 0f;
        const float dur = 0.6f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            screen.alpha = k;
            card.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(k));
            yield return null;
        }

        screen.alpha = 1f;
        card.localScale = Vector3.one;
        ready = true;
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // ---------- building the screen ----------

    void Build()
    {
        RectTransform root = (RectTransform)transform;

        RectTransform screenRt = Rect("Screen", root);
        Stretch(screenRt);
        screen = screenRt.gameObject.AddComponent<CanvasGroup>();

        Image dim = screenRt.gameObject.AddComponent<Image>();
        dim.color = new Color(0.07f, 0.12f, 0.30f, 0.62f);

        confettiRoot = Rect("Confetti", screenRt);
        Stretch(confettiRoot);
        MakeConfetti();

        card = Rect("Card", screenRt);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(780f, 540f);
        Image cardImg = card.gameObject.AddComponent<Image>();
        cardImg.sprite = panelSprite;
        cardImg.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        cardImg.color = Color.white;

        TextMeshProUGUI titleText = Text(Place("Title", card, new Vector2(0f, 175f), new Vector2(700f, 110f)), title, 92f, Navy);
        titleText.fontStyle = FontStyles.Bold;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 40f;
        titleText.fontSizeMax = 92f;

        Text(Place("Subtitle", card, new Vector2(0f, 98f), new Vector2(700f, 50f)), subtitle, 36f, Blue);
        timeText = Text(Place("Time", card, new Vector2(0f, 38f), new Vector2(700f, 60f)), "", 44f, Navy);

        // the yellow strip under the title, like a little highlight
        RectTransform strip = Place("Strip", card, new Vector2(0f, 128f), new Vector2(220f, 8f));
        strip.gameObject.AddComponent<Image>().color = Yellow;
        strip.GetComponent<Image>().raycastTarget = false;

        MakeButton(card, "BACK TO START", new Vector2(0f, -70f), BackToStart);
        MakeButton(card, "PLAY AGAIN", new Vector2(0f, -170f), PlayAgain);
    }

    void MakeConfetti()
    {
        Color[] colors = { Yellow, Blue, new Color32(0xE8, 0x4A, 0x5F, 0xFF), new Color32(0x4C, 0xC9, 0x7A, 0xFF), Color.white };

        for (int i = 0; i < confettiCount; i++)
        {
            RectTransform rt = Rect("Piece", confettiRoot);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Random.Range(12f, 22f), Random.Range(8f, 14f));
            rt.anchoredPosition = new Vector2(Random.Range(-960f, 960f), Random.Range(-540f, 540f));
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Image img = rt.gameObject.AddComponent<Image>();
            img.color = colors[Random.Range(0, colors.Length)];
            img.raycastTarget = false;

            confetti.Add(new Piece
            {
                rect = rt,
                vel = new Vector2(Random.Range(-30f, 30f), -Random.Range(120f, 320f)),
                spin = Random.Range(-240f, 240f)
            });
        }
    }

    void MakeButton(RectTransform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rt = Place(label + " Button", parent, pos, new Vector2(460f, 84f));

        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = buttonSprite;
        img.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = Color.white;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.normalColor = Blue;
        colors.highlightedColor = new Color32(0x3D, 0x86, 0xE6, 0xFF);
        colors.selectedColor = Blue;
        colors.pressedColor = new Color32(0x1A, 0x4E, 0x96, 0xFF);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform textRt = Rect("Text", rt);
        Stretch(textRt);
        Text(textRt, label, 34f, Color.white);
    }

    TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color)
    {
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 3f;
        tmp.raycastTarget = false;
        return tmp;
    }

    static RectTransform Place(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
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
