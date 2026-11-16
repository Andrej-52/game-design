using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// The intro cutscene at the start of a level:
//  1. a big cartoony alarm clock rings on a black screen,
//  2. cut to a close-up of Towelman checking his watch, the camera pushes in,
//  3. "I'M LATE!" slams in with a camera punch,
//  4. he sprints off while the camera pulls back to the normal view,
//  5. the player gets control and the timer starts.
// Press Enter or click to skip. Add it with Tools > Towelman > Add Intro Sequence.
public class IntroSequence : MonoBehaviour
{
    public TMP_FontAsset font;

    [Header("Text")]
    public string alarmText = "BRRRRING!";
    public string shout = "I'M LATE!";
    public string hint = "Get to the office before the timer runs out!";

    [Header("Hand-with-watch clip (set by Tools > Towelman > Add Intro Sequence)")]
    public VideoClip handClip;

    [Header("Camera")]
    [Tooltip("1 = normal camera distance, smaller = closer")]
    public float closeUpDistance = 0.45f;
    public float pushInAmount = 0.12f;       // how much closer the camera slowly creeps during the watch check
    public float pullBackSpeed = 4f;         // how fast the camera returns to the game view

    [Header("Run out")]
    public float runTime = 1.6f;
    public float runSpeedMultiplier = 1.2f;  // times his normal move speed

    const string WatchState = "Towelman_Rig|Idle_Watch";
    const string IdleState = "Towelman_Rig|Idle (1)";

