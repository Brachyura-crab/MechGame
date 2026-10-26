using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    // ทะเบียนกลางของผู้สั่งทั้งหมด: หาผู้สั่งที่ครอบคลุมตำแหน่ง, ตรวจว่าสองฝั่งอยู่ในระยะของกัน, จัดการ "ขอกำลังเสริม"
    public static class LinkRegistry
    {
        static readonly List<LinkSource> sources = new List<LinkSource>();
        static readonly List<LinkSource> donors = new List<LinkSource>();   // ที่พักชั่วคราว กันสร้างลิสต์ใหม่ทุกครั้ง

        public static IReadOnlyList<LinkSource> Sources => sources;

        // กัน static ค้างตอนปิด Reload Domain
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            sources.Clear();
            donors.Clear();
        }

        public static void Register(LinkSource s)
        {
            if (s != null && !sources.Contains(s)) sources.Add(s);
        }

        public static void Unregister(LinkSource s) => sources.Remove(s);

        // ผู้สั่งที่ทำงานอยู่ รับเพิ่มได้ และระยะเชื่อมครอบคลุมตำแหน่งนี้ (ซ้อนกันเลือกตัวที่ใกล้สุด)
        public static LinkSource FindCovering(Vector2 pos)
        {
            LinkSource best = null;
            float bestSqr = float.MaxValue;

            for (int i = 0; i < sources.Count; i++)
            {
                LinkSource s = sources[i];
                if (s == null || !s.CanAccept) continue;

                float r = s.ConnectRadius;
                float sq = (s.Position - pos).sqrMagnitude;
                if (sq <= r * r && sq < bestSqr) { bestSqr = sq; best = s; }
            }
            return best;
        }

        // ผู้สั่งสองฝั่งอยู่ในระยะสั่งการของกันและกันหรือไม่ (ใช้ค่าที่สั้นกว่า)
        public static bool CanReach(LinkSource a, LinkSource b)
        {
            if (a == null || b == null) return false;
            float r = Mathf.Min(a.CommandRadius, b.CommandRadius);
            return (a.Position - b.Position).sqrMagnitude <= r * r;
        }

        // ขอกำลังเสริมให้ requester จำนวน count: คืนจำนวนที่ได้จริง
        // ลำดับ: ผู้ให้แบบให้หมด (gate) ก่อน แล้วผู้ให้เฉพาะส่วนเกิน (leader) ในแต่ละกลุ่มใกล้สุดก่อน
        public static int Request(LinkSource requester, int count)
        {
            if (requester == null || !requester.IsActive || count <= 0) return 0;

            int need = Mathf.Min(count, requester.Space);
            if (need <= 0) return 0;

            donors.Clear();
            for (int i = 0; i < sources.Count; i++)
            {
                LinkSource s = sources[i];
                if (s == null || s == requester) continue;
                if (s.Donatable <= 0 || !CanReach(requester, s)) continue;
                donors.Add(s);
            }

            Vector2 origin = requester.Position;
            donors.Sort((a, b) =>
            {
                int ga = a.Profile.donateAll ? 0 : 1;
                int gb = b.Profile.donateAll ? 0 : 1;
                if (ga != gb) return ga.CompareTo(gb);
                return (a.Position - origin).sqrMagnitude.CompareTo((b.Position - origin).sqrMagnitude);
            });

            int got = 0;
            for (int i = 0; i < donors.Count && got < need; i++)
            {
                LinkSource d = donors[i];
                int give = Mathf.Min(need - got, d.Donatable);
                got += d.TransferTo(requester, give);
            }

            donors.Clear();
            return got;
        }
    }
}