using UnityEngine;

[CreateAssetMenu(menuName = "Mech/MechData")]
public class MechData : ScriptableObject
{
    [Header("Info")]
    public string id;            // ID คงที่ ห้ามเปลี่ยนหลังตั้งแล้ว (ใช้ตอนเซฟในอนาคต)
    public string displayName;

    [Header("Movement")]
    public float maxSpeed = 6f;
    public float acceleration = 40f;
    public float deceleration = 50f;
    public float dashSpeed = 18f;
    public float dashTime = 0.15f;
    public float dashCooldown = 1f;
    public float boostSpeed = 14f;
    public float boostTurnRate = 100f;

    [Header("Energy")]
    public float maxEnergy = 100f;
    public float energyRegen = 10f;
    public float dashEnergyCost = 20f;
    public float boostEnergyPerSec = 15f;

    [Header("Survival")]
    public float maxHP = 100f;
        public float maxShield = 50f;
    public float shieldRegen = 5f;

    [Header("Combat")]
    public float damageMultiplier = 1f;
    public float critChance = 0f;
    public float critDamage = 1.5f;
    public float resistKinetic = 0f;   // 0.3 = ลดดาเมจ 30%
    public float resistCap = 0.8f;
    public float fireRate = 1f;      // ตัวคูณความเร็วยิง
    public float reloadSpeed = 1f;   // ตัวคูณความเร็ว reload

    public void ApplyTo(StatBlock s)
    {
        s.SetBase(StatId.MaxSpeed, maxSpeed);
        s.SetBase(StatId.Acceleration, acceleration);
        s.SetBase(StatId.Deceleration, deceleration);
        s.SetBase(StatId.DashSpeed, dashSpeed);
        s.SetBase(StatId.DashTime, dashTime);
        s.SetBase(StatId.DashCooldown, dashCooldown);
        s.SetBase(StatId.BoostSpeed, boostSpeed);
        s.SetBase(StatId.BoostTurnRate, boostTurnRate);
        s.SetBase(StatId.MaxEnergy, maxEnergy);
        s.SetBase(StatId.EnergyRegen, energyRegen);
        s.SetBase(StatId.DashEnergyCost, dashEnergyCost);
        s.SetBase(StatId.BoostEnergyPerSec, boostEnergyPerSec);
        s.SetBase(StatId.MaxHP, maxHP);
        s.SetBase(StatId.MaxShield, maxShield);
        s.SetBase(StatId.ShieldRegen, shieldRegen);
        s.SetBase(StatId.DamageMultiplier, damageMultiplier);
        s.SetBase(StatId.CritChance, critChance);
        s.SetBase(StatId.CritDamage, critDamage);
        s.SetBase(StatId.ResistKinetic, resistKinetic);
        s.SetBase(StatId.ResistCap, resistCap);
        s.SetBase(StatId.FireRate, fireRate);
        s.SetBase(StatId.ReloadSpeed, reloadSpeed);
    }
}