    static readonly Color Navy = new Color32(0x1B, 0x2A, 0x5C, 0xFF);
    static readonly Color Yellow = new Color32(0xFF, 0xC8, 0x3D, 0xFF);
    static readonly Color ClockRed = new Color(0.86f, 0.17f, 0.20f);
    static readonly Color Cream = new Color(0.99f, 0.97f, 0.90f);
    static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f);

    PlayerHealth player;
    PlayerMovement movement;
    PlayerAttack attack;
    Rigidbody rb;
    Animator animator;
    LevelTimer timer;
    Camera cam;
    CameraFollow follow;

    readonly List<Canvas> hiddenHud = new List<Canvas>();
    float followSpeed;
    float cameraZ;
    bool cameraTaken;
    Vector3 cameraOffset;

    // UI
    GameObject canvasGo;
    CanvasGroup black, flash, shoutGroup, hintGroup, alarmTextGroup, rays;
    RectTransform clock, shoutRect, hourHand, minuteHand, handRoot;
    RawImage handImage;

    Coroutine main;
    bool finished;

    void Awake()
    {
        // must be done in Awake: the timer reads this in its Start
        timer = FindFirstObjectByType<LevelTimer>();
        if (timer != null) timer.startOnPlay = false;

        BuildUI();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null)
        {
            Debug.LogWarning("IntroSequence: no Player found, skipping the intro.");
            if (timer != null) timer.StartTimer();
            Destroy(gameObject);
            return;
        }

        player = p.GetComponent<PlayerHealth>();
        movement = p.GetComponent<PlayerMovement>();
        attack = p.GetComponent<PlayerAttack>();
        rb = p.GetComponent<Rigidbody>();
        animator = p.GetComponentInChildren<Animator>();

        cam = Camera.main;
        if (cam != null) follow = cam.GetComponent<CameraFollow>();

        main = StartCoroutine(Sequence());
    }

    void Update()
    {
        if (!finished && main != null && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            StopCoroutine(main);
            Finish();
        }
    }

    // ---------- the cutscene ----------

    IEnumerator Sequence()
    {
        // wait a frame so the player's and the camera's own scripts have started
        black.alpha = 1f;
        yield return null;

        if (movement != null) movement.Stun(9999f);
        if (attack != null) attack.enabled = false;
        if (player != null) player.Immune = true;

        // hide the hearts and the watch for now
        foreach (HeartsUI h in FindObjectsByType<HeartsUI>(FindObjectsSortMode.None)) Hide(h.GetComponentInParent<Canvas>());
        foreach (WatchUI w in FindObjectsByType<WatchUI>(FindObjectsSortMode.None)) Hide(w.GetComponentInParent<Canvas>());

        // take over the camera
        Vector3 offset = Vector3.zero;
        bool hasCamera = cam != null && follow != null && player != null;
        if (hasCamera)
        {
            offset = cam.transform.position - player.transform.position;
            cameraOffset = offset;
            cameraZ = cam.transform.position.z;
            cameraTaken = true;
            followSpeed = follow.smoothSpeed;
            follow.enabled = false;
        }

        float watchLength = 1.3f;
        if (animator != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name.Contains("Idle_Watch")) watchLength = clip.length;
        }

        Vector3 playerPos = hasCamera ? player.transform.position : Vector3.zero;
        float t;
        float lookTime = watchLength + 0.4f;

        if (handClip != null && hasCamera)
        {
            // the camera is already waiting on him, under the clip
            cam.transform.position = playerPos + offset * closeUpDistance;

            // 1. cut to the clip of a hand with a watch: it ticks to 8:00 and rings
            yield return HandClipBeat();

            // 2. cut back to him: he is checking his watch and the camera creeps in
            if (animator != null && animator.HasState(0, Animator.StringToHash(WatchState)))
                animator.Play(WatchState, 0, 0f);

            t = 0f;
            while (t < lookTime)
            {
                t += Time.unscaledDeltaTime;
                float d = closeUpDistance - pushInAmount * Mathf.Clamp01(t / lookTime);
                cam.transform.position = playerPos + offset * d;
                yield return null;
            }
        }
        else
        {
            // no clip: use the big alarm clock card instead
            if (animator != null && animator.HasState(0, Animator.StringToHash(WatchState)))
                animator.Play(WatchState, 0, 0f);

            // 1. the alarm clock
            yield return AlarmClock();

            // 2. cut to the close-up while he checks his watch
            t = 0f;
            float fade = 0.25f;

            while (t < lookTime)
            {
                t += Time.unscaledDeltaTime;
                black.alpha = 1f - Mathf.Clamp01(t / fade);
                if (hasCamera)
                {
                    float d = closeUpDistance - pushInAmount * Mathf.Clamp01(t / lookTime);
                    cam.transform.position = playerPos + offset * d;
                }
                yield return null;
            }
            black.alpha = 0f;
        }

        // 3. "I'M LATE!" with a punch
        yield return Shout(hasCamera, playerPos, offset);

        // 4. text away, he runs while the camera pulls back to the game view
        yield return FadeGroups(0.3f, shoutGroup, hintGroup);

        if (animator != null && animator.HasState(0, Animator.StringToHash(IdleState)))
            animator.CrossFadeInFixedTime(IdleState, 0.1f);

        float speed = movement != null ? movement.moveSpeed * runSpeedMultiplier : 6f;
        float run = 0f;
        while (run < runTime)
        {
            run += Time.deltaTime;
            if (rb != null) rb.linearVelocity = new Vector3(speed, rb.linearVelocity.y, 0f);

            // pull the camera back to its normal spot behind the player, following him as he runs
            if (hasCamera)
            {
                Vector3 target = player.transform.position + offset;
                cam.transform.position = Vector3.Lerp(cam.transform.position, target, 1f - Mathf.Exp(-pullBackSpeed * Time.deltaTime));
            }

            yield return null;
        }

        // 5. play
        Finish();
    }

    // plays the clip of the hand with the watch full screen, then cuts back to the game
    IEnumerator HandClipBeat()
    {
        RenderTexture texture = new RenderTexture(1024, 576, 0);
        VideoPlayer video = gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.isLooping = false;
        video.clip = handClip;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.targetTexture = texture;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.Prepare();

        float wait = 0f;
        while (!video.isPrepared && wait < 3f)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!video.isPrepared)
        {
            Debug.LogWarning("IntroSequence: the hand clip could not be played, skipping it.");
            Destroy(video);
            texture.Release();
            black.alpha = 0f;
            yield break;
        }

        handImage.texture = texture;
        handRoot.gameObject.SetActive(true);
        black.alpha = 0f;
        video.Play();

        float length = (float)video.length;
        float t = 0f;
        const float ringStart = 2.1f;   // the watch starts ringing about here in the clip
        while (t < length - 0.05f)
        {
            t += Time.unscaledDeltaTime;
            alarmTextGroup.alpha = t > ringStart ? Mathf.Min(1f, (t - ringStart) * 8f) : 0f;
            yield return null;
        }

        // hard cut back to the game
        alarmTextGroup.alpha = 0f;
        handRoot.gameObject.SetActive(false);
        video.Stop();
        Destroy(video);
        texture.Release();
    }

    IEnumerator AlarmClock()
    {
        clock.gameObject.SetActive(true);
        rays.alpha = 0f;
        alarmTextGroup.alpha = 0f;

        // pops in with a bounce, hands still on 7:57
        float t = 0f;
        const float pop = 0.4f;
        while (t < pop)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / pop);
            clock.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(k));
            yield return null;
        }

        // the minute hand ticks up to 12 (8:00)
        const float tickTime = 1.0f;
        t = 0f;
        while (t < tickTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tickTime);
            float minute = Mathf.Floor(k * 4f) / 4f;     // four jerky ticks
            minuteHand.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(342f, 360f, minute));
            hourHand.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(239f, 240f, k));
            yield return null;
        }

        minuteHand.localRotation = Quaternion.Euler(0f, 0f, -360f);
        hourHand.localRotation = Quaternion.Euler(0f, 0f, -240f);

        // it rings: the clock shakes, rays flash, the word appears
        t = 0f;
        const float ring = 1.1f;
        while (t < ring)
        {
            t += Time.unscaledDeltaTime;
            clock.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 70f) * 7f);
            clock.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 35f));
            rays.alpha = Mathf.Sin(t * 30f) > 0f ? 1f : 0.2f;
            alarmTextGroup.alpha = Mathf.Min(1f, t * 6f);
            yield return null;
        }

        // white flash, then to black for the cut
        t = 0f;
        const float flashTime = 0.15f;
        while (t < flashTime)
        {
            t += Time.unscaledDeltaTime;
            flash.alpha = Mathf.Clamp01(t / flashTime);
            yield return null;
        }

        clock.gameObject.SetActive(false);
        alarmTextGroup.alpha = 0f;
        flash.alpha = 0f;
    }

    IEnumerator Shout(bool hasCamera, Vector3 playerPos, Vector3 offset)
    {
        float t = 0f;
        const float dur = 0.4f;
        float baseDist = closeUpDistance - pushInAmount;

        // the text slams in, the camera jolts back
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            shoutGroup.alpha = Mathf.Min(1f, k * 3f);
            shoutRect.localScale = Vector3.one * Mathf.LerpUnclamped(1.8f, 1f, EaseOutBack(k));

            if (hasCamera)
            {
                float punch = Mathf.Sin(k * Mathf.PI) * 0.08f;
                Vector3 shake = new Vector3(Mathf.Sin(t * 90f), Mathf.Cos(t * 75f), 0f) * 0.25f * (1f - k);
                cam.transform.position = playerPos + offset * (baseDist + punch) + shake;
            }
            yield return null;
        }

        shoutGroup.alpha = 1f;
        shoutRect.localScale = Vector3.one;

        // hint fades in under it
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            hintGroup.alpha = Mathf.Clamp01(t / 0.4f);
            if (hasCamera) cam.transform.position = playerPos + offset * baseDist;
            yield return null;
        }
        hintGroup.alpha = 1f;

        float hold = 0.9f;
        while (hold > 0f)
        {
            hold -= Time.unscaledDeltaTime;
            if (hasCamera) cam.transform.position = playerPos + offset * baseDist;
            yield return null;
        }
    }

    IEnumerator FadeGroups(float duration, params CanvasGroup[] groups)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / duration);
            foreach (CanvasGroup g in groups) g.alpha = a;
            yield return null;
        }
        foreach (CanvasGroup g in groups) g.alpha = 0f;
    }

    // ends the cutscene (also used when it is skipped): gives control back and starts the clock
    void Finish()
    {
        if (finished) return;
        finished = true;

        if (follow != null)
        {
            // the normal camera script never changes the depth, so put it back exactly
            if (cam != null && cameraTaken)
            {
                Vector3 p = cam.transform.position;
                p.z = cameraZ;
                cam.transform.position = p;
            }
            if (followSpeed > 0f) follow.smoothSpeed = followSpeed;
            follow.enabled = true;
        }

        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(WatchState)
            && animator.HasState(0, Animator.StringToHash(IdleState)))
            animator.CrossFadeInFixedTime(IdleState, 0.15f);

        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        if (movement != null) movement.Stun(0f);
        if (attack != null) attack.enabled = true;
        if (player != null) player.Immune = false;

        foreach (Canvas c in hiddenHud) if (c != null) c.enabled = true;

        if (timer != null) timer.StartTimer();

        Destroy(gameObject);
    }

    void Hide(Canvas c)
    {
        if (c == null) return;
        c = c.rootCanvas;
        if (c.enabled)
        {
            c.enabled = false;
            hiddenHud.Add(c);
        }
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // ---------- UI ----------

    void BuildUI()
    {
        canvasGo = new GameObject("IntroCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = (RectTransform)canvasGo.transform;
        Sprite circle = MakeCircle(128);
        Sprite rounded = MakeRoundedRect(64, 22);

        // black screen (stays under everything else on this canvas)
        RectTransform blackRt = Rect("Black", root);
        Stretch(blackRt);
        Image blackImg = blackRt.gameObject.AddComponent<Image>();
        blackImg.color = Color.black;
        blackImg.raycastTarget = false;
        black = blackRt.gameObject.AddComponent<CanvasGroup>();
        black.alpha = 1f;
        black.blocksRaycasts = false;

        // the clip of the hand with the watch (full screen, cropped to fit)
        handRoot = Rect("HandClip", root);
        Stretch(handRoot);
        RectTransform handFit = Rect("Picture", handRoot);
        handFit.anchorMin = handFit.anchorMax = new Vector2(0.5f, 0.5f);
        handFit.anchoredPosition = Vector2.zero;
        handImage = handFit.gameObject.AddComponent<RawImage>();
        handImage.raycastTarget = false;
        AspectRatioFitter fitter = handFit.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;
        handRoot.gameObject.SetActive(false);

        // the alarm clock
        clock = Rect("AlarmClock", root);
        clock.anchorMin = clock.anchorMax = new Vector2(0.5f, 0.5f);
        clock.anchoredPosition = new Vector2(0f, 20f);
        clock.sizeDelta = new Vector2(600f, 600f);

        // bells and feet first so the body covers their ends
        Img("BellL", clock, circle, ClockRed, new Vector2(-170f, 235f), new Vector2(170f, 170f));
        Img("BellR", clock, circle, ClockRed, new Vector2(170f, 235f), new Vector2(170f, 170f));
        Img("BellLShine", clock, circle, new Color(1f, 0.55f, 0.5f), new Vector2(-195f, 255f), new Vector2(45f, 45f));
        Img("BellRShine", clock, circle, new Color(1f, 0.55f, 0.5f), new Vector2(145f, 255f), new Vector2(45f, 45f));
        Image footL = Img("FootL", clock, rounded, Ink, new Vector2(-170f, -275f), new Vector2(60f, 90f));
        footL.type = Image.Type.Sliced; footL.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        Image footR = Img("FootR", clock, rounded, Ink, new Vector2(170f, -275f), new Vector2(60f, 90f));
        footR.type = Image.Type.Sliced; footR.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f);

        Img("Outline", clock, circle, Ink, Vector2.zero, new Vector2(540f, 540f));
        Img("Body", clock, circle, ClockRed, Vector2.zero, new Vector2(515f, 515f));
        Img("Face", clock, circle, Cream, Vector2.zero, new Vector2(430f, 430f));

        for (int i = 0; i < 12; i++)
        {
            bool big = i % 3 == 0;
            RectTransform holder = Rect("Tick", clock);
            holder.anchoredPosition = Vector2.zero;
            holder.sizeDelta = Vector2.zero;
            holder.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            Image tick = Img("Mark", holder, null, Ink, new Vector2(0f, 178f), new Vector2(big ? 16f : 9f, big ? 42f : 24f));
            tick.rectTransform.localRotation = Quaternion.identity;
        }

        hourHand = Hand(clock, rounded, 24f, 120f);
        minuteHand = Hand(clock, rounded, 16f, 180f);
        hourHand.localRotation = Quaternion.Euler(0f, 0f, -239f);
        minuteHand.localRotation = Quaternion.Euler(0f, 0f, -342f);
        Img("Pin", clock, circle, Yellow, Vector2.zero, new Vector2(42f, 42f));

        // rays around the clock while it rings
        RectTransform raysRt = Rect("Rays", clock);
        raysRt.anchoredPosition = Vector2.zero;
        raysRt.sizeDelta = Vector2.zero;
        for (int i = 0; i < 12; i++)
        {
            RectTransform holder = Rect("Ray", raysRt);
            holder.anchoredPosition = Vector2.zero;
            holder.sizeDelta = Vector2.zero;
            holder.localRotation = Quaternion.Euler(0f, 0f, i * 30f + 15f);
            Img("Line", holder, null, Yellow, new Vector2(0f, 360f), new Vector2(14f, 70f));
        }
        rays = raysRt.gameObject.AddComponent<CanvasGroup>();

        // "BRRRRING!"
        RectTransform alarmRt = Rect("AlarmText", root);
        alarmRt.anchorMin = alarmRt.anchorMax = new Vector2(0.5f, 0.5f);
        alarmRt.anchoredPosition = new Vector2(0f, -380f);
        alarmRt.sizeDelta = new Vector2(1500f, 150f);
        alarmRt.localRotation = Quaternion.Euler(0f, 0f, 3f);
        TextMeshProUGUI alarmTmp = Text(alarmRt, alarmText, 120f, Yellow);
        alarmTmp.fontStyle = FontStyles.Bold;
        alarmTmp.outlineWidth = 0.35f;
        alarmTmp.outlineColor = (Color32)Navy;
        alarmTextGroup = alarmRt.gameObject.AddComponent<CanvasGroup>();
        alarmTextGroup.alpha = 0f;

        clock.gameObject.SetActive(false);

        // big shout, slightly tilted like a comic
        shoutRect = Rect("Shout", root);
        shoutRect.anchorMin = shoutRect.anchorMax = new Vector2(0.5f, 0.5f);
        shoutRect.anchoredPosition = new Vector2(0f, 120f);
        shoutRect.sizeDelta = new Vector2(1500f, 220f);
        shoutRect.localRotation = Quaternion.Euler(0f, 0f, -4f);
        TextMeshProUGUI shoutText = Text(shoutRect, shout, 190f, Yellow);
        shoutText.fontStyle = FontStyles.Bold;
        shoutText.outlineWidth = 0.35f;
        shoutText.outlineColor = (Color32)Navy;
        shoutGroup = shoutRect.gameObject.AddComponent<CanvasGroup>();
        shoutGroup.alpha = 0f;

        RectTransform hintRt = Rect("Hint", root);
        hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0.5f);
        hintRt.anchoredPosition = new Vector2(0f, -40f);
        hintRt.sizeDelta = new Vector2(1500f, 70f);
        TextMeshProUGUI hintText = Text(hintRt, hint, 44f, Color.white);
        hintText.outlineWidth = 0.3f;
        hintText.outlineColor = (Color32)Navy;
        hintGroup = hintRt.gameObject.AddComponent<CanvasGroup>();
        hintGroup.alpha = 0f;

        // white flash, on top
        RectTransform flashRt = Rect("Flash", root);
        Stretch(flashRt);
        Image flashImg = flashRt.gameObject.AddComponent<Image>();
        flashImg.color = Color.white;
        flashImg.raycastTarget = false;
        flash = flashRt.gameObject.AddComponent<CanvasGroup>();
        flash.alpha = 0f;
        flash.blocksRaycasts = false;
    }

    // a clock hand that turns around the centre of the clock
    static RectTransform Hand(RectTransform parent, Sprite rounded, float width, float length)
    {
        Image img = Img("Hand", parent, rounded, Ink, Vector2.zero, new Vector2(width, length));
        img.type = Image.Type.Sliced;
        img.rectTransform.pivot = new Vector2(0.5f, 0.1f);
        return img.rectTransform;
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

    static Image Img(string name, Transform parent, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
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
