using UnityEngine;

public class MechAim : MonoBehaviour
{
    [SerializeField] float muzzleDistance = 0.8f; // ระยะจุดปล่อยกระสุนจากกลางตัว
    [SerializeField] float aimHoldAfterShot = 0.35f;   // ยังนับว่าเล็งอยู่อีกกี่วินาทีหลังยิงนัดล่าสุด

    Vector2 lastDir = Vector2.down;

    public Vector2 AimPoint { get; private set; }
    public float LastShotTime { get; private set; } = -999f;
    public bool FireHeld { get; private set; }        // กำลังกดปุ่มยิงปุ่มใดปุ่มหนึ่ง (PlayerAimInput เป็นคนตั้ง)
    public bool IsAiming => FireHeld || Time.time - LastShotTime < aimHoldAfterShot;
    public void SetFireHeld(bool held) => FireHeld = held;
    public void NotifyShot() => LastShotTime = Time.time;

    // ทิศเล็งจากตัวหุ่นไปยังจุดเล็ง (ถ้าจุดเล็งทับตัวหุ่น คงทิศเดิม)
    public Vector2 AimDirection
    {
        get
        {
            Vector2 d = AimPoint - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.01f) lastDir = d.normalized;
            return lastDir;
        }
    }

    public Vector2 MuzzlePosition => (Vector2)transform.position + AimDirection * muzzleDistance;

    public void SetAimPoint(Vector2 worldPoint) => AimPoint = worldPoint;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, MuzzlePosition);
        Gizmos.DrawWireSphere(MuzzlePosition, 0.1f);
    }
}