public enum StatId
{
    // การเคลื่อนที่
    MaxSpeed,
    Acceleration,
    Deceleration,
    DashSpeed,
    DashTime,
    DashCooldown,
    BoostSpeed,
    BoostTurnRate,

    // พลังงาน
    MaxEnergy,
    EnergyRegen,
    DashEnergyCost,
    BoostEnergyPerSec,

    // ความอึด
    MaxHP,
    MaxShield,
    ShieldRegen,

    // การโจมตี
    DamageMultiplier,
    CritChance,
    CritDamage,

    // ความต้านทาน
    ResistKinetic,
    ResistCap,

    // อาวุธ
    FireRate,
    ReloadSpeed,

    Count // ต้องอยู่ท้ายสุดเสมอ ใช้นับจำนวนสถิติ ห้ามใช้เป็นสถิติจริง
}