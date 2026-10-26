using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD ช่องอาวุธ: ช่องที่ใช้ล่าสุดอยู่ด้านหน้า (สว่าง ใหญ่) ช่องอื่นอยู่ด้านหลัง (มืด เล็ก) และเลื่อนสลับกันนุ่มๆ
public class WeaponSlotsHud : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public Weapon weapon;
        public RectTransform root;     // ช่องทั้งอัน (ทุกช่องต้องใช้ Anchor เดียวกัน)

        [Tooltip("มืดตามตำแหน่งหน้า/หลัง ครอบทั้งช่อง (ว่างได้)")]
        public Image dim;
        [Tooltip("มืดตอน reload หรือกระสุนหมด เปิด/ปิดอย่างเดียว อยู่ภายในกรอบ (ว่างได้)")]
        [UnityEngine.Serialization.FormerlySerializedAs("statusDim")]
        public Image reloadDim;
        [Tooltip("หลอดกระสุน ต้องตั้ง Image Type = Filled, Fill Method = Horizontal และใส่ Sprite (ว่างได้)")]
        public Image fill;

        public TMP_Text ammoText;      // เช่น 12 / 30
        public TMP_Text reserveText;   // กระสุนสำรอง (ว่างได้)

        [System.NonSerialized] public float t;             // 0 = ด้านหลัง, 1 = ด้านหน้า
        [System.NonSerialized] public float fillShown = -1f;
        [System.NonSerialized] public int ammo = -1, magazine = -1, reserve = -2;
        [System.NonSerialized] public int lastAmmo = -1;
        [System.NonSerialized] public float pulse;          // 1 = เพิ่งยิง (หลอดเต็ม) ลดลงเป็น 0
        [System.NonSerialized] public float lastShotTime = -999f;
        [System.NonSerialized] public float shotInterval = 0.2f;

        [Tooltip("ModuleStatusIndicator ของช่องนี้ (ไว้ให้ ModuleDim ใช้ทีละชั้นกับ Reload/Position Dim)")]
        public ModuleStatusIndicator indicator;
    }

    [SerializeField] WeaponArbiter arbiter;
    [SerializeField] Slot[] slots;
    [SerializeField] int startFront;

    [Header("ตำแหน่ง (Anchored Position ของช่อง)")]
    [SerializeField] Vector2 frontPos = new Vector2(20f, 40f);
    [SerializeField] Vector2 backPos = new Vector2(-20f, -30f);
    [SerializeField, Range(0.5f, 1f)] float backScale = 0.9f;
    [SerializeField, Range(0f, 1f)] float dimAlpha = 0.55f;   // ความมืดของช่องด้านหลัง
    [SerializeField] float slideTime = 0.18f;

    [Header("หลอดกระสุน")]
    [SerializeField] float fillSpeed = 5f;   // ความเร็วที่หลอดวิ่งตามค่าจริง (เต็มหลอดต่อวินาที)
    [SerializeField, Range(0.1f, 1f)] float pulseDecayRatio = 0.8f;      // หลอดลดถึงค่าจริงภายในกี่ส่วนของช่วงห่างระหว่างนัด (1 = ลดพอดีถึงนัดถัดไป)
    [SerializeField] Vector2 pulseTimeRange = new Vector2(0.06f, 0.6f);   // เวลาลดต่ำสุด/สูงสุด (วินาที)

    int front = -1;

    void Start()
    {
        if (slots == null || slots.Length == 0) { enabled = false; return; }

        SetFront(Mathf.Clamp(startFront, 0, slots.Length - 1));
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].t = i == front ? 1f : 0f;   // เริ่มที่ตำแหน่งสุดท้ายเลย ไม่ต้องเลื่อน
            Apply(slots[i]);
        }
    }

    void Update()
    {
        // ช่องที่เพิ่งได้สิทธิ์ยิง = ช่องล่าสุด (Active เป็น null ตอนไม่ได้ยิง ค่าเดิมจึงค้างไว้)
        Weapon active = arbiter != null ? arbiter.Active : null;
        if (active != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].weapon == active && i != front) { SetFront(i); break; }
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            if (s.weapon == null || s.root == null) continue;

            float target = i == front ? 1f : 0f;
            s.t = Mathf.MoveTowards(s.t, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, slideTime));
            Apply(s);
            RefreshAmmo(s);
            RefreshFill(s);
            RefreshStatus(s);
        }
    }

    void SetFront(int i)
    {
        front = i;
        if (slots[i].root != null) slots[i].root.SetAsLastSibling();   // ช่องหน้าวาดทับช่องหลัง
    }

    void Apply(Slot s)
    {
        if (s.root == null) return;
        float e = Mathf.SmoothStep(0f, 1f, s.t);
        s.root.anchoredPosition = Vector2.Lerp(backPos, frontPos, e);
        s.root.localScale = Vector3.one * Mathf.Lerp(backScale, 1f, e);
        if (s.dim != null)
        {
            Color c = s.dim.color;
            c.a = Mathf.Lerp(dimAlpha, 0f, e);
            s.dim.color = c;
        }
    }

    void RefreshAmmo(Slot s)
    {
        Weapon w = s.weapon;
        int mag = w.Data.magazineSize;
        if (s.ammoText != null && (w.Ammo != s.ammo || mag != s.magazine))
        {
            s.ammo = w.Ammo;
            s.magazine = mag;
            s.ammoText.text = s.ammo + " / " + mag;
        }

        int res = w.Reserve;
        if (s.reserveText != null && res != s.reserve)
        {
            s.reserve = res;
            s.reserveText.text = res < 0 ? "INF" : res.ToString();
        }
    }

    void RefreshFill(Slot s)
    {
        if (s.fill == null) return;
        Weapon w = s.weapon;
        int mag = Mathf.Max(1, w.Data.magazineSize);

        float target = 0f;
        if (w.IsReloading)
        {
            int res = w.Reserve;   // -1 = ไม่จำกัด
            int canLoad = res < 0 ? mag - w.Ammo : Mathf.Min(mag - w.Ammo, res);
            float cur = (float)w.Ammo / mag;
            float full = (float)(w.Ammo + canLoad) / mag;
            target = Mathf.Lerp(cur, full, w.ReloadRatio);
        }

        if (s.fillShown < 0f)
        {
            s.fillShown = 0f;
            s.lastAmmo = w.Ammo;
        }
        s.fillShown = Mathf.MoveTowards(s.fillShown, target, fillSpeed * Time.unscaledDeltaTime);

        // กระสุนลดลง = ยิงไปหนึ่งนัด: หลอดเต็มทันที และจำช่วงห่างระหว่างนัดไว้คำนวณความเร็วลด
        if (w.Ammo < s.lastAmmo)
        {
            float t = Time.time;
            float interval = t - s.lastShotTime;
            s.shotInterval = interval < 1.5f ? interval : 1f / Mathf.Max(0.01f, w.Data.shotsPerSecond);
            s.lastShotTime = t;
            s.pulse = 1f;
        }
        s.lastAmmo = w.Ammo;

        if (s.pulse > 0f)
        {
            float decay = Mathf.Clamp(s.shotInterval * pulseDecayRatio, pulseTimeRange.x, pulseTimeRange.y);
            s.pulse = Mathf.MoveTowards(s.pulse, 0f, Time.unscaledDeltaTime / decay);
        }

        s.fill.fillAmount = Mathf.Max(s.fillShown, s.pulse);
    }

    void RefreshStatus(Slot s)
    {
        Weapon w = s.weapon;
        bool front = s.t >= 1f;   // อยู่ด้านหน้าเต็มแล้ว (ระหว่างเลื่อนยังนับเป็นด้านหลัง)

        // ลำดับความสำคัญ: PositionDim > ModuleDim > ReloadDim แสดงทีละชั้น
        if (s.indicator != null) s.indicator.DimSuppressed = !front;
        bool moduleDim = front && s.indicator != null && s.indicator.WantsDim;

        if (s.reloadDim != null)
        {
            bool on = front && !moduleDim && (w.IsReloading || w.IsOutOfAmmo);
            if (s.reloadDim.gameObject.activeSelf != on) s.reloadDim.gameObject.SetActive(on);
        }
    }
}