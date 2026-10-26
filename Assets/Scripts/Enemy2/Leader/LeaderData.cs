using UnityEngine;

namespace EnemyV2
{
    // ค่าของสมอง leader: บทบาท Guard (ยามรอบ gate) กับ Engage (สู้ผู้เล่น/kite) แยกกลุ่มกัน
    [CreateAssetMenu(menuName = "EnemyV2/LeaderData", fileName = "LeaderData_")]
    public class LeaderData : ScriptableObject
    {
        [Header("Awareness")]
        public float sightRadius = 20f;         // เห็นผู้เล่นในระยะนี้ = camp alert

        [Header("Guard: ยามรอบ gate")]
        public float guardRadius = 8f;          // จุดยามห่างจาก gate (ตอน alert อยู่บนเส้น gate→ผู้เล่น)
        public float fleeDistance = 10f;        // ผู้เล่นใกล้กว่านี้ = หนี
        public float fleeBuffer = 3f;           // หยุดหนีเมื่อห่างเกิน fleeDistance + ค่านี้
        public float maxFleeFromStation = 12f;  // หนีออกจากจุดยามได้ไกลสุด
        [Range(0f, 1f)] public float stationPull = 0.5f;   // แรงดึงเข้าจุดยามระหว่างหนี
        public float returnDelay = 1.5f;        // สงบนานเท่านี้ก่อนกลับจุดยาม
        public float blockTriggerDistance = 15f;// ผู้เล่นเข้าใกล้ leader ในระยะนี้ = ฝูงยามที่ไม่ได้โจมตียืนบัง leader (Block) ไกลกว่านี้ Guard รอบ gate
        public float guardAggroRange = 20f;     // ผู้เล่นเข้าใกล้ gate ในระยะนี้ = ฝูงยามส่วนหนึ่งออกโจมตี
        public float guardAggroBuffer = 4f;     // ต้องออกไปไกลกว่านี้อีกถึงเลิกโจมตี (กันกระพริบ)
        [Range(0f, 1f)] public float guardAttackFraction = 0.5f;   // สัดส่วนฝูงยามที่ออกโจมตี (0 = ไม่ออก) ที่เหลือบัง leader/เฝ้า gate
        public float guardFlockRadius = 6f;     // ฝูงยามประจำรอบ gate ในรัศมีนี้

        public float escortDistance = 14f;      // leader อยู่ห่าง gate เกินนี้ = ฝูงยามตาม leader (Hold) ไม่วิ่งไปรอที่ gate ก่อน
        [Range(0.1f, 0.95f)] public float escortReleaseRatio = 0.6f;   // กลับมาใกล้กว่า escortDistance × ค่านี้ ฝูงถึงไปประจำที่ gate

        [Header("Leash: ออกห่าง gate เกินระยะนี้ = กลับไปเฝ้า")]
        public float leashDistance = 70f;       // 0 = ปิด (ใช้กับ leader ที่มี camp เท่านั้น)
        [Range(0.1f, 0.95f)] public float leashReleaseRatio = 0.6f;   // กลับมาใกล้กว่า leashDistance × ค่านี้ = ออกไปสู้ได้อีก

        [Header("Engage: สู้ผู้เล่น (kite)")]
        public float keepDistance = 7f;         // ผู้เล่นใกล้กว่านี้ = ถอย
        public float retreatBuffer = 1f;        // หยุดถอยเมื่อห่างเกิน keepDistance + ค่านี้
        public float standoffDistance = 9.5f;   // ระยะที่ไปยืนจากผู้เล่น (ต้อง > keep + arrive)
        public float engageRange = 25f;         // ผู้เล่นอยู่ในระยะนี้จาก leader = สั่งฝูงโจมตี
        public int minAttackFlock = 4;          // ฝูงน้อยกว่านี้ = ไม่ส่งโจมตี (Hold)

        [Header("การเคลื่อนที่ (ร่วม)")]
        public float retreatSpeedRatio = 1f;    // ความเร็วหนี = MaxSpeed × ค่านี้
        public float cruiseSpeedRatio = 0.4f;   // ลอยช้า
        public float rushSpeedRatio = 1f;       // เร็ว
        public float rushStart = 12f;           // เป้าไกลกว่านี้ = Rush
        public float rushEnd = 5f;              // ใกล้กว่านี้ = กลับ Cruise
        public float arriveRadius = 1.5f;       // ถึงเป้าแล้วหยุดลอยนิ่ง
        public float slowZone = 3f;             // ใกล้เป้าค่อยๆ ชะลอ
        public float swayAmp = 0.5f;            // ส่ายข้างตกแต่ง (0 = ปิด)
        public float swayFreq = 1.5f;
        
        [Header("เดินไปมา (สุ่มจุดรอบจุดหมายเหมือน swarm)")]
        public float guardWanderRadius = 2.5f;    // Guard: เดินวนรอบจุดยาม (0 = ไม่เดิน)
        public float engageWanderRadius = 1.5f;   // Engage: เดินวนรอบจุดยืนห่างผู้เล่น (ถูกจำกัดไม่ให้เข้าเขตถอย)
        public float wanderRepathMin = 2f;        // สุ่มจุดใหม่ทุกกี่วินาที
        public float wanderRepathMax = 4f;

        [Header("แยกจาก leader ตัวอื่น (ตอนรุมผู้เล่นจะได้ไม่กอง)")]
        public float leaderSpacing = 6f;          // leader ใกล้กันกว่านี้เริ่มผลักออก (0 = ปิด)
        public float leaderSeparationSpeed = 3f;  // แรงผลัก (หน่วยความเร็ว)

        [Header("ฝูง")]
        public float orderInterval = 0.5f;      // ส่งคำสั่งซ้ำทุกกี่วินาที (คำสั่งหายตอนถูกเรียกกลับ)
        public float transferInterval = 1f;     // Guard เช็คโอนฝูงให้ Engage ทุกกี่วินาที
        public int guardMinFlock = 8;           // Guard โอนให้ Engage ได้จนเหลือเท่านี้ (ต่ำกว่ากติกาปกติที่ให้ได้แค่ส่วนเกินเป้า)

        void OnValidate()
        {
            arriveRadius = Mathf.Max(0.1f, arriveRadius);
            standoffDistance = Mathf.Max(standoffDistance, keepDistance + arriveRadius + 0.5f);
            rushStart = Mathf.Max(rushStart, rushEnd + 1f);
            orderInterval = Mathf.Max(0.05f, orderInterval);
            transferInterval = Mathf.Max(0.1f, transferInterval);
            minAttackFlock = Mathf.Max(0, minAttackFlock);
            guardMinFlock = Mathf.Max(0, guardMinFlock);
            guardWanderRadius = Mathf.Max(0f, guardWanderRadius);
            engageWanderRadius = Mathf.Max(0f, engageWanderRadius);
            wanderRepathMin = Mathf.Max(0.2f, wanderRepathMin);
            wanderRepathMax = Mathf.Max(wanderRepathMax, wanderRepathMin);
            leaderSpacing = Mathf.Max(0f, leaderSpacing);
            leaderSeparationSpeed = Mathf.Max(0f, leaderSeparationSpeed);
        }
    }
}