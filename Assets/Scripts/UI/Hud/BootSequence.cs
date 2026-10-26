using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ลำดับเปิดจอตอนเริ่มเกม: เส้นแบ่งจอสว่าง -> แผ่นดำ 5 ชิ้นเปิด (กลาง -> บน+ล่าง -> ซ้าย+ขวา) -> HUD เลื่อนเข้าจากขอบ
// ระหว่างนี้หยุดเวลาเกมและล็อกการควบคุม (PauseMenu.InputBlocked) จนจบลำดับ
public class BootSequence : MonoBehaviour
{
    public static bool Running { get; private set; }
    public static float HudProgress { get; private set; } = 1f;   // 0 = HUD ยังอยู่นอกจอ → 1 = เข้าที่แล้ว (ไม่มี BootSequence = 1 ตลอด)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Running = false; HudProgress = 1f; }

    public enum Edge { Left, Right, Top, Bottom }

    [Serializable]
    public class SlideItem
    {
        public RectTransform target;
        public Edge from = Edge.Bottom;   // เลื่อนมาจากขอบไหน
        public float delay = 0f;          // หน่วงเพิ่มเฉพาะชิ้นนี้ (วินาที)

        [NonSerialized] public Vector2 basePos;
        [NonSerialized] public Vector2 offset;
    }

    [Header("เปิดใช้")]
    [SerializeField] bool playOnStart = true;
    [SerializeField] bool allowSkip = true;           // กดแป้นใดก็ได้หรือคลิกเมาส์ = ข้ามลำดับ

    [Header("เริ่มต้น")]
    [SerializeField] float startDelay = 1f;           // จอดำสนิท รอเท่านี้ก่อนเส้นเริ่มสว่าง (วินาที)

    [Header("1. เส้นแบ่งจอ (ใส่ชุดเดียวกับ ScreenLineAlert)")]
    [SerializeField] Graphic[] lines;
    [SerializeField] float lineTime = 0.6f;           // เวลารวมของช่วงเส้นสว่างขึ้นแล้วลดลง
    [SerializeField, Range(0.05f, 0.9f)] float lineRiseFraction = 0.4f;   // สัดส่วนเวลาช่วงขาขึ้น

    [Header("2. แผ่นดำ 5 ชิ้น (ใส่ CanvasGroup ชุดเดียวกับที่ใส่ใน ScreenZoneBlackout)")]
    [SerializeField] ScreenZoneBlackout blackout;
    [SerializeField] CanvasGroup centerZone;
    [SerializeField] CanvasGroup topZone;
    [SerializeField] CanvasGroup bottomZone;
    [SerializeField] CanvasGroup leftZone;
    [SerializeField] CanvasGroup rightZone;
    [SerializeField] float zoneStart = 0.5f;          // เริ่มเปิดแผ่นแรกที่วินาทีนี้
    [SerializeField] float zoneStepGap = 0.45f;       // ระยะห่างระหว่างแต่ละจังหวะ
    [SerializeField] float zoneFadeTime = 0.55f;      // เวลาที่ใช้เปิดแต่ละชิ้น
    [SerializeField, Range(0f, 1f)] float flickerAmount = 0.5f;   // ความแรงของการกะพริบแบบจอเก่าตอนกำลังเปิด
    [SerializeField] float flickerFrequency = 30f;

    [Header("3. HUD เลื่อนเข้า")]
    [SerializeField] SlideItem[] slides;
    [SerializeField] float hudDelay = 0.2f;           // หลังแผ่นสุดท้ายเริ่มเปิดเสร็จ รอเท่านี้ก่อนเลื่อน
    [SerializeField] float hudTime = 0.7f;
    [SerializeField] float edgeMargin = 20f;          // ให้ออกไปนอกจอเกินขอบเท่านี้ (พิกเซล)

    Color[] lineBase;
    float t, total, hudStart;
    bool done = true;

    void Awake()
    {
        lineBase = new Color[lines.Length];
        for (int i = 0; i < lines.Length; i++)
            lineBase[i] = lines[i] != null ? lines[i].color : Color.white;
    }

    void Start()
    {
        if (!playOnStart) return;

        // ทำที่ Start (หลังทุก Awake) เพื่อไม่ให้สคริปต์อื่นจำค่าตอนที่ถูกซ่อนไปเป็นค่าปกติ
        SetLinesAlpha(0f);
        SetAllZones(1f);
        PrepareSlides();

        hudStart = zoneStart + zoneStepGap * 2f + zoneFadeTime + hudDelay;
        total = hudStart + hudTime + MaxSlideDelay();
        t = -startDelay;   // ค่าลบ = ช่วงรอ ทุกส่วนยังไม่เริ่ม (จอดำ)
        done = false;
        Running = true;
        HudProgress = 0f;
        Time.timeScale = 0f;
    }

    void OnDestroy()
    {
        HudProgress = 1f;
        if (Running) { Running = false; Time.timeScale = 1f; }
    }

