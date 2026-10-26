using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// กด F11 สลับเต็มจอ <-> หน้าต่าง (ทำงานได้ทุกเวลา รวมถึงตอน pause / หน้าจบเกม)
// วางบน GameObject ใดก็ได้ในซีนแรก ไม่ต้องลากอะไร อยู่ข้ามซีนเองเมื่อ Retry
public class FullscreenToggle : MonoBehaviour
{
    [SerializeField] Vector2Int fallbackWindowSize = new Vector2Int(1280, 720);   // ขนาดหน้าต่างตอนออกจากเต็มจอครั้งแรก

    static FullscreenToggle instance;
    Vector2Int windowSize;

    void Awake()
    {
        if (instance != null) { Destroy(this); return; }   // กันซ้ำตอนโหลดซีนใหม่
        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        windowSize = fallbackWindowSize;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f11Key.wasPressedThisFrame) Toggle();
    }

    void Toggle() => Apply(!Screen.fullScreen);

    // เรียกจาก Settings: ตั้งโหมดจอตามที่ต้องการ (ถ้าตัวนี้ยังไม่ถูกสร้าง จะสลับตรงๆ)
    public static void Request(bool full)
    {
        if (instance != null) instance.Apply(full);
        else
        {
            var r = Screen.currentResolution;
            if (full) Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
    }

    void Apply(bool full)
    {
        if (full == Screen.fullScreen) { GameSettings.RememberFullscreen(full); return; }

        var lockState = Cursor.lockState;   // จำสถานะเมาส์ไว้ (ล็อก/ปล่อย) เพราะเปลี่ยนโหมดจอแล้วมักถูกรีเซ็ต

        if (!full)
        {
            Screen.SetResolution(windowSize.x, windowSize.y, FullScreenMode.Windowed);
        }
        else
        {
            windowSize = new Vector2Int(Screen.width, Screen.height);   // จำขนาดหน้าต่างก่อนเข้าเต็มจอ
            var r = Screen.currentResolution;
            Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
        }

        GameSettings.RememberFullscreen(full);
        StartCoroutine(RestoreCursor(lockState));
    }

    IEnumerator RestoreCursor(CursorLockMode mode)
    {
        yield return null;
        yield return null;   // รอให้ระบบสลับโหมดจอเสร็จก่อน
        Cursor.lockState = mode;
    }
}