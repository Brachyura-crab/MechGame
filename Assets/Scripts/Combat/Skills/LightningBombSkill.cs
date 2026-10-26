using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Mech/Skills/Lightning Bomb")]
public class LightningBombSkill : SkillData
{
    [Header("Bomb")]
    public LightningBomb bombPrefab;
    public override int MagazineSize => 1;
    public float speed = 25f;
    public float maxRange = 15f;
    public float minRange = 1.5f;      // เมาส์อยู่ใกล้ตัวมากก็ให้บินอย่างน้อยเท่านี้

    [Header("Deceleration (ชะลอก่อนถึงจุดหมาย)")]
    public float slowDistance = 6f;      // ระยะก่อนถึงจุดหมายที่เริ่มชะลอ (ถ้าระยะทางทั้งหมดสั้นกว่านี้ จะใช้ 70% ของระยะทางแทน)
    [Tooltip("แกน X = ระยะที่เหลือในช่วงชะลอ (1 = เพิ่งเริ่มชะลอ, 0 = ถึงจุดหมาย), แกน Y = ตัวคูณความเร็ว")]
    public AnimationCurve slowCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 1f));
    public float minSpeed = 2f;          // ความเร็วต่ำสุด กันบินช้าจนไม่ถึงสักที
    public bool explodeOnContact = true; // true = ชนศัตรูระหว่างทางแล้วระเบิดทันที (เหมือนเดิม), false = บินผ่านไประเบิดที่จุดหมายเท่านั้น

    [Header("Blast")]
    public float blastRadius = 4f;
    public float damage = 30f;

    [Header("Status")]
    public StatusData stun;
    public float stunDuration = 3f;
    public StatusData shieldOff;
    public float shieldOffDuration = 5f;

    public override void GetInfo(MechStats mech, List<InfoLine> lines)
    {
        base.GetInfo(mech, lines);
        lines.Add(new InfoLine("DAMAGE", (damage * mech.Stats.Get(StatId.DamageMultiplier)).ToString("0.#")));
        lines.Add(new InfoLine("BLAST RADIUS", blastRadius.ToString("0.#")));
        lines.Add(new InfoLine("RANGE", maxRange.ToString("0.#")));
        lines.Add(new InfoLine("STUN", stunDuration.ToString("0.#") + "s"));
        lines.Add(new InfoLine("SHIELD OFF", shieldOffDuration.ToString("0.#") + "s"));
    }

    public override void Activate(MechSkills user, int rounds)
    {
        MechAim aim = user.Aim;
        Vector2 origin = user.transform.position;
        Vector2 dir = aim != null ? aim.AimDirection : Vector2.up;
        float dist = aim != null ? Vector2.Distance(origin, aim.AimPoint) : maxRange;
        dist = Mathf.Clamp(dist, minRange, maxRange);

        LightningBomb b = Instantiate(bombPrefab, origin, Quaternion.identity);
        b.Launch(dir, dist, this, user.Mech);
    }
}