using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public class ResourceInventory : MonoBehaviour
{
    [Serializable] public struct Entry { public ResourceType type; public int amount; }

    [SerializeField] List<Entry> startingAmounts = new List<Entry>();

    readonly Dictionary<ResourceType, int> amounts = new Dictionary<ResourceType, int>();

    public event Action<ResourceType, int> Changed;   // ชนิด, จำนวนใหม่

    public IReadOnlyDictionary<ResourceType, int> Amounts => amounts;

    void Awake()
    {
        foreach (Entry e in startingAmounts)
            if (e.type != null) amounts[e.type] = Mathf.Clamp(e.amount, 0, e.type.maxAmount);
    }

    public int Get(ResourceType t) => t != null && amounts.TryGetValue(t, out int v) ? v : 0;

    // เพิ่ม คืนจำนวนที่ใส่ได้จริง (เกินเพดานจะถูกตัด)
    public int Add(ResourceType t, int n)
    {
        if (t == null || n <= 0) return 0;
        int cur = Get(t);
        int added = Mathf.Min(n, t.maxAmount - cur);
        if (added <= 0) return 0;
        Set(t, cur + added);
        return added;
    }

    // เอาออกเท่าที่มี (ไม่พอก็ได้เท่าที่เหลือ) คืนจำนวนที่ได้จริง
    public int Take(ResourceType t, int n)
    {
        if (t == null || n <= 0) return 0;
        int taken = Mathf.Min(n, Get(t));
        if (taken <= 0) return 0;
        Set(t, Get(t) - taken);
        return taken;
    }

    // เอาออกแบบต้องครบ ไม่พอ = ไม่หัก คืน false (ไว้ใช้กับ repair kit)
    public bool TryConsume(ResourceType t, int n)
    {
        if (Get(t) < n) return false;
        Take(t, n);
        return true;
    }

    void Set(ResourceType t, int value)
    {
        amounts[t] = value;
        Changed?.Invoke(t, value);
    }

    [ContextMenu("Debug: refill to starting amounts")]
    void DebugRefill()
    {
        foreach (Entry e in startingAmounts)
            if (e.type != null) Add(e.type, e.amount);
    }
}