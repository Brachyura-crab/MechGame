using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/ShotgunAttackData")]
public class ShotgunAttackData : ScriptableObject
{
    [Header("Bullet")]
    public Bullet bulletPrefab;   // Bullet_Enemy (Hit Mask = Player เท่านั้น)
    public BulletParams pellet = new BulletParams
    {
        damage = 4f,
        critChance = 0f,
        speed = 14f,
        lifetime = 1f,
        type = DamageType.Kinetic,
        maxHits = 1,
        impactLifetime = 0.6f,
        impactSortingOrder = 20
    };

    [Header("Volley")]
    public int pellets = 8;
    public float spreadDegrees = 24f;   // มุมกระจายรวมของพัด
    public float muzzleOffset = 1f;     // จุดออกห่างจากศูนย์กลางตัว

    [Header("Timing")]
    public float range = 11f;           // ผู้เล่นอยู่ในระยะนี้จึงเริ่มยิง (ควรไม่เกิน speed × lifetime)
    public float windupTime = 0.5f;
    [Range(0.2f, 1f)] public float aimLockRatio = 0.7f;   // ล็อกทิศเมื่อ windup ผ่านไปกี่ส่วน
    public float cooldown = 5f;

    [Header("Feel")]
    [Range(0f, 1f)] public float windupMoveScale = 0.2f;  // ชะลอการเคลื่อนที่ตอน windup
    public float recoilSpeed = 3f;      // ความเร็วที่ดีดถอยตอนยิง

    void OnValidate()
    {
        pellets = Mathf.Max(1, pellets);
        spreadDegrees = Mathf.Max(0f, spreadDegrees);
        windupTime = Mathf.Max(0.05f, windupTime);
        cooldown = Mathf.Max(0.1f, cooldown);
        range = Mathf.Max(1f, range);
    }
}