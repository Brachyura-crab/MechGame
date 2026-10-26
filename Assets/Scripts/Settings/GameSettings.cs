using System;
using UnityEngine;

// ค่าตั้งของเกม (จอ, FPS, เอฟเฟกต์) เก็บใน PlayerPrefs และนำไปใช้ตอนเปิดเกมอัตโนมัติ
// เสียงอยู่ที่ AudioPrefs (Master/Music/Sfx)
public static class GameSettings
{
    // 0 = ไม่จำกัด
    public static readonly int[] FpsOptions = { 30, 60, 120, 144, 240, 0 };

    const string KVSync = "set_vsync", KFps = "set_fps", KFull = "set_full", KCrt = "opt_crt";

    static bool vsync = true;
    static int fpsLimit = 144;
    static bool crt = true;
    static bool loaded;

    public static event Action Changed;

    public static bool VSync => Ensure(vsync);
    public static int FpsLimit => Ensure(fpsLimit);
    public static bool CrtEffect => Ensure(crt);
    public static bool Fullscreen => Screen.fullScreen;

    static T Ensure<T>(T v) { Load(); return v; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => loaded = false;

    // ใช้ค่าที่เซฟไว้ทันทีตอนเปิดเกม (ก่อน scene แรกโหลด)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        Load();
        ApplyFrameRate();
        CrtScreenFx.GlobalEnabled = crt;

        // ใน Editor ไม่ยุ่งกับโหมดจอ; ใน build ใช้ค่าที่เซฟไว้ (ถ้ามี)
        if (!Application.isEditor && PlayerPrefs.HasKey(KFull))
            FullscreenToggle.Request(PlayerPrefs.GetInt(KFull) == 1);
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        vsync = PlayerPrefs.GetInt(KVSync, 1) == 1;
        fpsLimit = PlayerPrefs.GetInt(KFps, 144);
        crt = PlayerPrefs.GetInt(KCrt, 1) == 1;
    }

    // VSync เปิด: ให้จอคุมจังหวะ (targetFrameRate ไม่มีผล) / ปิด: ใช้เพดาน FPS ที่เลือก
    static void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = vsync ? 1 : 0;
        Application.targetFrameRate = vsync ? -1 : (fpsLimit > 0 ? fpsLimit : -1);
    }

    // ---------- ตั้งค่า (มีผลทันที ออกจากหน้า Settings แล้วเรียก Save) ----------
    public static void SetVSync(bool on)
    {
        Load();
        vsync = on;
        PlayerPrefs.SetInt(KVSync, on ? 1 : 0);
        ApplyFrameRate();
        Changed?.Invoke();
    }

    public static void SetFpsLimit(int limit)
    {
        Load();
        fpsLimit = limit;
        PlayerPrefs.SetInt(KFps, limit);
        ApplyFrameRate();
        Changed?.Invoke();
    }

    public static void SetCrtEffect(bool on)
    {
        Load();
        crt = on;
        PlayerPrefs.SetInt(KCrt, on ? 1 : 0);
        CrtScreenFx.GlobalEnabled = on;
        Changed?.Invoke();
    }

    public static void SetFullscreen(bool on)
    {
        RememberFullscreen(on);
        FullscreenToggle.Request(on);
    }

    // FullscreenToggle (F11) เรียกเพื่อบันทึกโหมดล่าสุด
    public static void RememberFullscreen(bool on)
    {
        PlayerPrefs.SetInt(KFull, on ? 1 : 0);
        Changed?.Invoke();
    }

    public static void Save() => PlayerPrefs.Save();

    public static string FpsLabel(int limit) => limit <= 0 ? "UNLIMITED" : limit.ToString();
}