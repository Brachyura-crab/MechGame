using UnityEngine;
using UnityEngine.Rendering.Universal;

// ไล่ความเข้มของแสงติดตัวหุ่นตามสถานะ Boost
public class BoostLight : MonoBehaviour
{
    [SerializeField] PlayerMove playerMove;
    [SerializeField] Light2D boostLight;
    [SerializeField] float maxIntensity = 1.2f;
    [SerializeField] float fadeInTime = 0.1f;     // วินาทีที่ใช้สว่างขึ้นเต็ม
    [SerializeField] float fadeOutTime = 0.25f;   // วินาทีที่ใช้ดับสนิท

    float level;   // 0 ถึง 1

    void Awake()
    {
        if (boostLight != null) boostLight.enabled = false;
    }

    void LateUpdate()
    {
        if (playerMove == null || boostLight == null) return;

        float rate = playerMove.IsBoosting
            ? 1f / Mathf.Max(0.01f, fadeInTime)
            : -1f / Mathf.Max(0.01f, fadeOutTime);

        level = Mathf.Clamp01(level + rate * Time.deltaTime);

        boostLight.enabled = level > 0.001f;   // ปิดทั้งดวงเมื่อดับ ไม่เสียภาระตอนไม่ใช้
        boostLight.intensity = maxIntensity * level;
    }
}