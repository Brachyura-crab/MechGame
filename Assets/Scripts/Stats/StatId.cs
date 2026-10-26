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
    
    // สกิล
    CooldownRate,
    AimAssist,   // > 0 = เปิดใช้ auto aim (ฐานเป็น 0 ไม่ต้องเพิ่มใน MechData)
    HPRegen,     // HP ที่ฟื้นต่อวินาที (ฐานเป็น 0 = ไม่ฟื้น ศัตรูที่ไม่ตั้งค่าจึงไม่ฟื้น)

    // เพิ่ม stat ใหม่ "เหนือบรรทัดนี้ ต่อท้าย HPRegen" เท่านั้น ห้ามแทรกกลาง (asset เก็บเป็นตัวเลข)
    Count // ต้องอยู่ท้ายสุดเสมอ ...
}