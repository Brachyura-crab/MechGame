using UnityEngine;

namespace EnemyV2
{
    // ค่าของ gate ฝั่งพลังงาน/ระยะ/การปิด (ค่าระเบิดอยู่ใน GateBlastData, ค่าการเสกอยู่ฝั่ง camp)
    [CreateAssetMenu(menuName = "EnemyV2/GateData", fileName = "GateData_")]
    public class GateData : ScriptableObject
    {
        [Header("Energy")]
        public float maxEnergy = 100f;
        public float chargeTime = 2f;        // พลังงานหมดแล้วชาร์จนานเท่านี้ (วินาที) จึงระเบิดและปิด

        [Header("Activation")]
        public float activeRadius = 30f;     // ผู้เล่นอยู่ในระยะนี้: ฆ่าศัตรูของ gate แล้วลดพลังงาน (กันฟาร์ม)
        public float exitBuffer = 2f;        // ต้องออกไปไกลกว่านี้อีกเท่าไรถึงถือว่าออก (กันกระพริบที่ขอบ)

        [Header("Closing")]
        public float destroyDelay = 0f;      // ปิดแล้วลบ gate หลังกี่วินาที (เผื่อให้เอฟเฟกต์ที่ติดกับ gate เล่นจบ)

        void OnValidate()
        {
            maxEnergy = Mathf.Max(1f, maxEnergy);
            chargeTime = Mathf.Max(0f, chargeTime);
            activeRadius = Mathf.Max(1f, activeRadius);
            exitBuffer = Mathf.Max(0f, exitBuffer);
            destroyDelay = Mathf.Max(0f, destroyDelay);
        }
    }
}