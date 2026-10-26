using UnityEngine;

namespace EnemyV2
{
    // แผงทดสอบ gate (ลบทิ้งได้เมื่อเสร็จ): ดูสถานะ และหักพลังงานด้วยปุ่ม แผงอยู่มุมขวาบน
    [RequireComponent(typeof(Gate))]
    public class GateDebugPanel : MonoBehaviour
    {
        Gate gate;
        string message = "";

        void Awake() => gate = GetComponent<Gate>();

        void OnGUI()
        {
            if (!DebugGate.Enabled) return;   // สวิตช์ debug ส่วนกลาง
            if (gate == null || gate.Data == null) return;

            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 200), GUI.skin.box);

            GUILayout.Label(name + "  [" + gate.State + "]");
            GUILayout.Label("พลังงาน " + gate.Energy.ToString("0") + " / " + gate.MaxEnergy.ToString("0"));
            GUILayout.Label("ผู้เล่นอยู่ในระยะ: " + (gate.PlayerInRange ? "ใช่ (ฆ่าแล้วลด)" : "ไม่ (ฆ่าแล้วไม่ลด)"));
            if (gate.State == GateState.Charging) GUILayout.Label("ชาร์จ " + (gate.ChargeRatio * 100f).ToString("0") + "%");
            GUILayout.Label(message);

            if (GUILayout.Button("หักพลังงาน 10%")) Drain(0.1f);
            if (GUILayout.Button("หักพลังงาน 50%")) Drain(0.5f);
            if (GUILayout.Button("หักจนหมด (เริ่มชาร์จ)")) Drain(1f);

            GUILayout.EndArea();
        }

        void Drain(float fraction)
        {
            float before = gate.Energy;
            gate.Drain(gate.MaxEnergy * fraction);
            message = "พลังงาน " + before.ToString("0") + " → " + gate.Energy.ToString("0");
        }
    }
}