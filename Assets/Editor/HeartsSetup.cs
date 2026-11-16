using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Towelman > Add Hearts HUD           : draws the heart sprites (if missing) and adds the hearts to the open scene
// Tools > Towelman > Regenerate Heart Sprites : redraws the sprites after a change in this file
public static class HeartsSetup
{
    const string Folder = "Assets/UI/Hearts";

    // the sprite is drawn in a space where the heart is about 2.2 wide and sits around the origin
    const int Out = 256;          // size of the finished sprite
    const int SS = 3;             // supersampling for smooth edges
    const int N = Out * SS;
    const float Span = 2.6f;      // how much of that space the sprite covers

    static readonly Color Outline = new Color(0.45f, 0.06f, 0.10f);
    static readonly Color RedTop = new Color(1f, 0.38f, 0.38f);
    static readonly Color RedBottom = new Color(0.86f, 0.14f, 0.20f);
    static readonly Color Rotten = new Color(0.48f, 0.09f, 0.15f);
    static readonly Color Bruise = new Color(0.72f, 0.68f, 0.22f);
    static readonly Color LidColor = new Color(0.58f, 0.13f, 0.17f);
    static readonly Color Bag = new Color(0.50f, 0.11f, 0.17f);
    static readonly Color EyeWhite = new Color(0.99f, 0.93f, 0.68f);
    static readonly Color Iris = new Color(0.30f, 0.65f, 0.20f);
    static readonly Color Pupil = new Color(0.07f, 0.07f, 0.09f);
    static readonly Color NoseColor = new Color(0.93f, 0.55f, 0.52f);
    static readonly Color Tooth_ = new Color(0.98f, 0.94f, 0.72f);
    static readonly Color Stache = new Color(0.13f, 0.13f, 0.14f);
    static readonly Color StacheShine = new Color(0.34f, 0.34f, 0.36f);

    [MenuItem("Tools/Towelman/Add Hearts HUD")]
    static void AddHud()
    {
        Sprite[] sprites = LoadOrMakeSprites(false);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create HUD");
        }

