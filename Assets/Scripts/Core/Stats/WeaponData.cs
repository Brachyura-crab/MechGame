using UnityEngine;

public enum FireMode { HoldAuto, ReleaseFire }

[CreateAssetMenu(menuName = "Mech/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string id;
    public string displayName;

    [Header("Fire")]
    public float damage = 10f;
    public float critChance = 0.05f;
    public float shotsPerSecond = 3f;
    public float bulletSpeed = 40f;
    public float bulletLifetime = 0.8f;
    public float spreadDegrees = 0f;
    public DamageType damageType = DamageType.Kinetic;
    public FireMode fireMode = FireMode.HoldAuto;
    public int maxHits = 1;   // จำนวนศัตรูที่กระสุนผ่านได้ (-1 = ไม่จำกัด)

    [Header("Ammo")]
    public int magazineSize = 20;
    public float reloadTime = 2f;
    public float autoReloadIdleDelay = 1f;   // หยุดยิงกี่วินาทีถึงเริ่ม reload อัตโนมัติ

    [Header("Bullet")]
    public Bullet bulletPrefab;
}