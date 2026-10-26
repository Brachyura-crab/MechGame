using System;
using UnityEngine;
using EnemyV2;

// แต้มแลกเสบียง วางบนผู้เล่น (object เดียวกับ ResourceInventory)
// ได้แต้มจากการฆ่าศัตรู (ตาม tier) และจากการปิดประตู
public class ScorePoints : MonoBehaviour
{
    [SerializeField] int startingPoints = 100;
    [SerializeField] int defaultKillPoints = 5;   // ศัตรูที่ไม่มี tier
    [SerializeField] int gateClosedPoints = 100;

    public int Points { get; private set; }
    public int TotalEarned { get; private set; }   // รวมคะแนนที่หาได้ ไม่หักตอนใช้จ่าย
    public event Action<int> Changed;   // แต้มใหม่

    void Awake() => Points = startingPoints;

    void OnEnable()
    {
        Health.AnyDied += OnAnyDied;
        Gate.AnyClosed += OnGateClosed;
    }

    void OnDisable()
    {
        Health.AnyDied -= OnAnyDied;
        Gate.AnyClosed -= OnGateClosed;
    }

    // นับเฉพาะที่ผู้เล่นเป็นคนฆ่า (เหมือน UltimateGauge)
    void OnAnyDied(Health victim, DamageResult r)
    {
        if (r.source != gameObject) return;
        StatusController sc = victim.GetComponent<StatusController>();
        TierData tier = sc != null ? sc.Tier : null;
        Add(tier != null ? tier.scoreOnKill : defaultKillPoints);
    }

    void OnGateClosed(Gate g) => Add(gateClosedPoints);

    public void Add(int n)
    {
        if (n <= 0) return;
        Points += n;
        TotalEarned += n;
        Changed?.Invoke(Points);
    }

    public bool TrySpend(int n)
    {
        if (n < 0 || Points < n) return false;
        Points -= n;
        Changed?.Invoke(Points);
        return true;
    }
}