    void Update()
    {
        if (done) return;

        t += Time.unscaledDeltaTime;

        if (allowSkip && ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)))
            t = total;

        UpdateLines();
        UpdateZones();
        UpdateSlides();

        if (t >= total) Finish();
    }

    // ---------- 1. เส้น ----------
    void UpdateLines()
    {
        float p = Mathf.Clamp01(t / Mathf.Max(0.01f, lineTime));
        float rise = lineRiseFraction;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null) continue;
            float baseA = lineBase[i].a;
            float a = p < rise
                ? Mathf.Lerp(0f, 1f, p / rise)                                   // ขาขึ้น: 0 -> เต็ม
                : Mathf.Lerp(1f, baseA, Mathf.SmoothStep(0f, 1f, (p - rise) / (1f - rise)));   // ขาลง: เต็ม -> ค่าเดิม
            Color c = lineBase[i]; c.a = a;
            lines[i].color = c;
        }
    }

    void SetLinesAlpha(float a)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null) continue;
            Color c = lineBase[i]; c.a = a;
            lines[i].color = c;
        }
    }

    // ---------- 2. แผ่นดำ ----------
    void UpdateZones()
    {
        SetZone(centerZone, 0, 0);
        SetZone(topZone, 1, 1);
        SetZone(bottomZone, 2, 1);
        SetZone(leftZone, 3, 2);
        SetZone(rightZone, 4, 2);
    }

    void SetZone(CanvasGroup g, int seed, int step)
    {
        if (g == null || blackout == null) return;

        float start = zoneStart + zoneStepGap * step;
        float p = Mathf.Clamp01((t - start) / Mathf.Max(0.01f, zoneFadeTime));

        float cover = 1f - p;
        if (p > 0f && p < 1f)
        {
            // กะพริบแบบจอเก่า: แรงสุดกลางช่วงเปิด จางลงตอนใกล้เปิดเต็มที่
            float noise = Mathf.PerlinNoise(Time.unscaledTime * flickerFrequency, seed * 17.3f);
            cover += (noise - 0.5f) * 2f * flickerAmount * Mathf.Sin(p * Mathf.PI);
        }
        blackout.SetCover(g, Mathf.Clamp01(cover));
    }

    void SetAllZones(float cover)
    {
        if (blackout == null) return;
        blackout.SetCover(centerZone, cover);
        blackout.SetCover(topZone, cover);
        blackout.SetCover(bottomZone, cover);
        blackout.SetCover(leftZone, cover);
        blackout.SetCover(rightZone, cover);
    }

    // ---------- 3. HUD ----------
    void PrepareSlides()
    {
        Canvas.ForceUpdateCanvases();
        foreach (var s in slides)
        {
            if (s.target == null) continue;
            s.basePos = s.target.anchoredPosition;
            s.offset = OffscreenOffset(s.target, s.from);
            s.target.anchoredPosition = s.basePos + s.offset;   // เริ่มนอกจอ
        }
    }

    // ระยะ (หน่วย anchoredPosition ของ parent) ที่ต้องเลื่อน เพื่อให้ชิ้นนี้พ้นขอบจอด้านที่เลือก
    Vector2 OffscreenOffset(RectTransform rt, Edge edge)
    {
        var canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null) return Vector2.zero;
        var canvasRt = (RectTransform)canvas.rootCanvas.transform;

        var e = new Vector3[4];
        var c = new Vector3[4];
        rt.GetWorldCorners(e);        // [0] ล่างซ้าย, [2] บนขวา
        canvasRt.GetWorldCorners(c);

        Vector2 d = Vector2.zero;
        switch (edge)
        {
            case Edge.Left:   d.x = -(e[2].x - c[0].x) - edgeMargin; break;
            case Edge.Right:  d.x = c[2].x - e[0].x + edgeMargin; break;
            case Edge.Top:    d.y = c[2].y - e[0].y + edgeMargin; break;
            case Edge.Bottom: d.y = -(e[2].y - c[0].y) - edgeMargin; break;
        }

        float scale = rt.parent != null ? Mathf.Max(0.0001f, rt.parent.lossyScale.x) : 1f;
        return d / scale;
    }

    void UpdateSlides()
    {
        float pm = Mathf.Clamp01((t - hudStart) / Mathf.Max(0.01f, hudTime));
        float im = 1f - pm;
        HudProgress = 1f - im * im * im;                      // easeOutCubic เหมือนชิ้นอื่น

        foreach (var s in slides)
        {
            if (s.target == null) continue;
            float p = Mathf.Clamp01((t - hudStart - s.delay) / Mathf.Max(0.01f, hudTime));
            float inv = 1f - p;
            float ease = 1f - inv * inv * inv;                    // easeOutCubic
            s.target.anchoredPosition = s.basePos + s.offset * (1f - ease);
        }
    }

    float MaxSlideDelay()
    {
        float m = 0f;
        foreach (var s in slides) if (s.target != null) m = Mathf.Max(m, s.delay);
        return m;
    }

    // ---------- จบ ----------
    void Finish()
    {
        done = true;

        for (int i = 0; i < lines.Length; i++)
            if (lines[i] != null) lines[i].color = lineBase[i];
        SetAllZones(0f);
        foreach (var s in slides)
            if (s.target != null) s.target.anchoredPosition = s.basePos;

        HudProgress = 1f;
        Running = false;
        Time.timeScale = 1f;
    }
}