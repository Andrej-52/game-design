using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Create Tutorial Billboards : (re)builds the prefab and puts four signs along the path in the open scene
// Tools > Towelman > Rebuild Billboard Prefab   : rebuilds only the prefab design (signs in scenes pick up the new look)
public static class BillboardSetup
{
    const string PrefabPath = "Assets/Prefabs/Billboard.prefab";
    const string MatFolder = "Assets/Materials";

    // sign size
    const float W = 22f, H = 14f, CY = 15f;           // width, height, height of the middle
    const float MY = CY + 0.6f;                        // middle of the icon area

    static readonly Color Navy = new Color(0.10f, 0.16f, 0.30f);
    static readonly Color Cream = new Color(1f, 0.95f, 0.80f);
    static readonly Color Orange = new Color(0.97f, 0.55f, 0.15f);
    static readonly Color Teal = new Color(0.25f, 0.72f, 0.75f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.25f);
    static readonly Color Red = new Color(0.9f, 0.25f, 0.25f);
    static readonly Color Sky = new Color(0.55f, 0.78f, 0.95f);

    static Material mNavy, mCream, mOrange, mTeal, mYellow, mWhite, mRed, mSky, mWood;
    static Mesh triangle;

    static readonly Billboard.Mode[] Modes = { Billboard.Mode.Walk, Billboard.Mode.Jump, Billboard.Mode.Attack, Billboard.Mode.Goal };
    static readonly string[] Headings = { "WALK", "JUMP", "ATTACK", "GET TO WORK!" };
    static readonly string[] Messages =
    {
        "Walk with A / D or the arrow keys",
        "Jump with W or the up arrow",
        "Swing your towel with SPACE",
        "Reach the office before the timer runs out!"
    };

