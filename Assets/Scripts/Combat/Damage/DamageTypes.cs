using UnityEngine;

public enum DamageType
{
    Kinetic,
    // เพิ่มประเภทใหม่ตรงนี้ แล้วเพิ่ม case ใน DamageCalculator.ResistStat
}

public struct DamageInfo
{
    public float baseAmount;
    public DamageType type;
    public GameObject source;   // ผู้โจมตี (ว่างได้ เช่น ดาเมจจากฉาก)
    public bool noCrit;         // true = ห้ามคริ (เช่น ดาเมจต่อเนื่อง)
    public float critChance;    // คริติคอลของอาวุธ (บวกกับ CritChance ของผู้โจมตี)
}

public struct DamageResult
{
    public float amount;        // ดาเมจสุดท้ายหลังคำนวณ (ก่อนแบ่งให้โล่/HP)
    public bool isCrit;
    public DamageType type;
    public GameObject source;
    public float shieldDamage;   // ส่วนที่เกราะดูดไป
    public float hpDamage;       // ส่วนที่ทะลุถึง HP
}