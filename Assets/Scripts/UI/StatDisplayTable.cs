using System;
using UnityEngine;

public enum StatFormat { Number, Percent, Multiplier, Seconds, PerSecond }

[CreateAssetMenu(menuName = "Mech/StatDisplayTable")]
public class StatDisplayTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public StatId stat;
        public string label;
        public string group;       // stat ที่เรียงติดกันและ group เดียวกันจะอยู่ใต้หัวข้อเดียว
        public StatFormat format;
        public bool visible = true;
        public bool lowerIsBetter;   // ติ๊กถ้าค่าน้อยลง = ดีขึ้น (เช่น ค่าใช้จ่าย, คูลดาวน์)
    }

    public Entry[] entries;

    public static string Format(float v, StatFormat f)
    {
        switch (f)
        {
            case StatFormat.Percent:    return (v * 100f).ToString("0.#") + "%";
            case StatFormat.Multiplier: return "x" + v.ToString("0.##");
            case StatFormat.Seconds:    return v.ToString("0.##") + "s";
            case StatFormat.PerSecond:  return v.ToString("0.#") + "/s";
            default:                    return v.ToString("0.#");
        }
    }

    public static string FormatDelta(float d, StatFormat f)
    {
        string sign = d >= 0f ? "+" : "-";
        float a = Mathf.Abs(d);
        switch (f)
        {
            case StatFormat.Percent:    return sign + (a * 100f).ToString("0.#") + "%";
            case StatFormat.Multiplier: return sign + a.ToString("0.##") + "x";
            case StatFormat.Seconds:    return sign + a.ToString("0.##") + "s";
            case StatFormat.PerSecond:  return sign + a.ToString("0.#") + "/s";
            default:                    return sign + a.ToString("0.#");
        }
    }

    static Entry E(StatId s, string label, string group, StatFormat f, bool low = false) =>
        new Entry { stat = s, label = label, group = group, format = f, visible = true, lowerIsBetter = low };

    [ContextMenu("Fill defaults")]
    void FillDefaults()
    {
        entries = new[]
        {
            E(StatId.MaxHP,             "MAX HP",        "DEFENSE",  StatFormat.Number),
            E(StatId.MaxShield,         "MAX SHIELD",    "DEFENSE",  StatFormat.Number),
            E(StatId.ShieldRegen,       "SHIELD REGEN",  "DEFENSE",  StatFormat.PerSecond),
            E(StatId.HPRegen,           "HP REGEN",      "DEFENSE",  StatFormat.PerSecond),
            E(StatId.ResistKinetic,     "KINETIC RESIST","DEFENSE",  StatFormat.Percent),
            E(StatId.ResistCap,         "RESIST CAP",    "DEFENSE",  StatFormat.Percent),

            E(StatId.MaxEnergy,         "MAX ENERGY",    "ENERGY",   StatFormat.Number),
            E(StatId.EnergyRegen,       "ENERGY REGEN",  "ENERGY",   StatFormat.PerSecond),
            E(StatId.DashEnergyCost,    "DASH COST",     "ENERGY",   StatFormat.Number, true),
            E(StatId.BoostEnergyPerSec, "BOOST COST",    "ENERGY",   StatFormat.PerSecond, true),

            E(StatId.MaxSpeed,          "MAX SPEED",     "MOBILITY", StatFormat.Number),
            E(StatId.Acceleration,      "ACCELERATION",  "MOBILITY", StatFormat.Number),
            E(StatId.Deceleration,      "DECELERATION",  "MOBILITY", StatFormat.Number),
            E(StatId.DashSpeed,         "DASH SPEED",    "MOBILITY", StatFormat.Number),
            E(StatId.DashTime,          "DASH TIME",     "MOBILITY", StatFormat.Seconds),
            E(StatId.DashCooldown,      "DASH COOLDOWN", "MOBILITY", StatFormat.Seconds, true),
            E(StatId.BoostSpeed,        "BOOST SPEED",   "MOBILITY", StatFormat.Number),
            E(StatId.BoostTurnRate,     "BOOST TURN",    "MOBILITY", StatFormat.Number),

            E(StatId.DamageMultiplier,  "DAMAGE",        "OFFENSE",  StatFormat.Multiplier),
            E(StatId.CritChance,        "CRIT CHANCE",   "OFFENSE",  StatFormat.Percent),
            E(StatId.CritDamage,        "CRIT DAMAGE",   "OFFENSE",  StatFormat.Multiplier),
            E(StatId.FireRate,          "FIRE RATE",     "OFFENSE",  StatFormat.Multiplier),
            E(StatId.ReloadSpeed,       "RELOAD SPEED",  "OFFENSE",  StatFormat.Multiplier),
            E(StatId.CooldownRate,      "COOLDOWN RATE", "OFFENSE",  StatFormat.Multiplier),
        };
    }
}