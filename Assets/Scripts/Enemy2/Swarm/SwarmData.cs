using UnityEngine;

namespace EnemyV2
{
    // ค่าของสมอง swarm (ความเร็วฐาน/การเร่ง/การเบรกมาจาก Stat ของ prefab เหมือนเดิม  ดาเมจ/ระยะ/คูลดาวน์โจมตีมาจาก AttackData)
    [CreateAssetMenu(menuName = "EnemyV2/SwarmData", fileName = "SwarmData_")]
    public class SwarmData : ScriptableObject
    {
        [Header("ความเร็ว")]
        [Range(0.1f, 1.5f)] public float cruiseSpeedRatio = 0.8f;   // เดินปกติ = MaxSpeed × ค่านี้
        public float recallSpeedRatio = 1.3f;                       // ตอนถูกเรียกกลับ = ความเร็วเดิน × ค่านี้
        public float retreatSpeedRatio = 1.0f;                      // ตอน Retreat = ความเร็วเดิน × ค่านี้
        public float arriveDecel = 12f;                             // เบรกตอนใกล้ถึงจุดหมาย (ไม่ใช่ตอนโจมตี) ใช้ค่าที่มากกว่า Deceleration ของ Stat

        [Header("เร่งความเร็วเข้าหาเป้า (คำสั่ง Attack) ระยะวัดจากเป้า")]
        public float boostRange = 12f;          // เข้าระยะนี้ = เริ่มเร่ง
        public float boostStopRange = 3f;       // เข้าระยะนี้ = เลิกเร่ง (ความเร็วค่อยๆ ลดกลับด้วย Deceleration ของ Stat: ต่ำ = ไถลเลยเป้า)
        public float boostWindupTime = 0.35f;  // ง้างนิ่งก่อนเริ่มเร่ง (สุ่ม ±20%) 0 = ไม่ง้าง
        public float windupRetryDelay = 1f;    // ถูกยิงขัดการง้าง = รอเท่านี้ก่อนง้างใหม่
        public float boostDuration = 3f;        // เร่งนานสุดเท่านี้ (วินาที) ถ้ายังไม่ถึงระยะหยุดเร่ง
        public float boostSpeedRatio = 1.5f;    // ความเร็วตอนเร่ง = MaxSpeed × ค่านี้
        public float boostCooldown = 0f;        // พักหลังเร่งจบก่อนเร่งใหม่ได้ (นอกจากนี้ต้องออกนอกระยะเร่งก่อนด้วย)
        public bool boostRequiresReady = true;  // ง้าง/เร่งได้เฉพาะตอนโจมตีพร้อม (กันพุ่งไปชนแล้วตีไม่ได้เพราะติด cooldown)
        [Range(0.1f, 1f)] public float attackStopFactor = 0.8f;   // อยู่ในระยะโจมตี × ค่านี้ = เลิกเดินเข้าหา

        [Header("ท่าว่าง (Hold / Guard / Assist): เดินวนรอบจุด")]
        public float holdRadius = 6f;           // Hold: รอบเจ้านาย
        public float assistRadius = 5f;         // Assist: รอบผู้สั่งที่ไปช่วย
        [Range(0.1f, 1f)] public float idleSpeedRatio = 0.6f;     // × ความเร็วเดิน
        public float idleRepathMin = 2f;
        public float idleRepathMax = 4f;

        [Header("Block")]
        public float blockOffset = 2f;          // ยืนห่างจากผู้ถูกป้องกันไปทางภัยเท่านี้

        [Header("แยกตัวไม่ซ้อนกัน")]
        public LayerMask enemyLayers;           // ตั้งเป็น Layer "Enemy"
        public float separationRadius = 1.2f;   // 0 = ปิด
        public float separationSpeed = 2f;
        public float separationInterval = 0.1f;

        [Header("ชะงักเมื่อโดนโจมตี (ช้าลงชั่วคราว + ตัดการเร่ง)")]
        public float staggerTime = 0.5f;                          // 0 = ไม่ชะงัก
        [Range(0f, 1f)] public float staggerSpeedRatio = 0.14f;   // ความเร็วระหว่างชะงัก = MaxSpeed × ค่านี้
        public float staggerMinDamage = 0f;
        public float staggerImmunityTime = 0.5f;

        void OnValidate()
        {
            boostRange = Mathf.Max(0.5f, boostRange);
            boostStopRange = Mathf.Clamp(boostStopRange, 0f, boostRange - 0.1f);
            boostDuration = Mathf.Max(0f, boostDuration);
            boostWindupTime = Mathf.Max(0f, boostWindupTime);
            windupRetryDelay = Mathf.Max(0f, windupRetryDelay);
            boostSpeedRatio = Mathf.Max(0.1f, boostSpeedRatio);
            boostCooldown = Mathf.Max(0f, boostCooldown);
            arriveDecel = Mathf.Max(0.1f, arriveDecel);
            idleRepathMax = Mathf.Max(idleRepathMax, idleRepathMin);
            separationInterval = Mathf.Max(0.02f, separationInterval);
        }
    }
}