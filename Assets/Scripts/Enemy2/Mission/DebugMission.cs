using UnityEngine;

namespace EnemyV2
{
    // แผงทดสอบภารกิจ (ลบทิ้งได้เมื่อเสร็จ): ดูเฟส/จำนวน gate/ระยะ และปุ่มปิด gate / วาร์ปผู้เล่น แผงอยู่มุมขวาล่าง
    [RequireComponent(typeof(MissionManager))]
    public class DebugMission : MonoBehaviour
    {
        MissionManager mm;
        string message = "";

        void Awake() => mm = GetComponent<MissionManager>();

        void OnGUI()
        {
            if (!DebugGate.Visible) return;   // สวิตช์ debug ส่วนกลาง (F1)
            if (mm == null) return;

            GUILayout.BeginArea(new Rect(Screen.width - 250, Screen.height - 330, 240, 320), GUI.skin.box);

            float d = EnemyTarget.Ensure() ? Vector2.Distance(EnemyTarget.Transform.position, mm.Origin) : -1f;

            GUILayout.Label("ภารกิจ: " + mm.Phase + "   seed " + mm.Seed);
            GUILayout.Label("gate ปิด " + mm.ClosedCount + " / " + mm.Total);
            GUILayout.Label("ห่างจุดส่ง " + d.ToString("0") + "  (เตือนที่ " + mm.WarnRadius.ToString("0") + ")");
            if (mm.OutOfArea) GUILayout.Label("ออกนอกพื้นที่! เหลือ " + mm.AbandonRemaining.ToString("0.0") + " วินาที");
            if (mm.Phase == MissionPhase.Extract) GUILayout.Label("จุดรับ " + (mm.ExtractProgress * 100f).ToString("0") + "%");
            GUILayout.Label(message);

            if (GUILayout.Button("ปิด gate ที่เหลือทั้งหมด")) CloseAll();
            if (GUILayout.Button("วาร์ปไปจุดส่ง/จุดรับ")) Teleport(mm.Origin);
            if (GUILayout.Button("วาร์ปออกนอกพื้นที่")) Teleport(mm.Origin + Vector2.right * (mm.WarnRadius + 20f));
            if (GUILayout.Button("วาร์ปไปใกล้ gate ที่ยังไม่ปิด")) TeleportNearOpenGate();

            GUILayout.EndArea();
        }

        // หักพลังงานจนหมด: gate เริ่มชาร์จแล้วระเบิดตามปกติ (ผู้เล่นอยู่ใกล้จะโดนดาเมจ)
        void CloseAll()
        {
            int n = 0;
            for (int i = Gate.All.Count - 1; i >= 0; i--)
            {
                Gate g = Gate.All[i];
                if (g != null && g.State == GateState.Active) { g.Drain(g.MaxEnergy); n++; }
            }
            message = "หักพลังงาน " + n + " gate";
        }

        void TeleportNearOpenGate()
        {
            for (int i = 0; i < mm.GateInfos.Count; i++)
            {
                if (!mm.GateInfos[i].closed)
                {
                    Teleport(mm.GateInfos[i].position + Vector2.left * 60f);
                    message = "วาร์ปไปใกล้ gate " + i;
                    return;
                }
            }
            message = "ไม่มี gate ที่ยังเปิด";
        }

        void Teleport(Vector2 p)
        {
            if (!EnemyTarget.Ensure()) return;

            Transform t = EnemyTarget.Transform;
            if (t.TryGetComponent(out Rigidbody2D rb)) { rb.position = p; rb.linearVelocity = Vector2.zero; }
            else t.position = new Vector3(p.x, p.y, t.position.z);
        }
    }
}