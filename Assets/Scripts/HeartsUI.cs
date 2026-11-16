using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The hearts in the top left corner. One heart per health point.
// A lost heart cracks in two halves that fall away, with a few shards. The rest of the hearts jump in panic.
public class HeartsUI : MonoBehaviour
{
    public PlayerHealth player;

    [Header("Sprites (set by Tools > Towelman > Add Hearts HUD)")]
    public Sprite heart;
    public Sprite leftHalf;
    public Sprite rightHalf;

    [Header("Look")]
    public float heartSize = 110f;
    public float spacing = 12f;
    public Color emptyColor = new Color(0f, 0f, 0f, 0.28f);

    class Slot
    {
        public RectTransform rect;
        public Image image;
        public float popStart = -10f;
    }

    readonly List<Slot> slots = new List<Slot>();
    int shown;
    float punch;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerHealth>();
        }

        if (player == null)
        {
            Debug.LogWarning("HeartsUI: no PlayerHealth found on the Player.");
            enabled = false;
            return;
        }

        for (int i = 0; i < player.maxHealth; i++)
            slots.Add(MakeSlot(i));

        shown = player.Health;
        for (int i = shown; i < slots.Count; i++)
            slots[i].image.color = emptyColor;
    }

    void Update()
    {
        int hp = player.Health;

        if (hp < shown)
        {
            // lost hearts: break them from the right, one after another
            int delayIndex = 0;
            for (int i = shown - 1; i >= hp; i--)
                StartCoroutine(BreakRoutine(slots[i], delayIndex++ * 0.15f));
            punch = 1f;
        }
        else if (hp > shown)
        {
            for (int i = shown; i < hp && i < slots.Count; i++)
            {
                slots[i].image.color = Color.white;
                slots[i].popStart = Time.time;
            }
        }
        shown = hp;

        punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 3f);

        // idle animation: the hearts beat, the last one beats fast and shakes
        for (int i = 0; i < slots.Count; i++)
        {
            Slot s = slots[i];
            if (i >= shown)
            {
                s.rect.localScale = Vector3.one;
                s.rect.localRotation = Quaternion.identity;
                continue;
            }

            bool last = shown == 1;
            float beat = last ? 0.12f * Mathf.Sin(Time.time * 12f) : 0.05f * Mathf.Sin(Time.time * 4f + i * 0.9f);
            float pop = EaseOutBack(Mathf.Clamp01((Time.time - s.popStart) / 0.4f));

            s.rect.localScale = Vector3.one * Mathf.Max(0.01f, (1f + beat + punch * 0.35f) * pop);
            s.rect.localRotation = Quaternion.Euler(0f, 0f, last ? Mathf.Sin(Time.time * 14f) * 6f : punch * Mathf.Sin(Time.time * 40f) * 8f);
        }
    }

    Slot MakeSlot(int i)
    {
        GameObject go = new GameObject("Heart " + (i + 1), typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(heartSize, heartSize);
        rt.anchoredPosition = new Vector2(heartSize * 0.5f + i * (heartSize + spacing), -heartSize * 0.5f);

        Image img = go.GetComponent<Image>();
        img.sprite = heart;
        img.preserveAspect = true;
        img.raycastTarget = false;

        return new Slot { rect = rt, image = img };
    }

    Image MakePiece(string name, Sprite sprite, Slot s)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = s.rect.sizeDelta;
        rt.anchoredPosition = s.rect.anchoredPosition;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    class Shard
    {
        public Image image;
        public Vector2 velocity;
        public float spin;
    }

    IEnumerator BreakRoutine(Slot s, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        s.image.color = emptyColor;   // an empty heart stays behind

        Image left = MakePiece("HeartLeft", leftHalf, s);
        Image right = MakePiece("HeartRight", rightHalf, s);
        RectTransform lr = left.rectTransform, rr = right.rectTransform;
        Vector2 basePos = s.rect.anchoredPosition;

        // little red and dark bits that fly out of the crack
        Color[] shardColors = { new Color(0.93f, 0.31f, 0.33f), new Color(0.75f, 0.2f, 0.25f), new Color(0.18f, 0.07f, 0.1f) };
        var shards = new List<Shard>();
        for (int i = 0; i < 9; i++)
        {
            GameObject go = new GameObject("Shard", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.sizeDelta = Vector2.one * Random.Range(8f, 16f);
            rt.anchoredPosition = basePos + Random.insideUnitCircle * 10f;
            Image img = go.GetComponent<Image>();
            img.color = shardColors[i % shardColors.Length];
            img.raycastTarget = false;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(180f, 420f);
            shards.Add(new Shard
            {
                image = img,
                velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.6f) * speed,
                spin = Random.Range(-500f, 500f)
            });
        }

        const float shakeTime = 0.18f, total = 1.0f;
        Vector2 vl = new Vector2(-150f, 340f), vr = new Vector2(150f, 340f);
        Vector2 pl = basePos, pr = basePos;
        float rotL = 0f, rotR = 0f;
        float t = 0f;

        while (t < total)
        {
            float dt = Time.deltaTime;
            t += dt;

            if (t < shakeTime)
            {
                // the heart shakes before it cracks
                Vector2 j = Random.insideUnitCircle * 5f;
                lr.anchoredPosition = basePos + j;
                rr.anchoredPosition = basePos + j;
            }
            else
            {
                vl.y -= 1500f * dt;
                vr.y -= 1500f * dt;
                pl += vl * dt;
                pr += vr * dt;
                rotL += 260f * dt;
                rotR -= 260f * dt;

                lr.anchoredPosition = pl;
                rr.anchoredPosition = pr;
                lr.localRotation = Quaternion.Euler(0f, 0f, rotL);
                rr.localRotation = Quaternion.Euler(0f, 0f, rotR);
            }

            float a = 1f - Mathf.InverseLerp(0.55f, total, t);
            left.color = new Color(1f, 1f, 1f, a);
            right.color = new Color(1f, 1f, 1f, a);

            if (t >= shakeTime)
            {
                foreach (Shard sh in shards)
                {
                    sh.velocity.y -= 1200f * dt;
                    sh.image.rectTransform.anchoredPosition += sh.velocity * dt;
                    sh.image.rectTransform.Rotate(0f, 0f, sh.spin * dt);
                    Color c = sh.image.color;
                    c.a = a;
                    sh.image.color = c;
                }
            }

            yield return null;
        }

        Destroy(left.gameObject);
        Destroy(right.gameObject);
        foreach (Shard sh in shards) Destroy(sh.image.gameObject);
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
