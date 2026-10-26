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

        // โมดูลพลังงานเสียจะลด EnergyRegen ผ่าน modifier อยู่แล้ว (0 = ไม่ฟื้น)
        float regen = mech.Stats.Get(StatId.EnergyRegen);
        if (regen > 0f && Current < max && Time.time - lastUseTime >= regenDelay)
        {
            Current = Mathf.Min(max, Current + regen * Time.deltaTime);
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