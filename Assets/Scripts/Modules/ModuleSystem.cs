using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health))]
public class ModuleSystem : MonoBehaviour
{
    [SerializeField] ModuleData[] modules;

    [Header("การเสียหาย (เกิดเมื่อดาเมจทะลุโล่ถึง HP)")]
    [SerializeField] float chanceScale = 3f;           // โอกาส = (HP ที่เสียต่อโดน / MaxHP) x ค่านี้
    [SerializeField] float mediumHitFraction = 0.08f;  // เสีย HP เกินสัดส่วนนี้ = เสียระดับกลาง
    [SerializeField] float heavyHitFraction = 0.2f;    // เกินสัดส่วนนี้ = เสียระดับหนัก

        [Tooltip("แกน X = เลือดที่เหลือ (0-1), แกน Y = สัดส่วนของแต้มความเสียหายรวมที่เสียได้ (0 = ไม่เสียเลย, 1 = เสียหนักทุกตัว)")]
    [SerializeField] AnimationCurve damagedLimitByHp = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.3f, 0.7f), new Keyframe(0.7f, 0.2f), new Keyframe(1f, 0f));

    [SerializeField] float hitLockout = 0.75f;         // กันเสียซ้ำจากชุดกระสุนเดียวกัน
    [Header("ซ่อม")]
    [SerializeField] ResourceType repairKit;           // ว่าง = ซ่อมฟรี (ไว้ทดสอบ)
    [SerializeField] bool interruptOnHpDamage = true;  // ดาเมจถึง HP ระหว่างซ่อม = ยกเลิก (ไม่เสีย kit)

    ResourceInventory inventory;
    MechStats mech;
    float repairElapsed, repairDuration;

    [Header("Debug")]
    [SerializeField] bool debugKeys = true;   // เลข 1-9 = ทำโมดูลเสียแย่ลง 1 ขั้น, Shift+เลข = ซ่อม 1 ขั้น
    [SerializeField] bool debugGui = true;
    [SerializeField] Vector2 debugGuiPos = new Vector2(20f, 250f);

    Health health;
    readonly Dictionary<ModuleData, ModuleDamage> state = new Dictionary<ModuleData, ModuleDamage>();
    float lastRollTime = -999f;

    public event Action<ModuleData, ModuleDamage> Changed;   // โมดูล, ระดับใหม่
    public event Action<ModuleData> RepairStarted, RepairFinished, RepairCancelled;
    public ModuleData Repairing { get; private set; }
    public float RepairProgress => Repairing != null ? Mathf.Clamp01(repairElapsed / repairDuration) : 0f;
    public ResourceType RepairKit => repairKit;
    public IReadOnlyList<ModuleData> Modules => modules;

    void Awake()
    {
        health = GetComponent<Health>();
        foreach (ModuleData m in modules)
            if (m != null) state[m] = ModuleDamage.None;
        inventory = GetComponent<ResourceInventory>();
        mech = GetComponent<MechStats>();   // Health บังคับให้มี MechStats อยู่แล้ว
        foreach (ModuleData m in modules) ApplyStatEffect(m);
    }

    void OnEnable() => health.Damaged += OnDamaged;
    void OnDisable() => health.Damaged -= OnDamaged;

    public ModuleDamage Tier(ModuleData m) =>
        m != null && state.TryGetValue(m, out ModuleDamage t) ? t : ModuleDamage.None;

    // 1 = ปกติ (โมดูลที่ไม่ได้ลงทะเบียน/ไม่ได้ตั้ง = ปกติเสมอ)
    public float Efficiency(ModuleData m) => m == null ? 1f : m.Efficiency(Tier(m));

    public bool IsOnline(ModuleData m) => Efficiency(m) > 0f;

    // ระดับเสียของชิ้นส่วน = ระดับที่หนักที่สุดของโมดูลย่อยในชิ้นนั้น (ไม่มีโมดูลย่อย = ปกติ)
    public ModuleDamage PartTier(BodyPartData part)
    {
        ModuleDamage worst = ModuleDamage.None;
        foreach (ModuleData m in modules)
            if (m != null && m.bodyPart == part && Tier(m) > worst) worst = Tier(m);
        return worst;
    }

    // เติมรายการโมดูลย่อยของชิ้นส่วนลงใน list ของผู้เรียก (ใช้ list เดิมซ้ำ ไม่สร้างขยะทุกเฟรม)
    public void GetModules(BodyPartData part, List<ModuleData> result)
    {
        result.Clear();
        foreach (ModuleData m in modules)
            if (m != null && m.bodyPart == part) result.Add(m);
    }

    public int DamagedCount
    {
        get
        {
            int n = 0;
            foreach (ModuleData m in modules)
                if (m != null && Tier(m) != ModuleDamage.None) n++;
            return n;
        }
    }

    // แต้มความเสียหายรวมตอนนี้ (Light=1, Medium=2, Heavy=3 รวมทุกโมดูล)
    public int DamagePoints
    {
        get
        {
            int n = 0;
            foreach (ModuleData m in modules)
                if (m != null) n += (int)Tier(m);
            return n;
        }
    }

    // งบแต้มความเสียหายที่เสียได้ ณ ตอนนี้ ขึ้นกับเลือดที่เหลือ
    public int DamageBudgetNow
    {
        get
        {
            int count = 0;
            foreach (ModuleData m in modules)
                if (m != null) count++;
            float hpRatio = Mathf.Clamp01(health.Current / Mathf.Max(1f, health.Max));
            float frac = Mathf.Clamp01(damagedLimitByHp.Evaluate(hpRatio));
            return Mathf.RoundToInt(frac * count * (int)ModuleDamage.Heavy);
        }
    }

    void OnDamaged(DamageResult r)
    {
        if (Repairing != null && interruptOnHpDamage && r.hpDamage > 0f) CancelRepair();
        if (r.hpDamage <= 0f || health.IsDead) return;
        if (Time.time - lastRollTime < hitLockout) return;
        if (DamagePoints >= DamageBudgetNow) return;   // งบเต็มแล้ว ไม่เสียเพิ่ม

        float frac = r.hpDamage / Mathf.Max(1f, health.Max);
        if (UnityEngine.Random.value >= Mathf.Clamp01(frac * chanceScale)) return;

        ModuleData target = PickTarget();
        if (target == null) return;

        lastRollTime = Time.time;
        ModuleDamage rolled = frac >= heavyHitFraction ? ModuleDamage.Heavy
                            : frac >= mediumHitFraction ? ModuleDamage.Medium
                            : ModuleDamage.Light;
        HitCapped(target, rolled, DamageBudgetNow - DamagePoints);
    }

    // สุ่มตามน้ำหนัก ถ้าเสียครบจำนวนสูงสุดแล้ว เลือกได้เฉพาะโมดูลที่เสียอยู่ (จะแย่ลง)
    ModuleData PickTarget()
    {
        float total = 0f;
        foreach (ModuleData m in modules)
            if (IsCandidate(m)) total += m.hitWeight;
        if (total <= 0f) return null;

        float roll = UnityEngine.Random.value * total;
        foreach (ModuleData m in modules)
        {
            if (!IsCandidate(m)) continue;
            roll -= m.hitWeight;
            if (roll <= 0f) return m;
        }
        return null;
    }

    bool IsCandidate(ModuleData m) => m != null && Tier(m) < ModuleDamage.Heavy;

    // เหมือน Hit แต่ไม่ให้เกินงบแต้มที่เหลือ
    void HitCapped(ModuleData m, ModuleDamage rolled, int pointsLeft)
    {
        int cur = (int)Tier(m);
        int next = cur >= (int)rolled ? cur + 1 : (int)rolled;
        next = Mathf.Min(next, cur + pointsLeft, (int)ModuleDamage.Heavy);
        SetTier(m, (ModuleDamage)next);
    }

    // ทำให้เสียตามระดับ ถ้าเสียอยู่แล้วเท่ากันหรือหนักกว่า = แย่ลงอีกหนึ่งขั้น
    public void Hit(ModuleData m, ModuleDamage rolled)
    {
        ModuleDamage cur = Tier(m);
        int next = cur >= rolled ? (int)cur + 1 : (int)rolled;
        SetTier(m, (ModuleDamage)Mathf.Min(next, (int)ModuleDamage.Heavy));
    }

    public void SetTier(ModuleData m, ModuleDamage t)
    {
        if (m == null || !state.ContainsKey(m) || state[m] == t) return;
        state[m] = t;
        ApplyStatEffect(m);
        if (m == Repairing) CancelRepair();
        Changed?.Invoke(m, t);
    }

    // โมดูลที่ผูกกับ stat: ใส่ตัวคูณ (ประสิทธิภาพ) ลงใน StatBlock ปกติ = ไม่มีตัวคูณ
    void ApplyStatEffect(ModuleData m)
    {
        if (m == null || !m.scalesStat || mech == null) return;

        StatBlock s = mech.Stats;
        s.RemoveModifiersFromSource(m);

        float eff = m.Efficiency(Tier(m));
        if (!Mathf.Approximately(eff, 1f))
            s.AddModifier(new StatModifier(m.stat, ModType.More, eff, m));
    }

    public void Repair(ModuleData m, int steps = 1) =>
        SetTier(m, (ModuleDamage)Mathf.Max(0, (int)Tier(m) - steps));

    public void RepairAll()
    {
        foreach (ModuleData m in modules) SetTier(m, ModuleDamage.None);
    }

    public int RepairCost(ModuleData m) => m == null ? 0 : m.RepairCost(Tier(m));
    public float RepairTime(ModuleData m) => m == null ? 0f : m.RepairTime(Tier(m));
    int Kits => repairKit != null && inventory != null ? inventory.Get(repairKit) : int.MaxValue;

    public bool CanRepair(ModuleData m) =>
        m != null && !health.IsDead && Repairing == null
        && Tier(m) != ModuleDamage.None && Kits >= RepairCost(m);

    public bool StartRepair(ModuleData m)
    {
        if (!CanRepair(m)) return false;
        Repairing = m;
        repairElapsed = 0f;
        repairDuration = Mathf.Max(0.01f, RepairTime(m));
        RepairStarted?.Invoke(m);
        return true;
    }

    public void CancelRepair()
    {
        if (Repairing == null) return;
        ModuleData m = Repairing;
        Repairing = null;
        RepairCancelled?.Invoke(m);
    }

    // เรียกเมื่อโมดูลถูกใช้งานจริง ถ้ากำลังซ่อมโมดูลนั้นอยู่ = ยกเลิกซ่อม (ไม่เสีย kit)
    public void NotifyUsed(ModuleData m)
    {
        if (m != null && m == Repairing) CancelRepair();
    }

    void FinishRepair()
    {
        ModuleData m = Repairing;
        int cost = RepairCost(m);
        Repairing = null;
        if (repairKit != null && inventory != null && !inventory.TryConsume(repairKit, cost))
        {
            RepairCancelled?.Invoke(m);   // kit ถูกใช้ไปที่อื่นระหว่างรอ
            return;
        }
        Repair(m);
        RepairFinished?.Invoke(m);
    }

    void Update()
    {
        if (Repairing != null)
        {
            if (health.IsDead) CancelRepair();
            else
            {
                repairElapsed += Time.deltaTime;
                if (repairElapsed >= repairDuration) FinishRepair();
            }
        }

        if (!debugKeys || !DebugGate.Enabled || Keyboard.current == null) return;
        bool shift = Keyboard.current.leftShiftKey.isPressed;
        for (int i = 0; i < modules.Length && i < 9; i++)
        {
            Key key = (Key)((int)Key.Digit1 + i);
            if (!Keyboard.current[key].wasPressedThisFrame) continue;
            bool ctrl = Keyboard.current.leftCtrlKey.isPressed;
            if (ctrl) StartRepair(modules[i]);          // ซ่อมจริง (ใช้ kit + เวลา)
            else if (shift) Repair(modules[i]);         // ซ่อมทันทีไม่เสีย kit (debug)
            else Hit(modules[i], ModuleDamage.Light);
        }
    }

    void OnGUI()
    {
        if (!debugGui || !DebugGate.Visible) return;
        for (int i = 0; i < modules.Length; i++)
        {
            ModuleData m = modules[i];
            if (m == null) continue;
            string extra = Tier(m) != ModuleDamage.None
                ? "  cost " + RepairCost(m) + " / " + RepairTime(m).ToString("F1") + "s" : "";
            if (m == Repairing) extra += "  REPAIRING " + Mathf.RoundToInt(RepairProgress * 100f) + "%";
                DebugOverlay.Label((i + 1) + "  " + m.displayName + "  " + Tier(m) + "  eff " + Efficiency(m).ToString("F2") + extra + "  [" + (m.bodyPart != null ? m.bodyPart.displayName + ":" + PartTier(m.bodyPart) : "-") + "]");
        }
        if (repairKit != null && inventory != null)
                DebugOverlay.Label("Repair kits " + inventory.Get(repairKit));
    }
}