using UnityEngine;

// เส้นเลเซอร์บอกทิศกระสุน แสดงเฉพาะตอนชาร์จ (ใช้กับอาวุธโหมด ReleaseFire)
public class WeaponAimLaser : MonoBehaviour
{
    [SerializeField] Weapon weapon;            // Plasma Magnum
    [SerializeField] LineRenderer line;
    [SerializeField] LayerMask hitMask;        // ค่าเดียวกับ Hit Mask ใน Bullet ของ plasma
    [SerializeField] float startDelay = 0.06f; // กดค้างเกินนี้ถึงเริ่มแสดง กันกะพริบตอนกดแตะ
    [SerializeField] float maxLength = 0f;     // 0 = ใช้ bulletSpeed x bulletLifetime
    [SerializeField] float minAlpha = 0.35f;   // ความโปร่งตอนเริ่มชาร์จ (เต็ม = 1)
    [Range(0f, 1f)]
    [SerializeField] float endFade = 0.15f;    // ความทึบปลายเส้นเทียบกับต้นเส้น

    ContactFilter2D filter;
    float heldTimer;

    static readonly RaycastHit2D[] buffer = new RaycastHit2D[16];

    void Awake()
    {

        filter = new ContactFilter2D();
        filter.SetLayerMask(hitMask);
        filter.useTriggers = false;

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.enabled = false;
    }

    void OnDisable()
    {
        if (line != null) line.enabled = false;
    }

    void LateUpdate()
    {
        bool charging = weapon != null && weapon.isActiveAndEnabled && weapon.IsHolding;
        heldTimer = charging ? heldTimer + Time.deltaTime : 0f;

        if (!charging || heldTimer < startDelay)
        {
            line.enabled = false;
            return;
        }

        WeaponData d = weapon.Data;
        weapon.GetShotRay(out Vector2 muzzle, out Vector2 dir);   // ทิศเดียวกับตอนยิงจริงเสมอ

        float range = maxLength > 0f ? maxLength : d.bulletSpeed * d.bulletLifetime;
        float len = range;

        // หยุดที่สิ่งกีดขวาง (collider ที่ไม่มี Health) ศัตรูทะลุผ่านเหมือนกระสุน
        int n = Physics2D.Raycast(muzzle, dir, filter, buffer, range);
        for (int i = 0; i < n; i++)
        {
            if (buffer[i].collider.GetComponentInParent<Health>() != null) continue;
            if (buffer[i].distance < len) len = buffer[i].distance;
        }

        line.SetPosition(0, muzzle);
        line.SetPosition(1, muzzle + dir * len);

        float a = Mathf.Lerp(minAlpha, 1f, weapon.HoldRatio);
        line.startColor = new Color(1f, 1f, 1f, a);
        line.endColor = new Color(1f, 1f, 1f, a * endFade);

        // ลำดับเดียวกับก้อนพลังงานปลายกระบอก แต่ใต้ลงหนึ่งขั้น
        line.sortingOrder = weapon.MuzzleSortingOrder - 1;   // ใต้ก้อนพลังงานปลายกระบอกหนึ่งขั้น

        line.enabled = true;
    }
}