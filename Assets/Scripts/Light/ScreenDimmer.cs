using UnityEngine;
using UnityEngine.Rendering.Universal;

// ทำให้ฉากมืดชั่วขณะโดยหรี่ Global Light 2D (เรียก Pulse ตอนยิง)
// เฉพาะ sprite ที่ใช้ material แบบ Lit เท่านั้นที่มืดลง glow แบบ Additive/Unlit ยังสว่างเท่าเดิม
public class ScreenDimmer : MonoBehaviour
{
    public static ScreenDimmer Instance { get; private set; }

    [SerializeField] Light2D globalLight;                    // ว่างไว้ = หา Global Light 2D ตัวแรกเอง
    [Range(0f, 1f)]
    [SerializeField] float dimmedRatio = 0.35f;              // ความสว่างที่เหลือเมื่อมืดสุด (0.35 = เหลือ 35%)
    [SerializeField] float decayTime = 0.18f;                // วินาทีที่ใช้สว่างกลับจากมืดสุด
    [Range(0f, 1f)]
    [SerializeField] float intensity = 1f;                   // ตัวคูณรวม ไว้ทำตัวเลือกผู้เล่น (0 = ปิด)

    public float Intensity { get => intensity; set => intensity = Mathf.Clamp01(value); }

    float baseIntensity;
    float level;
    float decayNow;   // เวลาที่ใช้จางจากมืดสุดของ pulse ล่าสุด

    void Awake()
    {
        Instance = this;

        if (globalLight == null)
        {
            foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (l.lightType == Light2D.LightType.Global) { globalLight = l; break; }
            }
        }

        if (globalLight == null)
        {
            Debug.LogWarning("ScreenDimmer: ไม่พบ Global Light 2D");
            enabled = false;
            return;
        }

        baseIntensity = globalLight.intensity;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // duration > 0 = เวลาที่ใช้จางจากมืดสุด (ระเบิดใช้ค่ายาวกว่าปืน) 0 = ใช้ Decay Time ตามปกติ
    public void Pulse(float strength, float duration = 0f)
    {
        float s = Mathf.Clamp01(strength);
        if (s < level) return;   // ต้องมืดกว่าที่เป็นอยู่ถึงจะแทนที่ ปืนจึงไม่ตัดความมืดของระเบิด
        level = s;
        decayNow = duration > 0f ? duration : decayTime;
    }

    void LateUpdate()
    {
        float k = level * intensity;
        globalLight.intensity = Mathf.Lerp(baseIntensity, baseIntensity * dimmedRatio, k);
        level = Mathf.MoveTowards(level, 0f, Time.deltaTime / Mathf.Max(0.01f, decayNow > 0f ? decayNow : decayTime));
    }
}