    [MenuItem("Tools/Towelman/Create Tutorial Billboards")]
    static void CreateTutorialBillboards()
    {
        GameObject prefab = BuildPrefab();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 start = player != null ? player.transform.position : Vector3.zero;
        Physics.SyncTransforms();

        for (int i = 0; i < Modes.Length; i++)
        {
            float x = start.x + 20f + i * 45f;

            // find the ground under that spot
            float y = start.y;
            if (Physics.Raycast(new Vector3(x, start.y + 50f, start.z), Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                y = hit.point.y;

            GameObject sign = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            sign.transform.position = new Vector3(x, y, start.z + 4f);   // behind the player's lane
            sign.name = "Billboard " + Modes[i];
            Undo.RegisterCreatedObjectUndo(sign, "Create billboard");

            Billboard b = sign.GetComponent<Billboard>();
            b.mode = Modes[i];
            b.heading = Headings[i];
            b.message = Messages[i];
            EditorUtility.SetDirty(b);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Created 4 billboards. Move them where you want them. Mode, Heading and Message are on the Billboard component.");
    }

    [MenuItem("Tools/Towelman/Rebuild Billboard Prefab")]
    static void Rebuild()
    {
        BuildPrefab();
        Debug.Log("Billboard prefab rebuilt.");
    }

    static GameObject BuildPrefab()
    {
        CleanUpOldAssets();
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(MatFolder);

        mNavy = Mat("Billboard_Navy", Navy, true);
        mCream = Mat("Billboard_Cream", Cream, true);
        mOrange = Mat("Billboard_Orange", Orange, true);
        mTeal = Mat("Billboard_Teal", Teal, true);
        mYellow = Mat("Billboard_Yellow", Yellow, true);
        mWhite = Mat("Billboard_White", Color.white, true);
        mRed = Mat("Billboard_Red", Red, true);
        mSky = Mat("Billboard_Sky", Sky, true);
        mWood = Mat("Billboard_Wood", new Color(0.55f, 0.35f, 0.2f), false);
        triangle = MakeTriangle();

        GameObject root = new GameObject("Billboard");
        Transform r = root.transform;

        // ---- the sign itself: chunky navy frame, cream board, wooden posts ----
        Part(r, PrimitiveType.Cylinder, "PostLeft", new Vector3(-7.5f, 4.75f, 0.9f), new Vector3(1.4f, 4.75f, 1.4f), mWood);
        Part(r, PrimitiveType.Cylinder, "PostRight", new Vector3(7.5f, 4.75f, 0.9f), new Vector3(1.4f, 4.75f, 1.4f), mWood);
        Part(r, PrimitiveType.Cube, "FootLeft", new Vector3(-7.5f, 0.35f, 0.9f), new Vector3(3f, 0.7f, 3f), mNavy);
        Part(r, PrimitiveType.Cube, "FootRight", new Vector3(7.5f, 0.35f, 0.9f), new Vector3(3f, 0.7f, 3f), mNavy);

        Part(r, PrimitiveType.Cube, "Frame", new Vector3(0f, CY, 0.3f), new Vector3(W + 1.4f, H + 1.4f, 0.9f), mNavy);
        Part(r, PrimitiveType.Cube, "Board", new Vector3(0f, CY, -0.1f), new Vector3(W, H, 0.5f), mCream);

        // round rivets in the corners
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sy in new[] { -1f, 1f })
                Part(r, PrimitiveType.Sphere, "Rivet", new Vector3(sx * (W / 2f + 0.1f), CY + sy * (H / 2f + 0.1f), -0.4f), Vector3.one * 1.1f, mOrange);

        // two lamps on top
        foreach (float sx in new[] { -1f, 1f })
        {
            Part(r, PrimitiveType.Cube, "LampArm", new Vector3(sx * 7f, CY + H / 2f + 1.3f, 0.3f), new Vector3(0.35f, 1.6f, 0.35f), mNavy);
            Part(r, PrimitiveType.Sphere, "Lamp", new Vector3(sx * 7f, CY + H / 2f + 2.4f, 0.3f), Vector3.one * 1.6f, mYellow);
        }

        // header (orange ribbon with a navy outline) and footer (navy ribbon)
        float headY = CY + H / 2f - 0.5f - 1.4f;
        Part(r, PrimitiveType.Cube, "HeaderOutline", new Vector3(0f, headY, -0.4f), new Vector3(W - 1f, 3.4f, 0.2f), mNavy);
        Part(r, PrimitiveType.Cube, "Header", new Vector3(0f, headY, -0.52f), new Vector3(W - 1.6f, 2.8f, 0.2f), mOrange);

        float footY = CY - H / 2f + 0.5f + 2f;
        Part(r, PrimitiveType.Cube, "Footer", new Vector3(0f, footY, -0.45f), new Vector3(W - 1.6f, 4f, 0.2f), mNavy);

        Color shadow = Navy;
        TextMeshPro headShadow = Text(r, "HeadingShadow", new Vector3(0.12f, headY - 0.12f, -0.64f), new Vector2(W - 3f, 2.6f), 1.5f, 3.6f, shadow);
        TextMeshPro head = Text(r, "Heading", new Vector3(0f, headY, -0.68f), new Vector2(W - 3f, 2.6f), 1.5f, 3.6f, Color.white);
        TextMeshPro msg = Text(r, "Message", new Vector3(0f, footY, -0.6f), new Vector2(W - 3f, 3.6f), 0.8f, 2f, Color.white);

        // ---- icon groups (one per mode) ----
        var pulsers = new List<Transform>();

        // WALK: [A][D]  or  [<][>]
        GameObject walk = Group(r, "WalkIcons");
        pulsers.Add(Key(walk.transform, "A", -8.2f, MY, 3.4f, null));
        pulsers.Add(Key(walk.transform, "D", -4.2f, MY, 3.4f, null));
        OrText(walk.transform, 0f, MY);
        pulsers.Add(Key(walk.transform, null, 4.2f, MY, 3.4f, 90f));
        pulsers.Add(Key(walk.transform, null, 8.2f, MY, 3.4f, -90f));

        // JUMP: [W]  or  [^]
        GameObject jump = Group(r, "JumpIcons");
        pulsers.Add(Key(jump.transform, "W", -4.5f, MY, 3.4f, null));
        OrText(jump.transform, 0f, MY);
        pulsers.Add(Key(jump.transform, null, 4.5f, MY, 3.4f, 0f));

        // ATTACK: [SPACE]  + a POW! burst
        GameObject attack = Group(r, "AttackIcons");
        pulsers.Add(Key(attack.transform, "SPACE", -3.5f, MY, 9.5f, null));
        GameObject pow = Group(attack.transform, "Pow", new Vector3(7.5f, MY, 0f));
        Part(pow.transform, PrimitiveType.Cube, "StarOutlineA", new Vector3(0f, 0f, -0.6f), new Vector3(4.6f, 4.6f, 0.2f), mNavy);
        Part(pow.transform, PrimitiveType.Cube, "StarOutlineB", new Vector3(0f, 0f, -0.6f), new Vector3(4.6f, 4.6f, 0.2f), mNavy, new Vector3(0f, 0f, 45f));
        Part(pow.transform, PrimitiveType.Cube, "StarA", new Vector3(0f, 0f, -0.75f), new Vector3(4.0f, 4.0f, 0.2f), mYellow);
        Part(pow.transform, PrimitiveType.Cube, "StarB", new Vector3(0f, 0f, -0.75f), new Vector3(4.0f, 4.0f, 0.2f), mYellow, new Vector3(0f, 0f, 45f));
        Text(pow.transform, "PowText", new Vector3(0f, 0f, -0.95f), new Vector2(4f, 2f), 1f, 1.9f, Red).text = "POW!";
        pulsers.Add(pow.transform);

        // GOAL: clock  ->  office
        GameObject goal = Group(r, "GoalIcons");

        // clock
        GameObject clock = Group(goal.transform, "Clock", new Vector3(-7.2f, MY, 0f));
        Part(clock.transform, PrimitiveType.Cylinder, "ClockRim", new Vector3(0f, 0f, -0.6f), new Vector3(5.2f, 0.2f, 5.2f), mNavy, new Vector3(90f, 0f, 0f));
        Part(clock.transform, PrimitiveType.Cylinder, "ClockFace", new Vector3(0f, 0f, -0.75f), new Vector3(4.5f, 0.2f, 4.5f), mWhite, new Vector3(90f, 0f, 0f));
        Part(clock.transform, PrimitiveType.Cylinder, "ClockBell", new Vector3(0f, 2.8f, -0.55f), new Vector3(1.1f, 0.15f, 1.1f), mRed, new Vector3(90f, 0f, 0f));
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Part(clock.transform, PrimitiveType.Cube, "Tick", new Vector3(Mathf.Sin(a) * 1.8f, Mathf.Cos(a) * 1.8f, -0.9f), new Vector3(0.22f, 0.5f, 0.1f), mNavy, new Vector3(0f, 0f, -i * 90f));
        }
        Transform hour = Pivot(clock.transform, "HourPivot", new Vector3(0f, 0f, -1.0f));
        Part(hour, PrimitiveType.Cube, "HourHand", new Vector3(0f, 0.55f, 0f), new Vector3(0.35f, 1.1f, 0.12f), mNavy);
        Transform minute = Pivot(clock.transform, "MinutePivot", new Vector3(0f, 0f, -1.1f));
        Part(minute, PrimitiveType.Cube, "MinuteHand", new Vector3(0f, 0.8f, 0f), new Vector3(0.25f, 1.6f, 0.12f), mRed);
        Part(clock.transform, PrimitiveType.Sphere, "ClockCap", new Vector3(0f, 0f, -1.15f), Vector3.one * 0.5f, mNavy);

        // arrow
        GameObject arrow = Group(goal.transform, "Arrow", new Vector3(-0.5f, MY, 0f));
        Part(arrow.transform, PrimitiveType.Cube, "ArrowShaftOutline", new Vector3(-0.4f, 0f, -0.6f), new Vector3(3.2f, 1.5f, 0.2f), mNavy);
        Part(arrow.transform, PrimitiveType.Cube, "ArrowShaft", new Vector3(-0.4f, 0f, -0.7f), new Vector3(2.9f, 1.1f, 0.2f), mOrange);
        Tri(arrow.transform, "ArrowHeadOutline", new Vector3(1.5f, 0f, -0.6f), 3.6f, -90f, mNavy);
        Tri(arrow.transform, "ArrowHead", new Vector3(1.5f, 0f, -0.7f), 3.0f, -90f, mOrange);

        // office
        GameObject office = Group(goal.transform, "Office", new Vector3(7.3f, MY, 0f));
        Part(office.transform, PrimitiveType.Cube, "OfficeOutline", new Vector3(0f, -0.2f, -0.6f), new Vector3(4.9f, 5.2f, 0.2f), mNavy);
        Part(office.transform, PrimitiveType.Cube, "OfficeBody", new Vector3(0f, -0.2f, -0.7f), new Vector3(4.4f, 4.7f, 0.2f), mSky);
        Part(office.transform, PrimitiveType.Cube, "OfficeRoof", new Vector3(0f, 2.3f, -0.75f), new Vector3(5.2f, 0.6f, 0.3f), mNavy);
        var windows = new List<Renderer>();
        foreach (float wx in new[] { -1.2f, 1.2f })
            foreach (float wy in new[] { 1.2f, -0.1f })
                windows.Add(Part(office.transform, PrimitiveType.Cube, "Window", new Vector3(wx, wy, -0.85f), new Vector3(1.2f, 0.9f, 0.1f), mYellow));
        Part(office.transform, PrimitiveType.Cube, "Door", new Vector3(0f, -1.55f, -0.85f), new Vector3(1.3f, 1.7f, 0.1f), mOrange);
        Part(office.transform, PrimitiveType.Cube, "FlagPole", new Vector3(0f, 3.4f, -0.75f), new Vector3(0.14f, 1.6f, 0.14f), mNavy);
        Transform flagPivot = Pivot(office.transform, "FlagPivot", new Vector3(0f, 3.95f, -0.8f));
        Part(flagPivot, PrimitiveType.Cube, "Flag", new Vector3(0.6f, -0.2f, 0f), new Vector3(1.2f, 0.7f, 0.1f), mRed);

        GameObject[] groups = { walk, jump, attack, goal };

        Billboard b = root.AddComponent<Billboard>();
        b.groups = groups;
        b.headingLabels = new[] { head, headShadow };
        b.messageLabels = new[] { msg };
        b.pulsers = pulsers.ToArray();
        b.minuteHand = minute;
        b.hourHand = hour;
        b.arrow = arrow.transform;
        b.flag = flagPivot;
        b.windows = windows.ToArray();
        b.heading = Headings[0];
        b.message = Messages[0];
        head.text = headShadow.text = b.heading;
        msg.text = b.message;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);   // same asset, so signs in scenes update
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    // ---------- building blocks ----------

    static GameObject Group(Transform parent, string name, Vector3? position = null)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position ?? Vector3.zero;
        return g;
    }

