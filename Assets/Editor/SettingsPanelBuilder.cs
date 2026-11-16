using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class SettingsPanelBuilder
{
    static readonly Color Navy = new Color32(0x1B, 0x2A, 0x5C, 0xFF);
    static readonly Color Blue = new Color32(0x24, 0x68, 0xC0, 0xFF);
    static readonly Color Yellow = new Color32(0xFF, 0xC8, 0x3D, 0xFF);
    static readonly Color Track = new Color32(0xD5, 0xDB, 0xE8, 0xFF);

    static Sprite panelSprite;
    static Sprite buttonSprite;
    static Sprite knobSprite;
    static Sprite checkSprite;
    static TMP_FontAsset font;

    [MenuItem("Tools/Towel Man/Create Settings Panel")]
    static void Create()
    {
        Canvas canvas = null;
        if (Selection.activeGameObject != null)
            canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Settings Panel", "No Canvas found in the open scene.", "OK");
            return;
        }
        canvas = canvas.rootCanvas;

        panelSprite = FindSprite("panel_rounded");
        buttonSprite = FindSprite("button_rounded");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        checkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        font = FindFont("Fredoka");

        RectTransform root = NewUI("SettingsPanel", canvas.transform);
        Stretch(root, 0f, 0f, 0f, 0f);

        RectTransform card = NewUI("Card", root);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(680f, 0f);
        AddImage(card, panelSprite, Color.white, true);

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(56, 56, 40, 48);
        layout.spacing = 22f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = card.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform title = NewUI("Title", card);
        SetHeight(title, 84f);
        AddText(title, "SETTINGS", 56f, Navy, TextAlignmentOptions.Center);

        AddSliderRow(card, "MASTER", 0.8f);
        AddSliderRow(card, "SOUND", 0.7f);
        AddSliderRow(card, "MUSIC", 0.6f);
        AddMuteRow(card);
        Button back = AddBackButton(card);

        MenuManager manager = Object.FindFirstObjectByType<MenuManager>();
        if (manager != null)
        {
            UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(manager.BackFromSettings));

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty prop = so.FindProperty("settingsPanel");
            if (prop != null)
            {
                GameObject old = prop.objectReferenceValue as GameObject;
                if (old != null)
                {
                    Undo.RecordObject(old, "Create Settings Panel");
                    old.SetActive(false);
                    old.name += " (Old)";
                }
                prop.objectReferenceValue = root.gameObject;
                so.ApplyModifiedProperties();
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(card);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Settings Panel");
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
    }

    static void AddSliderRow(Transform parent, string label, float value)
    {
        RectTransform row = NewUI(label + " Row", parent);
        SetHeight(row, 56f);
        AddLabel(row, label);

        RectTransform sliderRt = NewUI(label + " Slider", row);
        sliderRt.anchorMin = new Vector2(0.4f, 0f);
        sliderRt.anchorMax = new Vector2(1f, 1f);
        sliderRt.offsetMin = Vector2.zero;
        sliderRt.offsetMax = Vector2.zero;

        RectTransform background = NewUI("Background", sliderRt);
        CenterLine(background, 0f, 18f);
        Image backgroundImage = AddImage(background, panelSprite, Track, true);
        backgroundImage.pixelsPerUnitMultiplier = 4f;
        backgroundImage.raycastTarget = true;

        RectTransform fillArea = NewUI("Fill Area", sliderRt);
        CenterLine(fillArea, 0f, 18f);

        RectTransform fill = NewUI("Fill", fillArea);
        Stretch(fill, 0f, 0f, 0f, 0f);
        AddImage(fill, panelSprite, Blue, true).pixelsPerUnitMultiplier = 4f;

        RectTransform handleArea = NewUI("Handle Slide Area", sliderRt);
        CenterLine(handleArea, -36f, 36f);

        RectTransform handle = NewUI("Handle", handleArea);
        handle.sizeDelta = new Vector2(36f, 0f);
        Image handleImage = AddImage(handle, knobSprite, Yellow, false);
        handleImage.raycastTarget = true;

        Slider slider = sliderRt.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
    }

    static void AddMuteRow(Transform parent)
    {
        RectTransform row = NewUI("MUTE Row", parent);
        SetHeight(row, 56f);
        AddLabel(row, "MUTE");

        RectTransform toggleRt = NewUI("Mute Toggle", row);
        toggleRt.anchorMin = new Vector2(0.4f, 0.5f);
        toggleRt.anchorMax = new Vector2(0.4f, 0.5f);
        toggleRt.pivot = new Vector2(0f, 0.5f);
        toggleRt.sizeDelta = new Vector2(52f, 52f);

        RectTransform background = NewUI("Background", toggleRt);
        Stretch(background, 0f, 0f, 0f, 0f);
        Image backgroundImage = AddImage(background, panelSprite, Track, true);
        backgroundImage.pixelsPerUnitMultiplier = 3f;
        backgroundImage.raycastTarget = true;

        RectTransform mark = NewUI("Checkmark", background);
        Stretch(mark, 8f, 8f, 8f, 8f);
        Image markImage = AddImage(mark, checkSprite, Blue, false);

        Toggle toggle = toggleRt.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = markImage;
        toggle.isOn = false;
        markImage.canvasRenderer.SetAlpha(0f);
    }

    static Button AddBackButton(Transform parent)
    {
        RectTransform rt = NewUI("Back Button", parent);
        SetHeight(rt, 84f);
        Image image = AddImage(rt, buttonSprite, Blue, true);
        image.raycastTarget = true;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        RectTransform text = NewUI("Text", rt);
        Stretch(text, 0f, 10f, 0f, 0f);
        AddText(text, "BACK", 34f, Color.white, TextAlignmentOptions.Center);
        return button;
    }

    static void AddLabel(RectTransform row, string label)
    {
        RectTransform text = NewUI("Label", row);
        text.anchorMin = new Vector2(0f, 0f);
        text.anchorMax = new Vector2(0.38f, 1f);
        text.offsetMin = Vector2.zero;
        text.offsetMax = Vector2.zero;
        AddText(text, label, 30f, Navy, TextAlignmentOptions.Left);
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void CenterLine(RectTransform rt, float widthDelta, float height)
    {
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(widthDelta, height);
    }

    static void SetHeight(RectTransform rt, float height)
    {
        LayoutElement element = rt.gameObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
    }

    static Image AddImage(RectTransform rt, Sprite sprite, Color color, bool sliced)
    {
        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TextMeshProUGUI AddText(RectTransform rt, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.characterSpacing = 3f;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    static TMP_FontAsset FindFont(string key)
    {
        foreach (string guid in AssetDatabase.FindAssets(key + " t:TMP_FontAsset"))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }
}
