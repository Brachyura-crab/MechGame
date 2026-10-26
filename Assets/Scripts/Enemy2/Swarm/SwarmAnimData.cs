using UnityEngine;

namespace EnemyV2
{
    // ชุด frame ท่าโจมตีของ swarm เรียงตามลำดับ: frame แรก = ท่าพัก, ถึง windupFrames = ท่าง้าง, ที่เหลือ = ท่าโจมตี
    [CreateAssetMenu(menuName = "EnemyV2/SwarmAnimData", fileName = "SwarmAnim_")]
    public class SwarmAnimData : ScriptableObject
    {
        [Header("Sprite (เรียง frame 1..N จำนวนเท่ากัน)")]
        public Sprite[] body;
        public Sprite[] glow;      // เว้นว่างทั้งชุด = ไม่เปลี่ยน sprite ของ glow
        public Sprite[] effect;    // ช่องว่าง = ซ่อน effect ใน frame นั้น (ว่างทั้งชุด = ไม่ใช้)

        [Header("โครงท่า")]
        [Min(1)] public int windupFrames = 3;      // frame 1..ค่านี้ = ท่าง้าง (ค้างที่ frame สุดท้ายของชุดนี้ตอนพุ่ง)

        [Header("ความเร็วเล่น (frame/วินาที)")]
        public float strikeFps = 24f;              // ท่าโจมตี
        public float reverseFps = 24f;             // ย้อนกลับท่าพัก

        [Header("พลาด")]
        public float holdAfterBoost = 0.3f;        // boost จบแล้วยังไม่โจมตี ค้างท่าง้างนานเท่านี้ก่อนย้อน (swarm ไถลต่ออยู่)

        public int FrameCount => body != null ? body.Length : 0;

        void OnValidate()
        {
            windupFrames = Mathf.Max(1, windupFrames);
            strikeFps = Mathf.Max(1f, strikeFps);
            reverseFps = Mathf.Max(1f, reverseFps);
            holdAfterBoost = Mathf.Max(0f, holdAfterBoost);
        }
    }
}