using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    // ตัวทดสอบแกน link (ลบทิ้งได้เมื่อเสร็จ): ติดคู่กับ LinkSource เพื่อสั่งงาน/โอน/ขอ/ฆ่า ผ่านปุ่มบนจอ
    // ใช้ OnGUI จึงทำงานได้ไม่ว่าโปรเจกต์ใช้ Input ระบบเก่าหรือใหม่  แผงของแต่ละตัวเรียงต่อกันทางขวา
    [RequireComponent(typeof(LinkSource))]
    public class DebugLinkSource : MonoBehaviour
    {
        [Header("ตัวช่วยทดสอบ")]
        [SerializeField] GameObject followerPrefab;   // prefab swarm ทดสอบ (มี Follower + DebugFollowerMover)
        [SerializeField] Transform attackTarget;      // เป้าโจมตี / ภัยของ Block (ลาก object อะไรก็ได้มาใส่)
        [SerializeField] LinkSource otherSource;      // ผู้สั่งอีกคน ไว้ทดสอบโอน/ช่วย
        [SerializeField] float guardRadius = 3f;
        [SerializeField] int spawnCount = 5;
        [SerializeField] float spawnRingRadius = 4f;  // ในระยะเชื่อม = ถูกเชื่อมเอง / ไกลกว่าระยะเชื่อม = ไม่มีเจ้านาย
        [SerializeField] bool drawLinks = true;       // วาดเส้นจากผู้สั่งไปหาผู้ตาม (สีตามสถานะ)

        static readonly List<DebugLinkSource> all = new List<DebugLinkSource>();

        LinkSource source;
        string message = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void Awake() => source = GetComponent<LinkSource>();
        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        void Say(string s)
        {
            message = s;
            Debug.Log("[" + name + "] " + s);
        }

        // ---------- แผงปุ่ม ----------

        void OnGUI()
        {
            if (!DebugGate.Enabled) return;   // สวิตช์ debug ส่วนกลาง
            if (source == null || source.Profile == null) return;

            int idx = Mathf.Max(0, all.IndexOf(this));
            GUILayout.BeginArea(new Rect(10 + idx * 250, 10, 240, 500), GUI.skin.box);

            GUILayout.Label(name + (source.IsActive ? "  [ACTIVE]" : "  [INACTIVE]"));
            GUILayout.Label("ผู้ตาม " + source.Count + " / เป้า " + source.Desired + " / max " + source.Profile.maxFollowers);
            GUILayout.Label(StateSummary());
            GUILayout.Label(message);

            GUILayout.Label("— คำสั่ง —");
            if (GUILayout.Button("Hold ทั้งหมด")) DoIssue(LinkOrder.Hold());
            if (GUILayout.Button("Guard ที่ตัวผู้สั่ง")) DoIssue(LinkOrder.Guard(transform.position, guardRadius));
            if (GUILayout.Button("Attack เป้า")) { if (attackTarget != null) DoIssue(LinkOrder.Attack(attackTarget)); else Say("ยังไม่ได้ใส่ Attack Target"); }
            if (GUILayout.Button("Block (ปกป้องผู้สั่ง จากเป้า)")) { if (attackTarget != null) DoIssue(LinkOrder.Block(transform, attackTarget)); else Say("ยังไม่ได้ใส่ Attack Target"); }
            if (GUILayout.Button("Assist ผู้สั่งอีกคน")) { if (otherSource != null) DoIssue(LinkOrder.Assist(otherSource.transform)); else Say("ยังไม่ได้ใส่ Other Source"); }
            if (GUILayout.Button("Retreat มาที่ตัวผู้สั่ง")) DoIssue(LinkOrder.Retreat(transform.position));

            GUILayout.Label("— โอน / ขอ —");
            if (GUILayout.Button("โอนครึ่งหนึ่งให้อีกคน")) DoTransfer(source.Count / 2);
            if (GUILayout.Button("โอนทั้งหมดให้อีกคน")) DoTransfer(int.MaxValue);
            if (GUILayout.Button("ขอกำลังเสริมจนถึงเป้า")) Say("ขอ " + source.Missing + " ได้จริง " + source.RequestMissing());

            GUILayout.Label("— เสก / ลบ —");
            if (GUILayout.Button("เสก " + spawnCount + " ตัว ผูกกับตัวนี้เลย")) Spawn(true);
            if (GUILayout.Button("เสก " + spawnCount + " ตัว ไม่มีเจ้านาย")) Spawn(false);
            if (GUILayout.Button("ลบผู้ตาม 3 ตัว (จำลองตาย)")) KillFollowers(3);

            GUILayout.Label("— ผู้สั่ง —");
            if (GUILayout.Button("Deactivate (leader ตาย / gate ปิด)")) { source.Deactivate(); Say("Deactivate"); }
            if (GUILayout.Button("Reset (เปิดใหม่)")) { source.enabled = false; source.enabled = true; Say("Reset"); }

            GUILayout.EndArea();
        }

        string StateSummary()
        {
            int dormant = 0, waking = 0, linked = 0, recalling = 0;
            IReadOnlyList<Follower> list = source.Followers;
            for (int i = 0; i < list.Count; i++)
            {
                switch (list[i].State)
                {
                    case FollowerState.Dormant: dormant++; break;
                    case FollowerState.Waking: waking++; break;
                    case FollowerState.Linked: linked++; break;
                    case FollowerState.Recalling: recalling++; break;
                }
            }
            return "เขียว " + linked + "  เหลือง " + waking + "  แดง " + recalling + "  เทา " + dormant;
        }

        // ---------- การกระทำ ----------

        void DoIssue(LinkOrder order)
        {
            int n = source.Issue(order);
            Say(order + " → รับคำสั่ง " + n + " จาก " + source.Count + " ตัว");
        }

        void DoTransfer(int count)
        {
            if (otherSource == null) { Say("ยังไม่ได้ใส่ Other Source"); return; }
            int n = source.TransferTo(otherSource, count);
            Say("โอนให้ " + otherSource.name + " ได้ " + n + " ตัว");
        }

        void Spawn(bool linked)
        {
            if (followerPrefab == null) { Say("ยังไม่ได้ใส่ Follower Prefab"); return; }

            int attached = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                GameObject go = Instantiate(followerPrefab, RingPos(spawnRingRadius), Quaternion.identity);
                Follower f = go.GetComponent<Follower>();
                CampMember cm = go.GetComponent<CampMember>();
                if (cm != null) cm.Assign(GetComponent<Gate>());
                if (linked && f != null && f.SpawnLinkedTo(source)) attached++;
            }
            Say(linked ? "เสก " + spawnCount + " ตัว ผูกได้ " + attached : "เสก " + spawnCount + " ตัว ไม่มีเจ้านาย");
        }

        void KillFollowers(int count)
        {
            IReadOnlyList<Follower> list = source.Followers;
            int n = Mathf.Min(count, list.Count);
            for (int i = 0; i < n; i++) Destroy(list[i].gameObject);   // ลบจริงตอนจบเฟรม ลิสต์ไม่เปลี่ยนระหว่างวน
            Say("ลบ " + n + " ตัว");
        }

        Vector3 RingPos(float radius)
        {
            Vector2 p = (Vector2)transform.position + Random.insideUnitCircle.normalized * radius;
            return new Vector3(p.x, p.y, transform.position.z);
        }

        // ---------- Gizmo ----------

        void OnDrawGizmos()
        {
            if (!drawLinks || source == null) return;

            IReadOnlyList<Follower> list = source.Followers;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                Gizmos.color = DebugFollowerMover.StateColor(list[i].State);
                Gizmos.DrawLine(transform.position, list[i].transform.position);
            }
        }
    }
}