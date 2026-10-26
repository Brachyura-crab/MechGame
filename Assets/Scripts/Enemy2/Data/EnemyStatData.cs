using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/Enemy Stat Data", fileName = "EnemyStat_")]
public class EnemyStatData : StatProfile
{
    [Header("Info")]
    public string displayName = "Swarmer";

    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 20f;
    public float deceleration = 6f;      // น้อย = ไถลไกลตอนชะลอ

    [Header("Survival")]
    public float maxHP = 20f;
    public float maxShield = 0f;
    public float shieldRegen = 0f;

    [Header("Combat")]
    public float damageMultiplier = 1f;
    public float critChance = 0f;
    public float critDamage = 1.5f;
    public float resistKinetic = 0f;     // 0.3 = ลดดาเมจ 30%
    public float resistCap = 0.8f;       // ห้ามเป็น 0 ไม่งั้น resist ถูกตัดเป็น 0
    public float fireRate = 1f;          // ตัวคูณความเร็วโจมตี (ขั้นถัดไปจะใช้)

    public override void ApplyTo(StatBlock s)
    {
        s.SetBase(StatId.MaxSpeed, maxSpeed);
        s.SetBase(StatId.Acceleration, acceleration);
        s.SetBase(StatId.Deceleration, deceleration);

        s.SetBase(StatId.MaxHP, maxHP);
        s.SetBase(StatId.MaxShield, maxShield);
        s.SetBase(StatId.ShieldRegen, shieldRegen);

        s.SetBase(StatId.DamageMultiplier, damageMultiplier);
        s.SetBase(StatId.CritChance, critChance);
        s.SetBase(StatId.CritDamage, critDamage);
        s.SetBase(StatId.ResistKinetic, resistKinetic);
        s.SetBase(StatId.ResistCap, resistCap);
        s.SetBase(StatId.FireRate, fireRate);
    }
}