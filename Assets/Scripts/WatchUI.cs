using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A cartoony wrist watch in the top right corner that shows the LevelTimer.
// The red wedge is the time left and the hand sweeps around once during the whole level.
// The last seconds make the watch shake and flash. Everything is drawn from code, no art needed.
public class WatchUI : MonoBehaviour
{
    public LevelTimer timer;

    [Header("Look")]
    public float panicSeconds = 10f;   // watch starts shaking and flashing below this

    static readonly Color Strap = new Color(0.30f, 0.14f, 0.06f);
    static readonly Color StrapDark = new Color(0.18f, 0.08f, 0.03f);
    static readonly Color Bezel = new Color(0.10f, 0.10f, 0.12f);
    static readonly Color Face = new Color(0.99f, 0.97f, 0.90f);
    static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f);
    static readonly Color Gold = new Color(1f, 0.78f, 0.15f);
    static readonly Color Danger = new Color(0.95f, 0.15f, 0.15f);

    RectTransform watch;
    Image bezel, arc;
    RectTransform hand;
    TextMeshProUGUI label;
    Vector2 basePosition;

    void Start()
    {
        if (timer == null) timer = FindFirstObjectByType<LevelTimer>();
        if (timer == null)
        {
            Debug.LogWarning("WatchUI: no LevelTimer in the scene.");
            enabled = false;
            return;
        }

        Build();
    }

    void Build()
    {
        Sprite circle = MakeCircle(128);
        Sprite rounded = MakeRoundedRect(64, 22);

        RectTransform root = (RectTransform)transform;

        // everything that moves together (shake) lives in "watch"
        watch = Rect("Watch", root, Vector2.zero, new Vector2(520f, 330f));
        basePosition = watch.anchoredPosition;

        // the arm comes in from the right edge of the screen, the watch sits on the wrist
        Vector2 armPos = new Vector2(0f, 35f);
        Vector2 w = armPos + new Vector2(78f, 0f);   // center of the watch

        Img("Arm", watch, MakeArm(), Color.white, armPos, new Vector2(520f, 260f));

        Image strap = Img("Strap", watch, rounded, Strap, w, new Vector2(54f, 124f));
        strap.type = Image.Type.Sliced;
        foreach (float y in new[] { 52f, -52f })
            Img("Dot", watch, circle, StrapDark, w + new Vector2(0f, y), new Vector2(7f, 7f));

        bezel = Img("Bezel", watch, circle, Bezel, w, new Vector2(100f, 100f));
        Img("Face", watch, circle, Face, w, new Vector2(86f, 86f));

        arc = Img("TimeLeft", watch, circle, Color.green, w, new Vector2(78f, 78f));
        arc.type = Image.Type.Filled;
        arc.fillMethod = Image.FillMethod.Radial360;
        arc.fillOrigin = (int)Image.Origin360.Top;
        arc.fillClockwise = false;   // the wedge's edge is always where the hand points

        // 12 ticks
        for (int i = 0; i < 12; i++)
        {
            bool big = i % 3 == 0;
            RectTransform holder = Rect("Tick", watch, w, Vector2.zero);
            holder.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            Img("Mark", holder, null, Ink, new Vector2(0f, 35f), new Vector2(big ? 5f : 3f, big ? 11f : 6f));
        }

        // hand
        Image handImg = Img("Hand", watch, rounded, Ink, w, new Vector2(6f, 36f));
        handImg.type = Image.Type.Sliced;
        hand = handImg.rectTransform;
        hand.pivot = new Vector2(0.5f, 0.1f);
        Img("Pin", watch, circle, Gold, w, new Vector2(13f, 13f));

        // digital time below the watch
        GameObject textGo = new GameObject("Time", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(watch, false);
        RectTransform tr = (RectTransform)textGo.transform;
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = new Vector2(78f, -105f);
        tr.sizeDelta = new Vector2(260f, 70f);

        label = textGo.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 62f;
        label.raycastTarget = false;
        label.outlineWidth = 0.28f;
        label.outlineColor = new Color32(20, 12, 8, 255);
    }

    void Update()
    {
        float f = timer.Fraction;
        float remaining = timer.Remaining;

        arc.fillAmount = f;
        arc.color = Color.Lerp(Danger, new Color(0.35f, 0.85f, 0.45f), Mathf.Clamp01(f * 2f));
        hand.localRotation = Quaternion.Euler(0f, 0f, -(1f - f) * 360f);

        int total = Mathf.CeilToInt(remaining);
        label.text = (total / 60) + ":" + (total % 60).ToString("00");

        bool panic = remaining <= panicSeconds && !timer.TimeIsUp && timer.Running;
        if (panic)
        {
            float beat = Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI));   // once per second
            bool flash = Mathf.Repeat(Time.time * 4f, 1f) < 0.5f;

            watch.localScale = Vector3.one * (1f + 0.07f * beat);
            watch.anchoredPosition = basePosition + new Vector2(Mathf.Sin(Time.time * 70f), Mathf.Cos(Time.time * 55f)) * 3f;
            bezel.color = flash ? Danger : Bezel;
            label.color = flash ? Color.white : new Color(1f, 0.55f, 0.5f);
        }
        else
        {
            watch.localScale = Vector3.one;
            watch.anchoredPosition = basePosition;
            bezel.color = timer.TimeIsUp ? Danger : Bezel;
            label.color = Color.white;
        }
    }

    // ---------- small helpers ----------

    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(string name, Transform parent, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(name, parent, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // forearm coming in from the right with a clenched fist on the left (drawn with distance shapes)
    static Sprite MakeArm()
    {
        const int w = 512, h = 256;
        Color skin = new Color(0.93f, 0.62f, 0.40f);
        Color shade = new Color(0.78f, 0.47f, 0.29f);
        Color line = new Color(0.55f, 0.32f, 0.20f);

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                // x runs 0..2 and y 0..1 (y up)
                float x = (px + 0.5f) / h, y = (py + 0.5f) / h;

                float half = 0.19f + 0.025f * Mathf.Clamp01((x - 0.78f) / 1.2f);
                float forearm = Mathf.Max(Mathf.Abs(x - 1.49f) - 0.71f, Mathf.Abs(y - 0.5f) - half);

                float ex = (x - 0.47f) / 0.36f, ey = (y - 0.47f) / 0.33f;
                float fist = (Mathf.Sqrt(ex * ex + ey * ey) - 1f) * 0.33f;

                float bumps = Circle(x, y, 0.17f, 0.28f, 0.085f);
                bumps = Mathf.Min(bumps, Circle(x, y, 0.15f, 0.40f, 0.085f));
                bumps = Mathf.Min(bumps, Circle(x, y, 0.15f, 0.52f, 0.085f));
                bumps = Mathf.Min(bumps, Circle(x, y, 0.18f, 0.64f, 0.085f));
                bumps = Mathf.Min(bumps, Circle(x, y, 0.42f, 0.78f, 0.13f));   // thumb knuckle

                float d = SmoothMin(fist, bumps, 0.05f);
                d = SmoothMin(d, forearm, 0.09f);

                float alpha = Mathf.Clamp01(0.5f - d * h);
                Color c = skin;

                // a little shading towards the bottom and thin lines between the fingers
                c = Color.Lerp(c, shade, Mathf.Clamp01((0.45f - y) * 1.4f));
                if (x > 0.2f && x < 0.45f && (Mathf.Abs(y - 0.34f) < 0.006f || Mathf.Abs(y - 0.46f) < 0.006f || Mathf.Abs(y - 0.58f) < 0.006f))
                    c = line;

                c.a = alpha;
                tex.SetPixel(px, py, c);
            }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    static float Circle(float x, float y, float cx, float cy, float r)
    {
        return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
    }

    static float SmoothMin(float a, float b, float k)
    {
        float hh = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
        return Mathf.Min(a, b) - hh * hh * k * 0.25f;
    }

    static Sprite MakeCircle(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite MakeRoundedRect(int size, int radius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
            }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
}
