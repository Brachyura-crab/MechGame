using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    // ติดที่ gate คู่กับ Camp: alert ชุดเดียวของ camp + เลือกบทบาท (leader ที่ใกล้ผู้เล่นที่สุดเป็น Engage ตอน alert)
    // ถ้า gate หายไป (ปิดแล้ว) component นี้หายด้วย leader ที่เหลือจะเป็น Engage ตลอดเอง
    [RequireComponent(typeof(Camp))]
    public class CampCommander : MonoBehaviour
    {
        [SerializeField] float alertHold = 3f;          // alert ค้างหลังเห็น/โดนยิงครั้งล่าสุด (วินาที)
        [SerializeField] float roleInterval = 0.5f;     // ความถี่เลือกบทบาท
        [SerializeField] float roleSwitchMargin = 5f;   // ต้องใกล้ผู้เล่นกว่า Engage ปัจจุบันเกินเท่านี้ถึงสลับ (กันสลับไปมา)

        Camp camp;
        float alertUntil;
        Vector2 lastThreat;
        CampMember engager;
        CampMember forced;
        float forcedUntil;
        float nextRole;

        public bool Alerted => Time.time < alertUntil;
        public Vector2 LastThreat => lastThreat;
        public CampMember Engager => engager;

        void Awake() => camp = GetComponent<Camp>();

        // leader เห็นผู้เล่น หรือสมาชิกถูกยิง
        public void Alert(Vector2 pos)
        {
            alertUntil = Time.time + alertHold;
            lastThreat = pos;
        }

        public bool IsEngager(CampMember m) => m != null && m == engager;

        // debug: บังคับให้ m เป็น Engage ชั่วคราว (และ alert ตลอดช่วงนั้น)
        public void ForceEngager(CampMember m, float seconds)
        {
            forced = m;
            forcedUntil = Time.time + seconds;
            alertUntil = Mathf.Max(alertUntil, forcedUntil);
        }

        void Update()
        {
            if (Time.time < nextRole) return;
            nextRole = Time.time + roleInterval;
            UpdateRoles();
        }

        void UpdateRoles()
        {
            if (forced != null && Time.time < forcedUntil && IsMember(forced)) { engager = forced; return; }

            if (!Alerted || !EnemyTarget.Ensure()) { engager = null; return; }

            Vector2 pp = EnemyTarget.Transform.position;
            CampMember best = null;
            float bestD = float.MaxValue;
            float curD = float.MaxValue;

            IReadOnlyList<CampMember> list = camp.Members;
            for (int i = 0; i < list.Count; i++)
            {
                CampMember m = list[i];
                if (m == null || !m.IsLeader || !m.Source.IsActive) continue;
                if (m.TryGetComponent(out LeaderBrain lb) && lb.Returning) continue;

                float d = Vector2.Distance(m.transform.position, pp);
                if (m == engager) curD = d;
                if (d < bestD) { bestD = d; best = m; }
            }

            if (engager == null || !IsMember(engager) || curD == float.MaxValue) engager = best;       // Engage เดิมหาย/ตาย
            else if (best != engager && bestD + roleSwitchMargin < curD) engager = best;              // มีตัวที่ใกล้กว่าชัดเจน
        }

        bool IsMember(CampMember m)
        {
            IReadOnlyList<CampMember> list = camp.Members;
            for (int i = 0; i < list.Count; i++) if (list[i] == m) return true;
            return false;
        }
    }
}