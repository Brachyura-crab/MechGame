using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    // ติดที่ gate และ leader: ถือรายชื่อผู้ตาม ส่งคำสั่ง และโอนผู้ตามให้ผู้สั่งคนอื่น
    // ตัวนี้ไม่รู้ว่าเจ้าของเป็น gate หรือ leader: เจ้าของเรียก Deactivate() เองเมื่อตาย/ปิด
    public class LinkSource : MonoBehaviour
    {
        [SerializeField] LinkProfile profile;

        readonly List<Follower> followers = new List<Follower>();
        readonly List<Follower> scratch = new List<Follower>();
        bool active = true;

        public LinkProfile Profile => profile;
        public bool IsActive => active;
        public Vector2 Position => transform.position;
        public float ConnectRadius => profile.connectRadius;
        public float CommandRadius => profile.commandRadius;

        public IReadOnlyList<Follower> Followers => followers;
        public int Count => followers.Count;
        public int Desired => profile.desiredFollowers;
        public int Space => Mathf.Max(0, profile.maxFollowers - followers.Count);       // รับเพิ่มได้อีกกี่ตัว
        public int Missing => Mathf.Max(0, profile.desiredFollowers - followers.Count); // ขาดจากเป้ากี่ตัว
        public int Surplus => Mathf.Max(0, followers.Count - profile.desiredFollowers); // เกินเป้ากี่ตัว

        // ถูกขอแล้วให้ได้กี่ตัว: gate (donateAll) ให้ได้หมด, leader ให้เฉพาะส่วนเกิน
        public int Donatable => !active ? 0 : (profile.donateAll ? followers.Count : Surplus);

        public bool CanAccept => active && isActiveAndEnabled && followers.Count < profile.maxFollowers;

        public event System.Action CountChanged;   // จำนวนผู้ตามเปลี่ยน (เข้า/ออก/โอน)

        void Awake()
        {
            if (profile == null)
            {
                Debug.LogError("LinkSource: ต้องใส่ LinkProfile", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            active = true;
            LinkRegistry.Register(this);
        }

        void OnDisable()
        {
            LinkRegistry.Unregister(this);
            DetachAll();
        }

        // เจ้าของเรียกเมื่อ leader ตาย / gate ปิด: เลิกเป็นผู้สั่ง ผู้ตามทุกตัวกลายเป็นไม่มีเจ้านาย
        public void Deactivate()
        {
            if (!active) return;
            active = false;
            DetachAll();
        }

        // ---------- รับ/ปล่อยผู้ตาม ----------

        // รับผู้ตามเข้ามา (ถ้ามีเจ้านายเดิมจะย้ายออกจากลิสต์เดิมให้) คืน false ถ้ารับไม่ได้
        public bool TryAttach(Follower f)
        {
            if (f == null || f.IsDead || !CanAccept) return false;
            if (f.Owner == this) return true;

            LinkSource old = f.Owner;
            if (old != null) old.followers.Remove(f);

            followers.Add(f);
            f.OnAttached(this);

            if (old != null) old.CountChanged?.Invoke();
            CountChanged?.Invoke();
            return true;
        }

        // ปล่อยผู้ตามตัวนี้ (ตาย/ถูกลบ) ผู้ตามจะกลายเป็นไม่มีเจ้านาย
        public void Detach(Follower f)
        {
            if (f == null || !followers.Remove(f)) return;
            f.OnDetached(this);
            CountChanged?.Invoke();
        }

        void DetachAll()
        {
            if (followers.Count == 0) return;

            for (int i = followers.Count - 1; i >= 0; i--)
            {
                Follower f = followers[i];
                followers.RemoveAt(i);
                if (f != null) f.OnDetached(this);
            }
            CountChanged?.Invoke();
        }

        // ---------- คำสั่ง ----------

        // สั่งทุกตัวที่รับคำสั่งได้ (ตื่นแล้ว และอยู่ในระยะ) คืนจำนวนที่รับ
        public int Issue(LinkOrder order)
        {
            int n = 0;
            for (int i = 0; i < followers.Count; i++)
                if (followers[i].TryOrder(order)) n++;
            return n;
        }

        // สั่งไม่เกิน count ตัว (ตามลำดับในลิสต์) คืนจำนวนที่รับ
        public int Issue(LinkOrder order, int count)
        {
            int n = 0;
            for (int i = 0; i < followers.Count && n < count; i++)
                if (followers[i].TryOrder(order)) n++;
            return n;
        }

        public bool Issue(Follower f, LinkOrder order) => f != null && f.Owner == this && f.TryOrder(order);

        // ---------- โอน/ขอ ----------

        // โอนผู้ตามไม่เกิน count ตัวให้ผู้สั่งอีกคน คืนจำนวนที่โอนจริง
        // เงื่อนไข: สองฝั่งอยู่ในระยะสั่งการของกัน, ปลายทางรับได้, เฉพาะตัวที่ Linked (ไม่ใช่ตัวที่กำลังถูกเรียกกลับ)
        // ตัวที่อยู่ใกล้ปลายทางสุดไปก่อน ตัวที่โอนแล้วเดินไปหาเจ้านายใหม่เอง
        public int TransferTo(LinkSource to, int count)
        {
            if (to == null || to == this || count <= 0) return 0;
            if (!active || !to.CanAccept || !LinkRegistry.CanReach(this, to)) return 0;

            int n = Mathf.Min(count, to.Space);
            if (n <= 0) return 0;

            scratch.Clear();
            for (int i = 0; i < followers.Count; i++)
                if (followers[i].State == FollowerState.Linked) scratch.Add(followers[i]);

            Vector2 dest = to.Position;
            scratch.Sort((a, b) => (a.Position - dest).sqrMagnitude.CompareTo((b.Position - dest).sqrMagnitude));

            int moved = 0;
            for (int i = 0; i < scratch.Count && moved < n; i++)
                if (to.TryAttach(scratch[i])) moved++;

            scratch.Clear();
            return moved;
        }

        public int TransferAllTo(LinkSource to) => TransferTo(to, int.MaxValue);

        // ขอกำลังเสริมจากผู้สั่งคนอื่น คืนจำนวนที่ได้จริง
        public int RequestFollowers(int count) => LinkRegistry.Request(this, count);
        public int RequestMissing() => LinkRegistry.Request(this, Missing);

        // ---------- Debug ----------

        void OnDrawGizmosSelected()
        {
            if (profile == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, profile.connectRadius);                         // ระยะเชื่อม
            Gizmos.color = new Color(1f, 0.9f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, profile.commandRadius * profile.recallRatio);   // เริ่มเรียกกลับ
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, profile.commandRadius);                         // ระยะสั่งการ
        }
    }
}