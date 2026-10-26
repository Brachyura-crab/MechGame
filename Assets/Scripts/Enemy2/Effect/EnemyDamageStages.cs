using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDamageStages : MonoBehaviour
{
    [SerializeField] DamageStageData data;

    // -1 = ปกติ, 0+ = index ของขั้นที่ใช้อยู่ (ไว้ต่อกับเอฟเฟกต์/สี)
    public event Action<int> StageChanged;
    public int CurrentStage { get; private set; } = -1;

    Health health;
    StatBlock stats;
    bool started;

    void Awake() => health = GetComponent<Health>();

    void Start()
    {
        stats = health.Stats;     // เก็บไว้ใช้ตอนปิด (ปลอดภัยแม้ component อื่นถูกทำลายไปก่อน)
        started = true;
        Evaluate(health.Current, health.Max, true);
    }

    void OnEnable()
    {
        health.Changed += OnHealthChanged;
        if (started) Evaluate(health.Current, health.Max, true);   // เปิดซ้ำจาก pool
    }

    void OnDisable()
    {
        health.Changed -= OnHealthChanged;
        if (stats != null) stats.RemoveModifiersFromSource(this);
        CurrentStage = -1;
    }

    void OnHealthChanged(float current, float max) => Evaluate(current, max, false);

    void Evaluate(float current, float max, bool force)
    {
        if (stats == null || data == null) return;

        float ratio = max > 0f ? current / max : 0f;

        // หาขั้นที่ลึกที่สุดที่ HP ต่ำกว่า
        int idx = -1;
        float best = float.MaxValue;
        for (int i = 0; i < data.stages.Count; i++)
        {
            var s = data.stages[i];
            if (ratio < s.hpBelow && s.hpBelow < best) { best = s.hpBelow; idx = i; }
        }

        if (idx == CurrentStage && !force) return;

        stats.RemoveModifiersFromSource(this);
        if (idx >= 0)
        {
            foreach (var m in data.stages[idx].mods)
                stats.AddModifier(new StatModifier(m.stat, m.type, m.value, this));
        }

        CurrentStage = idx;
        StageChanged?.Invoke(idx);
    }
}