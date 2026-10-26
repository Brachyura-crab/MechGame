using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MechStats))]
public class StatusController : MonoBehaviour
{
    public class ActiveStatus
    {
        public StatusData data;
        public float remaining;
        public GameObject source;
    }

    [SerializeField] TierData tier;

    readonly List<ActiveStatus> active = new List<ActiveStatus>();
    MechStats mech;
    Rigidbody2D rb;
    float knockbackEndTime;

    public IReadOnlyList<ActiveStatus> Active => active;
    public ControlFlags Blocked { get; private set; }
    public bool CanMove => (Blocked & ControlFlags.Move) == 0;
    public bool CanAttack => (Blocked & ControlFlags.Attack) == 0;
    public bool CanCastSkills => (Blocked & ControlFlags.Skills) == 0;
    public bool ShieldDisabled => (Blocked & ControlFlags.ShieldOff) != 0;
    public TierData Tier => tier;
    public bool IsKnockedBack => Time.time < knockbackEndTime;   // ไว้ให้ AI ศัตรูเช็คว่าอย่าเพิ่งคุมความเร็วเอง

    public event Action Changed;   // รายการสถานะเปลี่ยน (ติดใหม่/หมด) ใช้กับไอคอน

    void Awake()
    {
        mech = GetComponent<MechStats>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        bool removed = false;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            active[i].remaining -= Time.deltaTime;
            if (active[i].remaining <= 0f)
            {
                RemoveAt(i);
                removed = true;
            }
        }
        if (removed) { RecalculateFlags(); Changed?.Invoke(); }
    }

    public void Apply(StatusData data, GameObject source, float durationOverride = -1f)
    {
        if (data == null) return;

        float duration = durationOverride > 0f ? durationOverride : data.duration;
        if (data.affectedByControlResist && tier != null)
            duration *= 1f - Mathf.Clamp01(tier.controlResist);
        if (duration <= 0f) return;

        // ติดซ้ำ: Refresh = ต่อเวลา / Stack = ซ้อนจนถึง maxStacks แล้วรีเฟรชชั้นที่เหลือน้อยสุด
        int count = 0;
        ActiveStatus weakest = null;
        foreach (ActiveStatus a in active)
        {
            if (a.data != data) continue;
            count++;
            if (weakest == null || a.remaining < weakest.remaining) weakest = a;
        }

        int limit = data.stackRule == StackRule.Refresh ? 1 : Mathf.Max(1, data.maxStacks);
        if (count >= limit && weakest != null)
        {
            weakest.remaining = Mathf.Max(weakest.remaining, duration);
            weakest.source = source;
            return;
        }

        var entry = new ActiveStatus { data = data, remaining = duration, source = source };
        active.Add(entry);

        if (data.modifiers != null)
        {
            foreach (StatusModifierDef m in data.modifiers)
                mech.Stats.AddModifier(new StatModifier(m.stat, m.type, m.value, entry));   // entry เป็น source ไว้ถอด
        }

        RecalculateFlags();
        Changed?.Invoke();
    }

    public void Knockback(Vector2 direction, float speed, float lockTime = 0.3f)
    {
        if (rb == null || direction.sqrMagnitude < 0.0001f) return;

        float resist = tier != null ? Mathf.Clamp01(tier.knockbackResist) : 0f;
        float s = speed * (1f - resist);
        if (s <= 0f) return;

        rb.linearVelocity += direction.normalized * s;
        knockbackEndTime = Mathf.Max(knockbackEndTime, Time.time + lockTime);   // ไม่ทำให้ล็อกที่ค้างอยู่สั้นลง
    }

    public void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) RemoveAt(i);
        RecalculateFlags();
        Changed?.Invoke();
    }

    public void Remove(StatusData data)
    {
        bool removed = false;
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i].data == data) { RemoveAt(i); removed = true; }
        if (removed) { RecalculateFlags(); Changed?.Invoke(); }
    }

    public bool Has(StatusData data)
    {
        for (int i = 0; i < active.Count; i++)
            if (active[i].data == data) return true;
        return false;
    }

    // เวลาที่เหลือของสถานะนี้ (ชั้นที่นานที่สุด) 0 = ไม่ได้ติดอยู่
    public float RemainingOf(StatusData data)
    {
        float best = 0f;
        for (int i = 0; i < active.Count; i++)
            if (active[i].data == data && active[i].remaining > best) best = active[i].remaining;
        return best;
    }

    void RemoveAt(int i)
    {
        mech.Stats.RemoveModifiersFromSource(active[i]);
        active.RemoveAt(i);
    }

    void RecalculateFlags()
    {
        ControlFlags f = ControlFlags.None;
        foreach (ActiveStatus a in active) f |= a.data.blocks;
        Blocked = f;
    }
}