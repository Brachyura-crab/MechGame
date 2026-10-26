using UnityEngine;

public enum ThemeRole { Background, Surface, InkDim, InkLow, InkMid, InkBright, OnInk, Warn, Danger }

[CreateAssetMenu(menuName = "Mech/UiTheme")]
public class UiTheme : ScriptableObject
{
    public Color background = new Color32(0x02, 0x08, 0x05, 255);   // พื้นจอ
    public Color surface    = new Color32(0x0A, 0x1F, 0x12, 255);   // พื้นแถว/แผงย่อย
    public Color inkDim     = new Color32(0x0A, 0x3A, 0x1E, 255);   // เส้นกริด ลายพื้น
    public Color inkLow     = new Color32(0x1A, 0x8A, 0x4A, 255);   // เส้นแบ่ง สิ่งที่ปิดใช้งาน
    public Color inkMid     = new Color32(0x2F, 0xD4, 0x74, 255);   // ตัวหนังสือทั่วไป เส้นกรอบ
    public Color inkBright  = new Color32(0x6B, 0xFF, 0xA6, 255);   // ค่าที่ต้องดู
    public Color onInk      = new Color32(0x02, 0x08, 0x05, 255);   // ตัวอักษรบนพื้นสีสว่าง
    public Color warn       = new Color32(0xFF, 0xB0, 0x00, 255);   // เตือน (ค่าต่ำ, กำลังชาร์จ)
    public Color danger     = new Color32(0xFF, 0x4D, 0x3D, 255);   // วิกฤต (ใกล้หมด, ออกนอกพื้นที่)

    public Color Get(ThemeRole r)
    {
        switch (r)
        {
            case ThemeRole.Background: return background;
            case ThemeRole.Surface:    return surface;
            case ThemeRole.InkDim:     return inkDim;
            case ThemeRole.InkLow:     return inkLow;
            case ThemeRole.InkMid:     return inkMid;
            case ThemeRole.InkBright:  return inkBright;
            case ThemeRole.Warn:       return warn;
            case ThemeRole.Danger:     return danger;
            default:                   return onInk;
        }
    }

    void OnValidate() => UiThemeSource.RaiseChanged();   // แก้สีใน asset แล้ว UI เปลี่ยนตามทันที
}