using System;
using UnityEngine;

[RequireComponent(typeof(MechStats))]
public class Health : MonoBehaviour
{
    MechStats mech;
    float lastMax;
    [SerializeField] float shieldRegenDelay = 3f;
    float lastMaxShield;
    float lastDamageTime = -999f;

    public float Current { get; private set; }
    public float Max => mech.Stats.Get(StatId.MaxHP);
    public bool IsDead => Current <= 0f;

    public float Shield { get; private set; }
    public float MaxShield => mech.Stats.Get(StatId.MaxShield);
    public StatBlock Stats => mech.Stats;

    public event Action<float, float> ShieldChanged; // current, max
    public event Action<DamageResult> Damaged;

    public event Action<float, float> Changed; // current, max
    public event Action Died;

    void Awake()
    {
        mech = GetComponent<MechStats>();
        lastMax = Max;
        Current = lastMax;
        lastMaxShield = MaxShield;
        Shield = lastMaxShield;
    }

    void Update()
    {
        // ถ้า MaxHP เปลี่ยน (จากตัวปรับ) ให้ตัดค่าปัจจุบันไม่ให้เกินและแจ้ง UI
        float max = Max;
        if (!Mathf.Approximately(max, lastMax))
        {
            lastMax = max;
            Current = Mathf.Min(Current, max);
            Changed?.Invoke(Current, max);
        }

        float maxShield = MaxShield;
        if (!Mathf.Approximately(maxShield, lastMaxShield))
        {
            lastMaxShield = maxShield;
            Shield = Mathf.Min(Shield, maxShield);
            ShieldChanged?.Invoke(Shield, maxShield);
        }

        // โล่ฟื้นเมื่อพ้นช่วงไม่โดนดาเมจ
        if (!IsDead && Shield < maxShield && Time.time - lastDamageTime >= shieldRegenDelay)
        {
            Shield = Mathf.Min(maxShield, Shield + mech.Stats.Get(StatId.ShieldRegen) * Time.deltaTime);
            ShieldChanged?.Invoke(Shield, maxShield);
        }
    }

        // จุดเดียวที่ดาเมจสุดท้ายเข้ามา (เรียกจาก DamageCalculator)
    public void TakeDamage(DamageResult r)
    {
        if (IsDead || r.amount <= 0f) return;

        lastDamageTime = Time.time;
        float remaining = r.amount;

        if (Shield > 0f)
        {
            float absorbed = Mathf.Min(Shield, remaining);
            Shield -= absorbed;
            remaining -= absorbed;
            ShieldChanged?.Invoke(Shield, MaxShield);
        }

        if (remaining > 0f)
        {
            Current = Mathf.Max(0f, Current - remaining);
            Changed?.Invoke(Current, Max);
        }

        Damaged?.Invoke(r);
        if (Current <= 0f) Died?.Invoke();
    }

    // สำหรับทดสอบ: ส่งดาเมจตรงๆ ข้ามสูตร
    public void Damage(float amount) => TakeDamage(new DamageResult { amount = amount, type = DamageType.Kinetic });

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Min(Max, Current + amount);
        Changed?.Invoke(Current, Max);
    }

        public void ResetFull()
    {
        Current = Max;
        Shield = MaxShield;
        lastDamageTime = -999f;
        Changed?.Invoke(Current, Max);
        ShieldChanged?.Invoke(Shield, MaxShield);
    }
}