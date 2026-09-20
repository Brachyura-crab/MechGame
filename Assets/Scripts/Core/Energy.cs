using System;
using UnityEngine;

[RequireComponent(typeof(MechStats))]
public class Energy : MonoBehaviour
{
    [SerializeField] float regenDelay = 1f; // หน่วงหลังใช้ก่อนเริ่มฟื้น (วินาที)

    MechStats mech;
    float lastMax;
    float lastUseTime = -999f;

    public float Current { get; private set; }
    public float Max => mech.Stats.Get(StatId.MaxEnergy);

    public event Action<float, float> Changed; // current, max

    void Awake()
    {
        mech = GetComponent<MechStats>();
        lastMax = Max;
        Current = lastMax;
    }

    void Update()
    {
        float max = Max;
        if (!Mathf.Approximately(max, lastMax))
        {
            lastMax = max;
            Current = Mathf.Min(Current, max);
            Changed?.Invoke(Current, max);
        }

        // ฟื้นเมื่อพ้นช่วงหน่วง (ระหว่าง Boost มีการใช้ทุกเฟรม ตัวจับเวลารีเซ็ตตลอด จึงไม่ฟื้นเอง)
        if (Current < max && Time.time - lastUseTime >= regenDelay)
        {
            Current = Mathf.Min(max, Current + mech.Stats.Get(StatId.EnergyRegen) * Time.deltaTime);
            Changed?.Invoke(Current, max);
        }
    }

    public bool Has(float amount) => Current >= amount;

    public void Use(float amount)
    {
        if (amount <= 0f) return;
        Current = Mathf.Max(0f, Current - amount);
        lastUseTime = Time.time;
        Changed?.Invoke(Current, Max);
    }
}