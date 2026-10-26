using UnityEngine;

// เก็บผู้เล่นไว้ที่เดียว ศัตรูทุกตัวเรียก Ensure() ก่อนใช้
public static class EnemyTarget
{
    public static Transform Transform { get; private set; }
    public static Health PlayerHealth { get; private set; }

    static int lastSearchFrame = -1;
    static bool warned;

    // ล้างค่าทุกครั้งที่เริ่ม Play (กรณีปิด Reload Domain ค่า static จะไม่ถูกล้างเอง)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Transform = null;
        PlayerHealth = null;
        lastSearchFrame = -1;
        warned = false;
    }

    public static bool Ensure()
    {
        // ต้องมีทั้ง Transform และ Health ที่ยังไม่ถูกทำลาย
        if (Transform != null && PlayerHealth != null) return true;

        if (Time.frameCount == lastSearchFrame) return Transform != null;   // ค้นหาไม่เกินหนึ่งครั้งต่อเฟรม
        lastSearchFrame = Time.frameCount;

        PlayerTag tag = PlayerTag.Instance;
        if (tag == null)
        {
            Transform = null;
            PlayerHealth = null;
            if (!warned) { warned = true; Debug.LogWarning("EnemyTarget: ไม่พบ PlayerTag บนผู้เล่น ศัตรูจะไม่รู้ว่าผู้เล่นอยู่ไหน"); }
            return false;
        }

        Transform = tag.transform;
        PlayerHealth = tag.Health;
        return Transform != null;
    }
}