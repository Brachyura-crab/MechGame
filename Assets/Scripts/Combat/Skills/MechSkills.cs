using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MechStats))]
public class MechSkills : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public SkillData skill;
        public InputActionReference input;
        [HideInInspector] public float remaining;   // คูลดาวน์ที่เหลือ (หน่วยวินาทีของคูลดาวน์ฐาน)
        [HideInInspector] public int loaded;
    }

    [SerializeField] Slot[] slots = new Slot[4];
    [SerializeField] bool debugGui = true;
    [SerializeField] Vector2 debugGuiPos = new Vector2(20f, 80f);

    public MechStats Mech { get; private set; }
    public MechAim Aim { get; private set; }
    StatusController status;
    ResourceInventory inventory;
    public bool IsBlocked => !CanCast || (status != null && !status.CanCastSkills);
    public bool CanCast { get; set; } = true;   // ไว้ให้สถานะ (Stun/Silence) ปิดการใช้สกิล

    public event Action<int> SkillCast;         // ส่งเลขช่องที่ใช้

    void Awake()
    {
        Mech = GetComponent<MechStats>();
        status = GetComponent<StatusController>();
        Aim = GetComponent<MechAim>();
        inventory = GetComponent<ResourceInventory>();
        foreach (Slot s in slots)
            if (s != null && s.skill != null) s.loaded = s.skill.MagazineSize;
    }

    void OnEnable()
    {
        foreach (Slot s in slots)
            if (s != null && s.input != null) s.input.action.Enable();
    }

    void Update()
    {
        float rate = Mathf.Max(0.01f, Mech.Stats.Get(StatId.CooldownRate));

        // ลดคูลดาวน์ตามอัตราปัจจุบัน (อัลติ CooldownRate x10 จึงเร่งของที่กำลังนับอยู่ด้วย)
        foreach (Slot s in slots)
            if (s != null && s.remaining > 0f)
                s.remaining = Mathf.Max(0f, s.remaining - Time.deltaTime * rate);

        foreach (Slot s in slots)
            if (s != null && s.skill != null) TryRefill(s);

        if (IsBlocked) return;

        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            if (s == null || s.skill == null || s.input == null) continue;
            if (s.input.action.WasPressedThisFrame() && s.remaining <= 0f && HasShots(s)) Cast(i);
        }
    }

    void Cast(int index)
    {
        Slot s = slots[index];
        int rounds = 0;
        if (Limited(s.skill))
        {
            rounds = s.loaded;      // ยิงทั้งหมดที่โหลดอยู่
            s.loaded = 0;
        }
        s.remaining = s.skill.cooldown;   // คูลดาวน์เริ่มนับตอนกดใช้
        s.skill.Activate(this, rounds);
        SkillCast?.Invoke(index);
    }

    // วินาทีจริงที่ต้องรอ (คิดตามอัตราปัจจุบัน)
    public float CooldownRemaining(int index) =>
        slots[index].remaining / Mathf.Max(0.01f, Mech.Stats.Get(StatId.CooldownRate));

    // 0-1 สำหรับวงหมุนบน HUD
    public float CooldownRatio(int index) =>
        slots[index].skill != null && slots[index].skill.cooldown > 0f
            ? slots[index].remaining / slots[index].skill.cooldown
            : 0f;

    static bool Limited(SkillData sk) => sk != null && sk.MagazineSize > 0;
    bool HasShots(Slot s) => !Limited(s.skill) || s.loaded > 0;
    bool HasReserve(SkillData sk) =>
        sk.ammoResource == null || inventory == null || inventory.Get(sk.ammoResource) > 0;

    // ไว้ให้ HUD (-1 = ไม่จำกัด)
    public int Loaded(int index) => Limited(slots[index].skill) ? slots[index].loaded : -1;
    public int Reserve(int index)
    {
        SkillData sk = slots[index].skill;
        return Limited(sk) && sk.ammoResource != null && inventory != null
            ? inventory.Get(sk.ammoResource) : -1;
    }
    void TryRefill(Slot s)
    {
        SkillData sk = s.skill;
        if (!Limited(sk) || s.remaining > 0f || s.loaded >= sk.MagazineSize || !HasReserve(sk)) return;

        int need = sk.MagazineSize - s.loaded;
        int got = sk.ammoResource != null && inventory != null
            ? inventory.Take(sk.ammoResource, need) : need;
        s.loaded += got;   // นัดสำรองไม่พอ = ได้เท่าที่มี
    }

    void OnGUI()
    {
        if (!debugGui) return;
        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            if (s == null || s.skill == null || s.input == null) continue;
            float rem = CooldownRemaining(i);
            string state = rem > 0f ? rem.ToString("F1") + "s" : "READY";
            
            if (Limited(s.skill))
            {
                state += "   " + s.loaded + "/" + s.skill.MagazineSize;
                if (Reserve(i) >= 0) state += "  Reserve " + Reserve(i);
                if (s.loaded <= 0 && !HasReserve(s.skill)) state = "NO AMMO";
            }

            GUI.Label(new Rect(debugGuiPos.x, debugGuiPos.y + i * 22f, 400f, 24f),
                s.input.action.name + "  " + s.skill.displayName + "  " + state);
        }
    }
}