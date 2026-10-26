using UnityEngine;
using System.Collections.Generic;

public abstract class SkillData : ScriptableObject
{
    [Header("Info")]
    public string id;
    public string displayName;
    public Sprite icon;          // ไว้ใช้กับ HUD ทีหลัง
    [TextArea] public string description;

    [Header("Cooldown")]
    public float cooldown = 5f;

    [Header("Magazine (นัดแบบกระสุน)")]
    public int magazineSize = 0;        // ใช้กับสกิลที่ไม่ override MagazineSize (0 = ไม่จำกัดนัด)
    public ResourceType ammoResource;   // นัดสำรองที่ใช้ reload (ว่าง = reload ได้ไม่จำกัด)

    public virtual int MagazineSize => magazineSize;

    public abstract void Activate(MechSkills user, int rounds);   // rounds = จำนวนนัดที่ยิง (ถ้าสกิลไม่จำกัดนัด = 0)

    public virtual void GetInfo(MechStats mech, List<InfoLine> lines)
    {
        float rate = Mathf.Max(0.01f, mech.Stats.Get(StatId.CooldownRate));
        lines.Add(new InfoLine("COOLDOWN", (cooldown / rate).ToString("0.#") + "s"));
        if (MagazineSize > 0) lines.Add(new InfoLine("MAGAZINE", MagazineSize.ToString()));
    }
}