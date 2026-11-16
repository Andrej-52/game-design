using TMPro;
using UnityEngine;

// A cartoony tutorial sign. Pick a Mode (which icons to show) and edit Heading / Message in the Inspector.
public class Billboard : MonoBehaviour
{
    public enum Mode { Walk, Jump, Attack, Goal }

    public Mode mode = Mode.Walk;
    public string heading = "WALK";
    [TextArea(2, 4)] public string message = "Walk with A / D or the arrow keys";

    [Header("Parts (set by the prefab)")]
    public GameObject[] groups;           // one icon group per Mode, in the same order
    public TextMeshPro[] headingLabels;   // the text and its shadow
    public TextMeshPro[] messageLabels;

    [Header("Animated parts (set by the prefab)")]
    public Transform[] pulsers;           // keys and stars that pulse
    public Transform minuteHand;          // the clock on the goal sign
    public Transform hourHand;
    public Transform arrow;               // bobs to the right
    public Transform flag;                // waves
    public Renderer[] windows;            // office windows that blink

    Vector3 arrowStart;
    MaterialPropertyBlock block;

    static readonly Color WindowA = new Color(1f, 0.85f, 0.25f);
    static readonly Color WindowB = new Color(1f, 0.98f, 0.7f);

    void Awake()
    {
        if (arrow != null) arrowStart = arrow.localPosition;
        Apply();
    }

    void OnValidate()
    {
        SetText(headingLabels, heading);
        SetText(messageLabels, message);

#if UNITY_EDITOR
        // switching objects on and off is not allowed directly inside OnValidate
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) ApplyMode(); };
#endif
    }

    void Update()
    {
        float t = Time.time;

        if (pulsers != null)
            for (int i = 0; i < pulsers.Length; i++)
                if (pulsers[i] != null)
                    pulsers[i].localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 4f + i * 1.3f));

        if (minuteHand != null) minuteHand.localRotation = Quaternion.Euler(0f, 0f, -t * 150f);
        if (hourHand != null) hourHand.localRotation = Quaternion.Euler(0f, 0f, -t * 12.5f);
        if (arrow != null) arrow.localPosition = arrowStart + new Vector3(Mathf.Sin(t * 5f) * 0.5f, 0f, 0f);
        if (flag != null) flag.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 6f) * 12f);

        if (windows != null && windows.Length > 0)
        {
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] == null) continue;
                float k = (Mathf.Sin(t * 3f + i * 1.9f) + 1f) * 0.5f;
                windows[i].GetPropertyBlock(block);
                block.SetColor("_BaseColor", Color.Lerp(WindowA, WindowB, k));
                block.SetColor("_Color", Color.Lerp(WindowA, WindowB, k));
                windows[i].SetPropertyBlock(block);
            }
        }
    }

    void Apply()
    {
        SetText(headingLabels, heading);
        SetText(messageLabels, message);
        ApplyMode();
    }

    void ApplyMode()
    {
        if (groups == null) return;
        for (int i = 0; i < groups.Length; i++)
            if (groups[i] != null)
                groups[i].SetActive(i == (int)mode);
    }

    static void SetText(TextMeshPro[] labels, string text)
    {
        if (labels == null) return;
        foreach (TextMeshPro l in labels)
            if (l != null && l.text != text)
                l.text = text;
    }
}
