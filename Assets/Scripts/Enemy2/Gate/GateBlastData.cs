using UnityEngine;

namespace EnemyV2
{
    // ค่าของการระเบิดตอน gate ชาร์จจบ (ใช้โดย GateBlast และ GateVisual)
    [CreateAssetMenu(menuName = "EnemyV2/GateBlastData", fileName = "GateBlast_")]
    public class GateBlastData : ScriptableObject
    {
        public float radius = 18f;
        [Range(0f, 1f)] public float maxFraction = 0.4f;   // ดาเมจที่ศูนย์กลาง = สัดส่วนของ Max HP ผู้เล่น (ก่อน Resist)
        public AnimationCurve falloff = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.1f));   // X = ระยะ/รัศมี
        public float knockback = 14f;                      // ความเร็วผลักผู้เล่นที่ศูนย์กลาง (ลดตามเส้นโค้งเดียวกัน)

        void OnValidate() => radius = Mathf.Max(1f, radius);
    }
}