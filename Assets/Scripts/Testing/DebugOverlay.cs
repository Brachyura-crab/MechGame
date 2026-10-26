using UnityEngine;
using UnityEngine.InputSystem;

// ตัวจัดเรียงข้อความ debug บนจอ: ทุกตัวเรียก DebugOverlay.Label(...) แล้วบรรทัดจะเรียงต่อกันเอง
// กด F1 เพื่อซ่อน/แสดง (มีเฉพาะ Editor และ Development Build)
[DefaultExecutionOrder(-32000)]
public class DebugOverlay : MonoBehaviour
{
    public static Vector2 Origin = new Vector2(20f, 20f);
    const float LineH = 22f;

    static DebugOverlay inst;
    static float y;
    static int lastFrame = -1;
    static EventType lastType;
    static float fps;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (!DebugGate.Enabled || inst != null) return;
        var go = new GameObject("DebugOverlay");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugOverlay>();
    }

    void Awake() => inst = this;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            DebugGate.Visible = !DebugGate.Visible;

        float dt = Time.unscaledDeltaTime;
        if (dt > 0f) fps = Mathf.Lerp(fps, 1f / dt, 0.05f);

        // ปุ่มทดสอบ: F2 = สลับ VSync, F3 = วนเพดาน FPS (ใช้ได้เมื่อ VSync ปิด)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.f2Key.wasPressedThisFrame) GameSettings.SetVSync(!GameSettings.VSync);
            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                var o = GameSettings.FpsOptions;
                int i = System.Array.IndexOf(o, GameSettings.FpsLimit);
                GameSettings.SetFpsLimit(o[(i + 1) % o.Length]);
            }
        }
    }

    void OnGUI()
    {
        y = Origin.y;   // เริ่มบรรทัดแรกใหม่ทุกครั้งที่วาด
        Label("FPS " + Mathf.RoundToInt(fps) + "   VSync " + (GameSettings.VSync ? "ON" : "OFF")
              + "   Limit " + GameSettings.FpsLabel(GameSettings.FpsLimit) + "   [F2 vsync / F3 limit]");
    }
    public static void Label(string text)
    {
        if (!DebugGate.Visible) return;

        // กันพลาดกรณีลำดับ OnGUI ไม่เรียงตามที่คาด
        var e = Event.current;
        if (e != null && (Time.frameCount != lastFrame || e.type != lastType))
        {
            lastFrame = Time.frameCount; lastType = e.type; y = Origin.y;
        }

        GUI.Label(new Rect(Origin.x, y, 760f, LineH), text);
        y += LineH;
    }
}