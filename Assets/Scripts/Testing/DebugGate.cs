using UnityEngine;

// จุดเดียวที่ตัดสินว่า debug (ปุ่มทดสอบ, ข้อความบนจอ) เปิดได้ไหม
// Enabled: เปิดใน Editor และ Development Build เท่านั้น build ปกติที่ส่งเพื่อนจะปิดหมด
// Visible: สวิตช์ซ่อน/แสดงข้อความ debug ตอนเล่น (กด F1) ปิดเสมอเมื่อ Enabled เป็น false
public static class DebugGate
{
    public static bool Enabled => Application.isEditor || Debug.isDebugBuild;

    static bool visible = true;
    public static bool Visible
    {
        get => Enabled && visible;
        set => visible = value;
    }
}