public static class UpgradeApplier
{
    public static void Apply(StatBlock stats, object source)
    {
        var entry = GameSession.SelectedMech;
        var cfg = GameConfig.Instance;
        if (entry == null || cfg == null || cfg.upgrades == null || cfg.upgrades.defs == null) return;

        foreach (var d in cfg.upgrades.defs)
        {
            int lv = SaveService.Level(entry.id, d.kind);
            if (lv <= 0) continue;
            float inc = cfg.upgrades.BonusPercent(d, lv) / 100f;   // 0.2 = +20%
            stats.AddModifier(new StatModifier(ToStat(d.kind), ModType.Increased, inc, source));
        }
    }

    public static StatId ToStat(UpgradeKind k)
    {
        switch (k)
        {
            case UpgradeKind.MaxHp: return StatId.MaxHP;
            case UpgradeKind.ShieldRegen: return StatId.ShieldRegen;
            case UpgradeKind.EnergyRegen: return StatId.EnergyRegen;
            case UpgradeKind.MaxSpeed: return StatId.MaxSpeed;
            default: return StatId.FireRate;
        }
    }
}