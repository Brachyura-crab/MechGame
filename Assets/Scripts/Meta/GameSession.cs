using UnityEngine;

/// ค่าที่ต้องข้ามฉาก (เมนู -> เลือกหุ่น -> เลือกด่าน -> เล่น -> ผลลัพธ์)
public static class GameSession
{
    public static MechEntry SelectedMech;
    public static MissionNode SelectedMission;

    // ผลรอบล่าสุด (ใช้โชว์หน้าสรุป)
    public static bool LastWon;
    public static int LastScore;
    public static int LastPointsEarned;

    public static void Clear()
    {
        SelectedMech = null;
        SelectedMission = null;
        LastWon = false;
        LastScore = 0;
        LastPointsEarned = 0;
    }
}