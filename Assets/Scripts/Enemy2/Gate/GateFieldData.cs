using UnityEngine;

namespace EnemyV2
{
    // ค่าของสนามอันตรายรอบ gate (ดู GateField)
    [CreateAssetMenu(menuName = "EnemyV2/GateFieldData", fileName = "GateFieldData_")]
    public class GateFieldData : ScriptableObject
    {
        [Header("สนาม (ปกติ)")]
        public float radius = 7f;                 // ผู้เล่นเข้าในรัศมีนี้ = โดนสนาม (แรงขึ้นเรื่อยๆ เมื่อเข้าใกล้กลาง)

        [Header("ลดความเร็ว (ทวีคูณตามความลึก)")]
        [Range(0.02f, 1f)] public float minSpeedMul = 0.2f;   // ความเร็วที่ใจกลาง = ปกติ × ค่านี้ (ที่ขอบ = x1)
        public bool slowBoost = true;             // ลด BoostSpeed ด้วย
        public bool slowDash = false;             // ลด DashSpeed ด้วย (false = พุ่งหนีได้เต็มแรง)

        [Header("ดาเมจต่อเนื่อง (ผ่านเกราะตามปกติ)")]
        public float damagePerSecond = 4f;        // ที่ใจกลาง (หน่วยดาเมจก่อนหักเกราะ/ต้านทาน)
        [Range(0.5f, 4f)] public float damagePower = 2f;      // ความชัน: สูง = ที่ขอบแทบไม่เจ็บ เจ็บแรงเมื่อลึก
        [Range(0.1f, 1f)] public float tickInterval = 0.25f;

        [Header("ตอน Charging (กว้างมาก แต่เบามาก)")]
        public float chargeRadiusMul = 3f;        // รัศมี × ค่านี้
        [Range(0f, 1f)] public float chargeDamageMul = 0.15f; // ดาเมจ × ค่านี้
        [Range(0f, 1f)] public float chargeSpeedStrength = 0.4f;   // แรงลดความเร็ว (1 = เท่าปกติ 0 = ไม่ลดเลย)
        public float chargeRampTime = 0.6f;       // ใช้เวลานี้ขยายจากปกติเป็นแบบ Charging

        void OnValidate()
        {
            radius = Mathf.Max(0.5f, radius);
            damagePerSecond = Mathf.Max(0f, damagePerSecond);
            chargeRadiusMul = Mathf.Max(1f, chargeRadiusMul);
            chargeRampTime = Mathf.Max(0.01f, chargeRampTime);
        }
    }
}