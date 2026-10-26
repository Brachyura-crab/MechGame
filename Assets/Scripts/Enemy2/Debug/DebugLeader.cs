using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    // แผงทดสอบ leader (ลบทิ้งได้เมื่อเสร็จ): ดูบทบาท/alert/ขนาดฝูง และบังคับบทบาทหรือ alert ด้วยปุ่ม แผงเรียงต่อกันมุมซ้ายล่าง
    [RequireComponent(typeof(LeaderBrain))]
    public class DebugLeader : MonoBehaviour
    {
        static readonly List<DebugLeader> all = new List<DebugLeader>();

        LeaderBrain brain;
        string message = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void Awake() => brain = GetComponent<LeaderBrain>();
        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        void OnGUI()
        {
            if (!DebugGate.Enabled) return;   // สวิตช์ debug ส่วนกลาง
            if (brain == null) return;

            int idx = Mathf.Max(0, all.IndexOf(this));
            GUILayout.BeginArea(new Rect(10 + idx * 250, Screen.height - 190, 240, 180), GUI.skin.box);

            CampCommander cmd = brain.Member != null && brain.Member.Camp != null
                ? brain.Member.Camp.GetComponent<CampCommander>() : null;

            GUILayout.Label(name + "  [" + brain.Role + "]" + (brain.Retreating ? " กำลังถอย" : ""));
            GUILayout.Label("camp: " + (brain.HasCamp ? "มี" : "ไม่มี") + "   alert: " + (cmd != null && cmd.Alerted ? "ใช่" : "ไม่"));
            GUILayout.Label("ฝูง " + brain.Source.Count + " / เป้า " + brain.Source.Desired);
            GUILayout.Label(message);

            if (cmd != null)
            {
                if (GUILayout.Button("บังคับเป็น Engage 10 วินาที")) { cmd.ForceEngager(brain.Member, 10f); message = "บังคับ Engage"; }
                if (GUILayout.Button("camp alert (ครั้งหนึ่ง)")) { cmd.Alert(transform.position); message = "alert"; }
            }
            else GUILayout.Label("(ไม่มี CampCommander บน gate)");

            GUILayout.EndArea();
        }
    }
}