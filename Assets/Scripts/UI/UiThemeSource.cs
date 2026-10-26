using System;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class UiThemeSource : MonoBehaviour
{
    public static UiThemeSource Instance { get; private set; }
    public static event Action Changed;

    [SerializeField] UiTheme theme;

    [Header("สีสถานะ (ใช้ร่วมกันทุกหุ่น ไม่เปลี่ยนตามธีม)")]
    [SerializeField] Color caution  = new Color32(0xFF, 0xE5, 0x4D, 255);   // MINR
    [SerializeField] Color alert    = new Color32(0xFF, 0x8C, 0x33, 255);   // MAJR
    [SerializeField] Color critical = new Color32(0xFF, 0x33, 0x33, 255);   // FAIL

    public UiTheme Theme => theme;

    public static UiThemeSource Current
    {
        get
        {
            if (Instance != null) return Instance;
            return Application.isPlaying ? null : FindAnyObjectByType<UiThemeSource>();   // ไว้พรีวิวใน Editor
        }
    }

    public static Color Role(ThemeRole r)
    {
        UiThemeSource s = Current;
        return s != null && s.theme != null ? s.theme.Get(r) : Color.magenta;   // ม่วงแดง = ยังไม่ได้ตั้งธีม
    }

    public static Color Caution  => Current != null ? Current.caution  : Color.yellow;
    public static Color Alert    => Current != null ? Current.alert    : Color.yellow;
    public static Color Critical => Current != null ? Current.critical : Color.red;

    public static Color LevelColor(float ratio)
    {
        if (ratio < 0.2f) return Role(ThemeRole.Danger);
        if (ratio < 0.4f) return Role(ThemeRole.Warn);
        return Role(ThemeRole.InkBright);
    }

    public static Color TierColor(ModuleDamage t)
    {
        switch (t)
        {
            case ModuleDamage.Light:  return Caution;
            case ModuleDamage.Medium: return Alert;
            case ModuleDamage.Heavy:  return Critical;
            default:                  return Role(ThemeRole.InkMid);
        }
    }

    public static void RaiseChanged() => Changed?.Invoke();

    // เรียกตอนเลือกหุ่น: UiThemeSource.Instance.SetTheme(mechData.uiTheme);
    public void SetTheme(UiTheme t)
    {
        theme = t;
        Changed?.Invoke();
    }

    void OnEnable()
    {
        Instance = this;
        Changed?.Invoke();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    void OnValidate() => Changed?.Invoke();
}