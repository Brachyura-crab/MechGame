using UnityEngine;

// กล้องสั่น + จอมืดลงตอนเกิดระเบิด ความแรงตามระยะจากกล้องถึงจุดระเบิด
// ใส่ไว้ที่ object ใดก็ได้ในฉาก (เช่น Main Camera) ฉากละหนึ่งตัว
public class ExplosionScreenFx : MonoBehaviour
{
    public static ExplosionScreenFx Instance { get; private set; }

    [Header("ระยะ (หน่วยโลก วัดจากขอบวงระเบิด)")]
    [SerializeField] float shakeMaxDistance = 16f;
    [SerializeField] float dimMaxDistance = 9f;

    [Header("กล้องสั่น")]
    [SerializeField] float shakeMaxAmplitude = 0.6f;     // ความแรงสูงสุดเมื่ออยู่ในวงระเบิด
    [SerializeField] float shakeDuration = 0.45f;        // วินาทีที่ใช้จางหมด
    [Tooltip("แกน X = ระยะ (0 ถึง 1 ของระยะสูงสุด), แกน Y = ตัวคูณความแรง")]
    [SerializeField] AnimationCurve shakeFalloff = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.3f, 0.6f), new Keyframe(1f, 0f));

    [Header("จอมืด")]
    [Range(0f, 1f)]
    [SerializeField] float dimMaxStrength = 1f;          // 1 = มืดสุดตาม Dimmed Ratio ของ ScreenDimmer
    [SerializeField] float dimDuration = 0.5f;           // วินาทีที่ใช้สว่างกลับจากมืดสุด
    [Tooltip("แกน X = ระยะ (0 ถึง 1 ของระยะสูงสุด), แกน Y = ตัวคูณความแรง")]
    [SerializeField] AnimationCurve dimFalloff = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.4f, 0.4f), new Keyframe(1f, 0f));

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // scale = ตัวคูณความแรงรวม (เผื่อสกิลอื่นใช้ร่วมกันและต้องการแรงต่างกัน)
    public static void Trigger(Vector2 point, float blastRadius, float scale = 1f, bool shake = true)
    {
        if (Instance != null) Instance.Play(point, blastRadius, scale, shake);
    }

        void Play(Vector2 point, float blastRadius, float scale, bool shake)
    {
        Camera cam = Camera.main;
        Vector2 viewer = cam != null ? (Vector2)cam.transform.position : point;
        float edgeDist = Mathf.Max(0f, Vector2.Distance(viewer, point) - blastRadius);

        float shakeK = shakeFalloff.Evaluate(Mathf.Clamp01(edgeDist / shakeMaxDistance));
        float dimK = dimFalloff.Evaluate(Mathf.Clamp01(edgeDist / dimMaxDistance));

        if (shake && shakeK > 0.001f && CameraShake.Instance != null)
            CameraShake.Instance.Kick(shakeMaxAmplitude * shakeK * scale, shakeDuration);

        if (dimK > 0.001f && ScreenDimmer.Instance != null)
            ScreenDimmer.Instance.Pulse(dimMaxStrength * dimK * scale, dimDuration);
    }
}