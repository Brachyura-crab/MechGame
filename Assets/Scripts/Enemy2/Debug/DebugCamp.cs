using UnityEngine;

namespace EnemyV2
{
    // แผงทดสอบ camp (ลบทิ้งได้เมื่อเสร็จ): ดูจำนวน และปุ่มจัดทัพ/เสก/ลบ แผงอยู่ใต้แผง gate มุมขวาบน
    [RequireComponent(typeof(Camp))]
    public class DebugCamp : MonoBehaviour
    {
        Camp camp;
        string message = "";

        void Awake() => camp = GetComponent<Camp>();

        void OnGUI()
        {
            if (!DebugGate.Enabled) return;   // สวิตช์ debug ส่วนกลาง
            if (camp == null || camp.Data == null) return;

            CampData d = camp.Data;
            GUILayout.BeginArea(new Rect(Screen.width - 250, 220, 240, 260), GUI.skin.box);

            GUILayout.Label(name + (camp.Deployed ? "  [จัดทัพแล้ว]" : "  [ยังไม่จัดทัพ]"));
            GUILayout.Label("leader " + camp.LeaderCount + " / " + d.leaderTarget);
            GUILayout.Label("swarm (มีเจ้านาย) " + camp.SwarmCount + " / " + d.swarmTarget);
            GUILayout.Label("ตัวค้าง " + camp.OrphanCount + "   รวมสมาชิก " + camp.MemberCount + " / " + d.maxAlivePerGate);
            GUILayout.Label("การเกิด: " + (Camp.SpawningEnabled ? "เปิด" : "ปิด"));
            GUILayout.Label(message);

            if (GUILayout.Button("เปิด/ปิดการเกิด (ทุก camp)")) Camp.SpawningEnabled = !Camp.SpawningEnabled;
            if (GUILayout.Button("บังคับจัดทัพ")) { camp.ForceDeploy(); message = "สั่งจัดทัพ"; }
            if (GUILayout.Button("เสก leader 1 ตัว")) { camp.SpawnLeaderNow(); message = "เสก leader"; }
            if (GUILayout.Button("เสก swarm 5 ตัว (ผูก gate)")) message = "เสก swarm " + camp.SpawnSwarmNow(5) + " ตัว";
            if (GUILayout.Button("ลบศัตรูทั้งหมดของ camp")) { camp.ClearMembers(); message = "ลบแล้ว (ไม่หักพลังงาน)"; }

            GUILayout.EndArea();
        }
    }
}