        HeartsUI existing = Object.FindFirstObjectByType<HeartsUI>();
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject hearts = new GameObject("Hearts", typeof(RectTransform), typeof(HeartsUI));
        hearts.transform.SetParent(canvas.transform, false);
        RectTransform rt = hearts.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40f, -40f);
        rt.sizeDelta = new Vector2(800f, 130f);

        HeartsUI ui = hearts.GetComponent<HeartsUI>();
        ui.heart = sprites[0];
        ui.leftHalf = sprites[1];
        ui.rightHalf = sprites[2];
        Undo.RegisterCreatedObjectUndo(hearts, "Create hearts");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Hearts HUD added. Press Play, and lose health to see them break.");
    }

    [MenuItem("Tools/Towelman/Regenerate Heart Sprites")]
    static void Regenerate()
    {
        Sprite[] sprites = LoadOrMakeSprites(true);

        HeartsUI ui = Object.FindFirstObjectByType<HeartsUI>();
        if (ui != null)
        {
            ui.heart = sprites[0];
            ui.leftHalf = sprites[1];
            ui.rightHalf = sprites[2];
            EditorUtility.SetDirty(ui);
        }
    }

    static Sprite[] LoadOrMakeSprites(bool force)
    {
        EnsureFolder("Assets/UI");
        EnsureFolder(Folder);

        string[] names = { "Heart", "HeartLeft", "HeartRight" };
        var result = new Sprite[3];

        for (int mode = 0; mode < 3; mode++)
        {
            string path = Folder + "/" + names[mode] + ".png";

            if (force || !File.Exists(path))
            {
                Texture2D tex = Render(mode);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            result[mode] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        return result;
    }

    // ---------- drawing ----------

    // mode 0 = whole heart, 1 = left half, 2 = right half (cut along a zig-zag crack)
    static Texture2D Render(int mode)
    {
        // the mustache: a curve with a thickness that gets thinner towards the tip, which curls up
        const int samples = 48;
        var stache = new Vector2[samples];
        var stacheR = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (samples - 1f);
            stache[i] = new Vector2(0.02f + 0.54f * t, -0.33f - 0.16f * Mathf.Sin(Mathf.PI * t) + 0.16f * t * t * t);
            stacheR[i] = 0.025f + 0.11f * Mathf.Pow(1f - t, 0.8f);
        }

        bool[] inside = new bool[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                inside[y * N + x] = HeartF(Warp(World(x, y))) < 0f;

        // ring of points used to find the pixels near the edge (the thick outline)
        int ringRadius = Mathf.RoundToInt(N * 0.027f);
        const int ringCount = 40;
        var ring = new Vector2Int[ringCount];
        for (int i = 0; i < ringCount; i++)
        {
            float a = i * Mathf.PI * 2f / ringCount;
            ring[i] = new Vector2Int(Mathf.RoundToInt(Mathf.Cos(a) * ringRadius), Mathf.RoundToInt(Mathf.Sin(a) * ringRadius));
        }

        var big = new Color[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                int i = y * N + x;
                if (!inside[i]) continue;

                Vector2 p = World(x, y);

                bool inner = true;
                foreach (Vector2Int o in ring)
                {
                    int nx = x + o.x, ny = y + o.y;
                    if (nx < 0 || ny < 0 || nx >= N || ny >= N || !inside[ny * N + nx]) { inner = false; break; }
                }

                Color c;
                if (!inner)
                {
                    c = Outline;
                }
                else
                {
                    c = Color.Lerp(RedBottom, RedTop, Mathf.InverseLerp(-1f, 1.2f, p.y));
                    c = Shine(c, p);
                    c = Face(c, p, stache, stacheR);
                }

                if (mode != 0)
                {
                    float split = Split(p.y);
                    bool leftSide = p.x < split;
                    if ((mode == 1) != leftSide) continue;               // belongs to the other half
                    if (Mathf.Abs(p.x - split) < 0.035f) c = Outline;   // dark edge along the crack
                }

                big[i] = c;
            }
        }

        // shrink with smoothing (averaging, weighted by alpha so edges don't get dark)
        var pixels = new Color[Out * Out];
        for (int y = 0; y < Out; y++)
        {
            for (int x = 0; x < Out; x++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int sy = 0; sy < SS; sy++)
                    for (int sx = 0; sx < SS; sx++)
                    {
                        Color c = big[(y * SS + sy) * N + x * SS + sx];
                        r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                    }

                pixels[y * Out + x] = a > 0f
                    ? new Color(r / a, g / a, b / a, a / (SS * SS))
                    : new Color(0f, 0f, 0f, 0f);
            }
        }

        Texture2D tex = new Texture2D(Out, Out, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static Vector2 World(int px, int py)
    {
        return new Vector2(((px + 0.5f) / N - 0.5f) * Span, ((py + 0.5f) / N - 0.5f) * Span + 0.12f);
    }

    // makes the heart lumpy and lopsided instead of perfect
    static Vector2 Warp(Vector2 p)
    {
        return p;
    }

    // inside the heart when this is below zero
    static float HeartF(Vector2 p)
    {
        float a = p.x * p.x + p.y * p.y - 1f;
        return a * a * a - p.x * p.x * p.y * p.y * p.y;
    }

    // x position of the crack at height y (zig-zag)
    static float Split(float y)
    {
        float t = y * 5.5f;
        float tri = Mathf.Abs(t - Mathf.Floor(t) - 0.5f) * 2f - 0.5f;
        return tri * 0.22f;
    }

    static Color Shine(Color c, Vector2 p)
    {
        float dx = p.x + 0.55f, dy = p.y - 0.66f;
        const float cos = 0.819f, sin = 0.574f;   // 35 degrees
        float xr = dx * cos + dy * sin;
        float yr = -dx * sin + dy * cos;
        if ((xr / 0.17f) * (xr / 0.17f) + (yr / 0.07f) * (yr / 0.07f) < 1f)
            return Color.Lerp(c, Color.white, 0.45f);
        return c;
    }

    static float EllipseN(Vector2 p, Vector2 center, float rx, float ry)
    {
        float dx = (p.x - center.x) / rx, dy = (p.y - center.y) / ry;
        return dx * dx + dy * dy;
    }

    static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    // rotten patches, a bruise and a few spots
    static Color Blemishes(Color c, Vector2 p)
    {
        float n = EllipseN(p, new Vector2(-0.62f, 0.32f), 0.16f, 0.21f);
        if (n < 1f) c = Color.Lerp(c, Rotten, n > 0.8f ? 0.5f : 0.85f);

        n = EllipseN(p, new Vector2(0.58f, -0.12f), 0.11f, 0.09f);
        if (n < 1f) c = Color.Lerp(c, Rotten, 0.85f);

        n = EllipseN(p, new Vector2(0.46f, 0.66f), 0.18f, 0.11f);
        if (n < 1f) c = Color.Lerp(c, Bruise, 0.55f);

        if ((p - new Vector2(-0.2f, 0.74f)).magnitude < 0.035f) c = Rotten;
        if ((p - new Vector2(0.12f, 0.82f)).magnitude < 0.03f) c = Rotten;
        if ((p - new Vector2(-0.46f, -0.32f)).magnitude < 0.04f) c = Rotten;
        return c;
    }

    // grumpy droopy eyes with bags, heavy brows, a big nose, buck teeth and a bushy mustache
    static Color Face(Color c, Vector2 p, Vector2[] stache, float[] stacheR)
    {
        // mustache (the same curve mirrored)
        if (Mathf.Abs(p.x) < 0.78f && p.y > -0.85f && p.y < -0.05f)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), p.y);
            float body = float.MaxValue, shine = float.MaxValue;

            for (int i = 0; i < stache.Length; i++)
            {
                float t = i / (stache.Length - 1f);
                body = Mathf.Min(body, (q - stache[i]).magnitude - stacheR[i]);
                if (t > 0.08f && t < 0.75f)
                    shine = Mathf.Min(shine, (q - (stache[i] + new Vector2(0f, 0.05f))).magnitude - stacheR[i] * 0.28f);
            }

            if (body < 0f)
                c = shine < 0f ? StacheShine : Stache;
        }

        return c;
    }

    static Color Tooth(Color c, Vector2 p, float x0, float x1, float y0, float y1)
    {
        if (p.x < x0 || p.x > x1 || p.y < y0 || p.y > y1) return c;

        const float edge = 0.018f;
        bool onEdge = p.x < x0 + edge || p.x > x1 - edge || p.y < y0 + edge;
        return onEdge ? Outline : Tooth_;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
