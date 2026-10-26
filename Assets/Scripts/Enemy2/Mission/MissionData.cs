using UnityEngine;

namespace EnemyV2
{
    // ค่าของภารกิจ: การวาง gate, จุดรับ, และการออกนอกพื้นที่ภารกิจ (แมพไม่จำกัด จึงตัดจบเมื่อผู้เล่นออกไกลเกิน)
    [CreateAssetMenu(menuName = "EnemyV2/MissionData", fileName = "MissionData_")]
    public class MissionData : ScriptableObject
    {
        [Header("Gates (วางรอบจุดส่ง)")]
        public int gateCount = 3;
        public float minDistance = 150f;
        public float maxDistance = 220f;
        public float minGateSpacing = 120f;      // ระยะห่างต่ำสุดระหว่าง gate

        [Header("จุดรับ (จุดเดียวกับจุดส่ง)")]
        public float extractRadius = 15f;
        public float extractHold = 5f;           // ปิด gate ครบแล้วอยู่ในโซนครบเท่านี้ = ชนะ (ของชั่วคราวแทนยานมารับ)

        [Header("ออกนอกพื้นที่ภารกิจ")]
        public float warnRadius = 320f;          // ห่างจากจุดส่งเกินนี้ = เริ่มนับถอยหลัง (ต้องมากกว่าระยะ gate + camp)
        public float abandonCountdown = 10f;     // นับครบ = ภารกิจล้มเหลว กลับเข้าพื้นที่ก่อนครบ = รีเซ็ต

        void OnValidate()
        {
            gateCount = Mathf.Max(1, gateCount);
            minDistance = Mathf.Max(1f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);
            minGateSpacing = Mathf.Max(0f, minGateSpacing);
            extractRadius = Mathf.Max(1f, extractRadius);
            extractHold = Mathf.Max(0f, extractHold);
            warnRadius = Mathf.Max(maxDistance + 50f, warnRadius);
            abandonCountdown = Mathf.Max(0f, abandonCountdown);
        }
    }
}