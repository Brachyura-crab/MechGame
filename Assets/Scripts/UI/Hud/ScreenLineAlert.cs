using UnityEngine;
using UnityEngine.UI;

// เส้นแบ่งหน้าจอ: แฟลชแดงตอนโดนโจมตี + กระพริบเหลือง/แดงตามความต่ำของ HP (ยิ่งน้อยยิ่งถี่)
// ลากเส้น (Image/Graphic ใดก็ได้) ใส่ช่อง Lines ไม่ต้องแก้ตัวเส้น
public class ScreenLineAlert : MonoBehaviour
{
    [Header("อ้างอิง")]
    [SerializeField] Health health;
    [SerializeField] Graphic[] lines;

    [Header("สี")]
    [SerializeField] Color warnColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] Color dangerColor = new Color(1f, 0.2f, 0.15f);
    [SerializeField] Color hitColor = new Color(1f, 0.1f, 0.1f);

    [Header("เกณฑ์ HP (สัดส่วน 0-1)")]
    [SerializeField, Range(0f, 1f)] float warnRatio = 0.6f;     // ต่ำกว่านี้ = เริ่มกระพริบสีเหลือง
    [SerializeField, Range(0f, 1f)] float dangerRatio = 0.3f;   // ต่ำกว่านี้ = สีแดง

    [Header("กระพริบตาม HP")]
    [SerializeField] float minHz = 1f;    // ความถี่ตอนเริ่มเตือน (ครั้งต่อวินาที)
    [SerializeField] float maxHz = 6f;    // ความถี่ตอน HP ใกล้ 0
    [SerializeField, Range(0f, 1f)] float blinkMinAlpha = 0.15f;   // ตอนดับสุดของแต่ละจังหวะ

    [Header("แฟลชตอนโดนโจมตี")]
    [SerializeField] float hitFlashTime = 0.3f;

    Color[] baseColors;
    float hitT;   // 1 = เพิ่งโดน แล้วลดเป็น 0

    void Awake()
    {
        baseColors = new Color[lines.Length];
        for (int i = 0; i < lines.Length; i++)
            baseColors[i] = lines[i] != null ? lines[i].color : Color.white;
    }

    void OnEnable()
    {
        if (health != null) health.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        if (health != null) health.Damaged -= OnDamaged;
        Restore();
    }

    void OnDamaged(DamageResult r) => hitT = 1f;

    void Restore()
    {
        if (baseColors == null) return;
        for (int i = 0; i < lines.Length; i++)
            if (lines[i] != null) lines[i].color = baseColors[i];
    }

    void Update()
    {
        if (health == null || BootSequence.Running) return;

        float dt = Time.unscaledDeltaTime;
        hitT = Mathf.MoveTowards(hitT, 0f, dt / Mathf.Max(0.01f, hitFlashTime));

        float max = health.Max;
        float ratio = max > 0f ? Mathf.Clamp01(health.Current / max) : 0f;

        bool alert = ratio < warnRatio || health.IsDead;
        Color stateColor = baseColors.Length > 0 ? baseColors[0] : Color.white;
        float alpha = 1f;

        if (alert)
        {
            stateColor = (ratio < dangerRatio || health.IsDead) ? dangerColor : warnColor;

            if (health.IsDead) alpha = 1f;   // ตายแล้ว แดงค้างไม่กระพริบ
            else
            {
                float severity = 1f - ratio / Mathf.Max(0.0001f, warnRatio);   // 0 ที่เกณฑ์เตือน .. 1 ที่ HP = 0
                float hz = Mathf.Lerp(minHz, maxHz, severity);
                float wave = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * hz * Mathf.PI * 2f);
                alpha = Mathf.Lerp(blinkMinAlpha, 1f, wave);
            }
        }

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null) continue;

            Color c;
            if (alert) { c = stateColor; c.a = alpha; }
            else c = baseColors[i];                          // HP ปกติ = สีเดิมของเส้น

            if (hitT > 0f)                                   // แฟลชแดงทับ
            {
                c = Color.Lerp(c, hitColor, hitT);
                c.a = Mathf.Lerp(c.a, 1f, hitT);
            }

            lines[i].color = c;
        }
    }
}