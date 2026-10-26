using UnityEngine;

namespace EnemyV2
{
    // ค่าของระบบ link: ฝั่งผู้สั่ง (gate/leader) ใช้สองกลุ่มแรก ฝั่งผู้ตาม (swarm) ใช้สองกลุ่มหลัง
    // ใช้ asset เดียวกันหรือแยกกันก็ได้
    [CreateAssetMenu(menuName = "EnemyV2/LinkProfile", fileName = "LinkProfile_")]
    public class LinkProfile : ScriptableObject
    {
        [Header("ผู้สั่ง: ระยะ (หน่วย world)")]
        public float connectRadius = 10f;      // swarm ที่ไม่มีเจ้านายเข้าระยะนี้ = ถูกเชื่อม
        public float commandRadius = 150f;     // ระยะสั่งการ (ไกลมาก) เกินนี้สั่งไม่ได้ / โอนไม่ได้
        [Range(0.5f, 1f)] public float recallRatio = 0.9f;      // ผู้ตามไกลถึง commandRadius × ค่านี้ = ถูกเรียกกลับ
        [Range(0.1f, 0.95f)] public float releaseRatio = 0.7f;  // กลับมาใกล้กว่า commandRadius × ค่านี้ = รับคำสั่งได้อีก

        [Header("ผู้สั่ง: จำนวนผู้ตาม")]
        public int desiredFollowers = 20;      // เป้าหมาย (ต่ำกว่านี้ = ขอเพิ่ม, เกินนี้ = ให้คนอื่นได้)
        public int maxFollowers = 40;          // รับได้สูงสุด
        public bool donateAll = false;         // true = ถูกขอแล้วให้ได้ทั้งหมด (gate) / false = ให้เฉพาะส่วนที่เกินเป้า (leader)

        [Header("ผู้ตาม: เวลา (วินาที)")]
        public float wakeTime = 1f;            // มีเจ้านายแล้วใช้เวลานี้จนตื่นเต็ม
        public float sleepTime = 1.5f;         // ไม่มีเจ้านายแล้วใช้เวลานี้จนหลับสนิท (ใช้กับภาพ)
        public float thinkInterval = 0.2f;     // ความถี่เช็คระยะ/หาเจ้านาย

        [Header("ผู้ตาม: ลบตัวที่ไม่มีเจ้านาย (ต้องครบทุกเงื่อนไข)")]
        public float orphanLifetime = 20f;           // ไม่มีเจ้านายนานเท่านี้
        public float orphanDespawnDistance = 40f;    // และอยู่ห่างผู้เล่นเท่านี้
        [Range(0f, 0.5f)] public float offscreenMargin = 0.15f;   // และอยู่นอกจอ (ขอบเผื่อเป็นสัดส่วนของจอ)

        void OnValidate()
        {
            connectRadius = Mathf.Max(0.1f, connectRadius);
            commandRadius = Mathf.Max(commandRadius, connectRadius + 1f);
            releaseRatio = Mathf.Min(releaseRatio, recallRatio - 0.05f);
            desiredFollowers = Mathf.Max(0, desiredFollowers);
            maxFollowers = Mathf.Max(maxFollowers, desiredFollowers);
            wakeTime = Mathf.Max(0.05f, wakeTime);
            sleepTime = Mathf.Max(0.05f, sleepTime);
            thinkInterval = Mathf.Max(0.05f, thinkInterval);
            orphanLifetime = Mathf.Max(0f, orphanLifetime);
            orphanDespawnDistance = Mathf.Max(0f, orphanDespawnDistance);
        }
    }
}