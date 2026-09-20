public enum ModType { Flat, Increased, More }

public struct StatModifier
{
    public StatId stat;
    public ModType type;
    public float value;     // Flat = บวกตรงๆ, Increased = 0.2 คือ +20%, More = 1.5 คือ x1.5
    public object source;   // ใครเป็นคนใส่ ใช้ตอนถอด

    public StatModifier(StatId stat, ModType type, float value, object source)
    {
        this.stat = stat;
        this.type = type;
        this.value = value;
        this.source = source;
    }
}