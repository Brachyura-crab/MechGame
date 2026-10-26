using UnityEngine;

public enum ModuleDamage { None = 0, Light = 1, Medium = 2, Heavy = 3 }

[CreateAssetMenu(menuName = "Mech/ModuleData")]
public class ModuleData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    [Min(0f)] public float hitWeight = 1f;   // น้ำหนักตอนสุ่มว่าโมดูลไหนโดน
    public BodyPartData bodyPart;   // โมดูลนี้อยู่ชิ้นส่วนไหน
    [Header("คุม stat (ติ๊กเมื่อโมดูลนี้ควบคุม stat ของหุ่น เช่น โมดูลโล่ = ShieldRegen)")]
    public bool scalesStat;
    public StatId stat;   // stat จะถูกคูณด้วยประสิทธิภาพของโมดูล (เสียหนัก = 0)

    [Tooltip("ประสิทธิภาพ 0-1 ตามระดับ: None, Light, Medium, Heavy (0 = ใช้ไม่ได้)")]
    public float[] efficiency = { 1f, 0.6f, 0.3f, 0f };

    public float Efficiency(ModuleDamage d)
    {
        int i = (int)d;
        return i < efficiency.Length ? efficiency[i] : 0f;
    }

    [Tooltip("repair kit ที่ใช้ซ่อมหนึ่งขั้นจากระดับนั้น: None, Light, Medium, Heavy (Light = ฟรี)")]
    public int[] repairCost = { 0, 0, 1, 1 };

    [Tooltip("เวลาซ่อมหนึ่งขั้น (วินาทีเกม) จากระดับนั้น")]
    public float[] repairTime = { 0f, 2f, 3f, 4f };

    public int RepairCost(ModuleDamage d)
    {
        int i = (int)d;
        return i < repairCost.Length ? repairCost[i] : 0;
    }

    public float RepairTime(ModuleDamage d)
    {
        int i = (int)d;
        return i < repairTime.Length ? repairTime[i] : 0f;
    }
}