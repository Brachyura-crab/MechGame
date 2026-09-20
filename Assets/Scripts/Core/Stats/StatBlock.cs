using System.Collections.Generic;

public class StatValue
{
    public float baseValue;
    public float min = 0f;
    public float max = float.MaxValue;

    readonly List<StatModifier> mods = new List<StatModifier>();
    float cached;
    bool dirty = true;

    public float Value
    {
        get
        {
            if (dirty) Recalculate();
            return cached;
        }
    }

    public void SetBase(float v)
    {
        baseValue = v;
        dirty = true;
    }

    public void Add(StatModifier m)
    {
        mods.Add(m);
        dirty = true;
    }

    public bool RemoveFromSource(object source)
    {
        int removed = mods.RemoveAll(m => m.source == source);
        if (removed > 0) dirty = true;
        return removed > 0;
    }

    void Recalculate()
    {
        float flat = 0f, increased = 0f, more = 1f;
        for (int i = 0; i < mods.Count; i++)
        {
            switch (mods[i].type)
            {
                case ModType.Flat:      flat += mods[i].value; break;
                case ModType.Increased: increased += mods[i].value; break;
                case ModType.More:      more *= mods[i].value; break;
            }
        }
        float v = (baseValue + flat) * (1f + increased) * more;
        cached = v < min ? min : (v > max ? max : v);
        dirty = false;
    }
}

public class StatBlock
{
    readonly StatValue[] values = new StatValue[(int)StatId.Count];

    public StatBlock()
    {
        for (int i = 0; i < values.Length; i++)
            values[i] = new StatValue();
    }

    public float Get(StatId id) => values[(int)id].Value;

    public void SetBase(StatId id, float v) => values[(int)id].SetBase(v);

    public void AddModifier(StatModifier m) => values[(int)m.stat].Add(m);

    // ถอดทุกตัวปรับที่มาจากแหล่งนี้ ในทุกสถิติ
    public void RemoveModifiersFromSource(object source)
    {
        for (int i = 0; i < values.Length; i++)
            values[i].RemoveFromSource(source);
    }
}