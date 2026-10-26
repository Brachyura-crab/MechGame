using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TMPro;

// เมนู Pause (Esc): RESUME / SETTINGS / RESTART / ABANDON / MAIN MENU / QUIT
public class PauseMenu : MenuScreen
{
    public static bool IsPaused { get; private set; }
    public static bool InputBlocked => IsPaused || ResultScreen.Showing;

    [Header("Scenes (ต้องอยู่ใน Build Settings)")]
    [SerializeField] string missionScene = "MissionSelect";
    [SerializeField] string menuScene = "MainMenu";

    [Header("ปุ่ม")]
    [SerializeField] Vector2 buttonsPos = new Vector2(80f, 230f);
    [SerializeField] Vector2 buttonSize = new Vector2(340f, 54f);
    [SerializeField] float buttonGap = 12f;

    Canvas canvas;
    SettingsPanel settings;
    bool settingsOpen;

    protected override void OnBuild()
    {
        EnsureEventSystem();
        canvas = stage.GetComponentInParent<Canvas>();
        if (canvas != null) { canvas.sortingOrder = 1000; canvas.enabled = false; }
        IsPaused = false;
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (RunManager.Instance == null) return;   // ไม่ใช่ scene ในด่าน (เมนู/เลือกยูนิต/เลือกด่าน) ไม่ต้องเปิด Pause
        if (ResultScreen.Showing) return;

        if (!IsPaused) Open();
        else if (settingsOpen) settings.Close();
        else Resume();
    }

    void Open()
    {
        IsPaused = true;
        settingsOpen = false;
        Time.timeScale = 0f;
        AudioManager.SetPaused(true);
        if (canvas != null) canvas.enabled = true;
        Rebuild();
    }

    void Resume()
    {
        IsPaused = false;
        settingsOpen = false;
        Time.timeScale = 1f;
        AudioManager.SetPaused(false);
        if (canvas != null) canvas.enabled = false;
    }

    void CloseSettings()
    {
        settingsOpen = false;
        Rebuild();
    }

    void Rebuild()
    {
        ResetDyn();

        var dim = B(dyn, 0, 0, W, H, ThemeRole.Background, 0.75f);
        dim.raycastTarget = true;

        B(dyn, buttonsPos.x, 112, 10, 10, ThemeRole.InkBright, 1f);
        L(dyn, "PAUSED", buttonsPos.x + 20, 100, 400, 40, 28, ThemeRole.InkBright, true);
        L(dyn, "// MISSION SUSPENDED", buttonsPos.x + 4, 150, 400, 24, 13, ThemeRole.InkLow);

        string[] labels = { "RESUME", "SETTINGS", "RESTART MISSION", "ABANDON MISSION", "MAIN MENU", "QUIT" };
        System.Action[] acts = { Resume, OpenSettings, Restart, Abandon, ToMenu, Quit };
        for (int i = 0; i < labels.Length; i++)
            PauseButton(dyn, buttonsPos.x, buttonsPos.y + i * (buttonSize.y + buttonGap),
                        buttonSize.x, buttonSize.y, labels[i], acts[i]);

        if (settingsOpen)
        {
            settings = new SettingsPanel(kit, dyn, CloseSettings, false);
            settings.Build();
        }
    }

    void OpenSettings()
    {
        settingsOpen = true;
        Rebuild();
    }

    void PauseButton(Transform parent, float x, float y, float w, float h, string label, System.Action act)
    {
        var frame = Card(parent, x, y, w, h, ThemeRole.InkMid, 0.8f, ThemeRole.Surface, 0.5f);
        var host = frame.rectTransform;

        var tri = Tri(host, 18, h / 2 - 7, 9, 14, ThemeRole.InkBright, 1f, false);
        tri.gameObject.SetActive(false);
        L(host, label, 40, 0, w - 50, h, 18, ThemeRole.InkMid, true);

        Hit(host, () => act());
        var hit = host.GetChild(host.childCount - 1);
        var relay = hit.gameObject.AddComponent<HoverRelay>();
        relay.onHover = on =>
        {
            if (on) GameAudio.UiHover();
            tri.gameObject.SetActive(on);
            frame.SetStyle(on ? ThemeRole.InkBright : ThemeRole.InkMid, on ? 1f : 0.8f,
                           on ? ThemeRole.InkMid : ThemeRole.Surface, on ? 0.35f : 0.5f);
        };
    }

    // ---------- การกระทำ ----------
    void Leave()
    {
        settingsOpen = false;
        if (canvas != null) canvas.enabled = false;
        IsPaused = false;
        Time.timeScale = 1f;
        AudioManager.SetPaused(false);
    }

    void Restart()
    {
        Leave();
        if (RunManager.Instance != null) RunManager.Instance.Retry();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Abandon() { Debug.Log("[Pause] Abandon clicked"); Leave(); LoadScene(missionScene); }
    void ToMenu() { Leave(); LoadScene(menuScene); }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        if (IsPaused) { IsPaused = false; Time.timeScale = 1f; }
    }
}