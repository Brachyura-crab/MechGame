using UnityEngine;

public static class DamageCalculator
{
    // ทุกอาวุธ/สกิล/สถานะต้องส่งดาเมจผ่านที่นี่ ห้ามเรียก Health.Damage ตรงๆ
    public static DamageResult Deal(DamageInfo info, MechStats attacker, Health target)
    {
        float amount = info.baseAmount;
        bool crit = false;

        if (attacker != null)
        {
            StatBlock a = attacker.Stats;
            amount *= a.Get(StatId.DamageMultiplier);

            if (!info.noCrit && Random.value < info.critChance + a.Get(StatId.CritChance))
            {
                crit = true;
                amount *= a.Get(StatId.CritDamage);
            }
        }

        StatBlock t = target.Stats;
        float resist = Mathf.Min(t.Get(ResistStat(info.type)), t.Get(StatId.ResistCap));
        amount *= 1f - Mathf.Max(0f, resist);

        var result = new DamageResult
        {
            amount = amount,
            isCrit = crit,
            type = info.type,
            source = info.source
        };
        target.TakeDamage(result);
        return result;
    }

    static StatId ResistStat(DamageType type)
    {
        switch (type)
        {
            case DamageType.Kinetic: return StatId.ResistKinetic;
            default: return StatId.ResistKinetic;
        }
    }
}