    static Transform Pivot(Transform parent, string name, Vector3 position)
    {
        return Group(parent, name, position).transform;
    }

    static Renderer Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        if (euler.HasValue) part.transform.localRotation = Quaternion.Euler(euler.Value);

        MeshRenderer r = part.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;

        // decoration only: the player and the enemies walk right past it
        Object.DestroyImmediate(part.GetComponent<Collider>());
        return r;
    }

    // a flat triangle pointing up, rotated by zRot degrees (0 = up, -90 = right, 90 = left)
    static Renderer Tri(Transform parent, string name, Vector3 position, float size, float zRot, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        go.transform.localScale = Vector3.one * size;
        go.AddComponent<MeshFilter>().sharedMesh = triangle;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        return r;
    }

    // A keycap: navy base, white face and a letter (or a triangle arrow if label is null)
    static Transform Key(Transform parent, string label, float x, float y, float width, float? arrowRotation)
    {
        const float h = 3.4f;
        GameObject key = Group(parent, "Key_" + (label ?? "Arrow"), new Vector3(x, y, 0f));
        Part(key.transform, PrimitiveType.Cube, "KeyBase", new Vector3(0f, -0.15f, -0.6f), new Vector3(width + 0.4f, h + 0.4f, 0.5f), mNavy);
        Part(key.transform, PrimitiveType.Cube, "KeyFace", new Vector3(0f, 0f, -0.9f), new Vector3(width, h, 0.2f), mWhite);

        if (label != null)
            Text(key.transform, "KeyLabel", new Vector3(0f, 0f, -1.05f), new Vector2(width, h), 1f, label.Length > 1 ? 2f : 2.6f, Navy).text = label;
        else
            Tri(key.transform, "KeyArrow", new Vector3(0f, 0f, -1.05f), 1.7f, arrowRotation.Value, mNavy);

        return key.transform;
    }

    static void OrText(Transform parent, float x, float y)
    {
        Text(parent, "Or", new Vector3(x, y, -0.7f), new Vector2(3f, 2f), 1f, 1.8f, Navy).text = "or";
    }

    static TextMeshPro Text(Transform parent, string name, Vector3 position, Vector2 size, float minSize, float maxSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize * 10f;   // 3D text: font size 10 = 1 world unit tall
        tmp.fontSizeMax = maxSize * 10f;
        tmp.text = name;
        return tmp;
    }

    // ---------- assets ----------

    static Mesh MakeTriangle()
    {
        string path = MatFolder + "/Billboard_Triangle.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;

        Mesh m = new Mesh { name = "Billboard_Triangle" };
        m.vertices = new[] { new Vector3(0f, 0.5f, 0f), new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
        m.triangles = new[] { 0, 1, 2, 0, 2, 1 };   // both sides, so it shows from either direction
        m.normals = new[] { Vector3.back, Vector3.back, Vector3.back };
        m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Material Mat(string name, Color color, bool unlit)
    {
        string path = MatFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");

        Material m = new Material(shader);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // removes the assets of the first (blue LED) design
    static void CleanUpOldAssets()
    {
        string[] old = { "BillboardFrame", "BillboardBoard", "BillboardPost", "BillboardMetal", "BillboardBezel", "BillboardNeon", "BillboardLamp", "BillboardScreen" };
        foreach (string n in old)
        {
            AssetDatabase.DeleteAsset(MatFolder + "/" + n + ".mat");
            AssetDatabase.DeleteAsset(MatFolder + "/" + n + ".png");
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
