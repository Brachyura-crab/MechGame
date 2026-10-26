using UnityEngine;

// ตัวช่วยกลางของ AI ศัตรู: ตัดสินว่าถูกควบคุม (stun/knockback) อยู่หรือไม่
public static class EnemyMotion
{
    // true  = AI สั่งความเร็วได้ตามปกติ
    // false = ถูกควบคุมอยู่ AI ห้ามเขียนความเร็ว
    //         knockback: ปล่อยให้ไถลตามแรงผลัก, stun/root: เบรกลงเรื่อยๆ ด้วย decel
    public static bool CanSteer(StatusController status, Rigidbody2D rb, float decel, float dt)
    {
        if (status == null) return true;
        if (status.IsKnockedBack) return false;
        if (!status.CanMove)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, decel * dt);
            return false;
        }
        return true;
    }

    public static bool CanAttack(StatusController status) => status == null || status.CanAttack;

    // เตือนใน Console (เฉพาะ Development/Editor) ถ้า prefab ลืมใส่ StatusController
    public static void WarnIfMissing(StatusController status, Component owner)
    {
        if (status == null && Debug.isDebugBuild)
            Debug.LogWarning($"{owner.name}: ไม่มี StatusController - stun/knockback/CC resist จะไม่ทำงานกับตัวนี้", owner);
    }
}