using UnityEngine;

namespace EnemyV2
{
    // ค่าของ camp: จำนวนเป้าหมาย การจัดทัพ การเติม ปรับสมดุลฝูง final wave และเพดาน
    [CreateAssetMenu(menuName = "EnemyV2/CampData", fileName = "CampData_")]
    public class CampData : ScriptableObject
    {
        [Header("จำนวนเป้าหมายของ camp")]
        public int leaderTarget = 2;            // leader ที่ยังมีชีวิต
        public int swarmTarget = 60;            // swarm ที่มีเจ้านาย (ตัวค้างไม่นับ gate เสกชดเชย)

        [Header("จัดทัพครั้งแรก")]
        public float spawnRadius = 110f;        // ผู้เล่นเข้าระยะนี้ครั้งแรก = จัดทัพ (จัดแล้วค้างตลอด ออกไปก็ไม่รีเซ็ต)
        public int spawnPerFrame = 4;           // ทยอยเสกต่อเฟรม กันกระตุก
        public float deployScatter = 3f;        // swarm ที่จัดทัพ กระจายรอบเจ้านาย

        [Header("เติมตัวที่ขาด (แยกชนิด)")]
        public float leaderRefillInterval = 3f; // วินาทีต่อ leader 1 ตัว
        public float swarmRefillInterval = 0.5f;// วินาทีต่อ swarm 1 ตัว
        public float gateScatter = 1.5f;        // swarm ที่เติมเกิดรอบ gate
        public float leaderOffset = 2f;         // leader เกิดเยื้องจาก gate ไปทางผู้เล่น
        public float leaderScatter = 1f;

        [Header("ปรับสมดุลฝูง")]
        public bool autoBalance = true;         // leader ที่ฝูงต่ำกว่าเป้า ให้ camp เรียกขอกำลังเสริมให้
        public float balanceInterval = 1f;

        [Header("Final wave (ตอน gate ระเบิด ไม่นับเข้า camp ไม่ติดเพดาน)")]
        public int finalLeaders = 1;
        public int finalSwarm = 18;             // ผูกกับ leader ของ final wave
        public float finalScatter = 4f;

        [Header("เพดาน")]
        public int maxAlivePerGate = 90;        // ทุกตัวของ camp นี้ รวมตัวค้าง (เติมหยุดเมื่อถึง)
        public int maxAlive = 220;              // รวมทุก camp

        [Header("Misc")]
        public float checkInterval = 0.25f;     // ความถี่นับจำนวนสมาชิก

        void OnValidate()
        {
            leaderTarget = Mathf.Max(0, leaderTarget);
            swarmTarget = Mathf.Max(0, swarmTarget);
            spawnRadius = Mathf.Max(1f, spawnRadius);
            spawnPerFrame = Mathf.Max(1, spawnPerFrame);
            leaderRefillInterval = Mathf.Max(0.05f, leaderRefillInterval);
            swarmRefillInterval = Mathf.Max(0.05f, swarmRefillInterval);
            balanceInterval = Mathf.Max(0.1f, balanceInterval);
            finalLeaders = Mathf.Max(0, finalLeaders);
            finalSwarm = Mathf.Max(0, finalSwarm);
            maxAlivePerGate = Mathf.Max(1, maxAlivePerGate);
            maxAlive = Mathf.Max(maxAlivePerGate, maxAlive);
            checkInterval = Mathf.Max(0.05f, checkInterval);
        }
    }
}