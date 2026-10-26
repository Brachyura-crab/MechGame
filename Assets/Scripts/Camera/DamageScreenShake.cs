using UnityEngine;

// กล้องสั่นตอนผู้เล่นโดนดาเมจ (ใช้ CameraShake ตัวเดิม) ยิ่งโดนแรงยิ่งสั่นแรง
// ดาเมจที่โล่ดูดสั่นเบากว่าดาเมจที่ทะลุถึง HP
public class DamageScreenShake : MonoBehaviour
{
    [SerializeField] Health health;   // Health ของผู้เล่น

    [Header("ความแรง (หน่วยโลก)")]
    [SerializeField] float minAmplitude = 0.08f;       // โดนเบาที่สุด
    [SerializeField] float maxAmplitude = 0.5f;        // โดนแรงสุด
    [SerializeField] float fullDamage = 40f;           // ดาเมจเท่านี้ขึ้นไป = สั่นแรงสุด
    [SerializeField, Range(0f, 1f)] float shieldFactor = 0.4f;   // โล่ดูดดาเมจ นับเป็นกี่ส่วนของดาเมจที่โดน HP

    [Header("เวลา")]
    [SerializeField] float duration = 0.3f;            // วินาทีที่ใช้จางหมด

    void OnEnable()
    {
        if (health != null) health.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        if (health != null) health.Damaged -= OnDamaged;
    }

    void OnDamaged(DamageResult r)
    {
        if (CameraShake.Instance == null) return;

        float effective = r.hpDamage + r.shieldDamage * shieldFactor;
        if (effective <= 0f) return;

        float t = Mathf.Clamp01(effective / Mathf.Max(0.01f, fullDamage));
        CameraShake.Instance.Kick(Mathf.Lerp(minAmplitude, maxAmplitude, t), duration);
    }
}