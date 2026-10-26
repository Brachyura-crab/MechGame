using UnityEngine;

// คุมความมืดของ 5 ส่วนหน้าจอ ตามระดับความเสียหายของโมดูลที่ผูกไว้
// alpha 0 = มองเห็นปกติ, 1 = มืดสนิท
public class ScreenZoneBlackout : MonoBehaviour
{
    [System.Serializable]
    public class Zone
    {
        public string name;          // ไว้ดูใน Inspector เท่านั้น เช่น Top, Center
        public CanvasGroup group;    // CanvasGroup ของรูปสีดำส่วนนั้น
        public ModuleData module;    // โมดูลที่ถ้าเสียแล้วส่วนนี้จะมืด

        [System.NonSerialized] public ModuleDamage lastTier;
        [System.NonSerialized] public float current;
        [System.NonSerialized] public float flickerEnd;
        [System.NonSerialized] public float nextFlicker;
        [System.NonSerialized] public float bootCover;   // ความมืดที่ BootSequence สั่ง (0 = ไม่มีผล)
    }

    [SerializeField] ModuleSystem modules;
    [SerializeField] Zone[] zones;

    [Header("ความมืดค้างตามระดับ")]
    [SerializeField, Range(0f, 1f)] float lightAlpha = 0f;
    [SerializeField, Range(0f, 1f)] float mediumAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] float heavyAlpha = 1f;

    [Header("การเปลี่ยน")]
    [SerializeField] float darkenSpeed = 6f;     // ความเร็วตอนมืดลง (alpha ต่อวินาที)
    [SerializeField] float brightenSpeed = 1.5f; // ความเร็วตอนสว่างกลับหลังซ่อม

    [Header("กะพริบ")]
    [SerializeField] float hitFlickerTime = 0.4f;                       // กะพริบทันทีที่โมดูลแย่ลง
    [SerializeField] Vector2 lightFlickerInterval = new Vector2(1f, 3f); // ช่วงห่างของกะพริบสุ่ม (ระดับ Light)
    [SerializeField] float lightFlickerTime = 0.25f;
    [SerializeField] float flickerFrequency = 30f;

    void Awake()
    {
        foreach (Zone z in zones)
        {
            if (z.group == null) continue;
            z.group.interactable = false;
            z.group.blocksRaycasts = false;
            z.group.alpha = 0f;
        }
    }

    void OnEnable()
    {
        modules.Changed += OnModuleChanged;
        foreach (Zone z in zones)
        {
            z.lastTier = modules.Tier(z.module);
            z.current = BaseAlpha(z.lastTier);
            z.nextFlicker = Time.time + Random.Range(lightFlickerInterval.x, lightFlickerInterval.y);
        }
    }

    void OnDisable() => modules.Changed -= OnModuleChanged;

    void OnModuleChanged(ModuleData m, ModuleDamage tier)
    {
        foreach (Zone z in zones)
        {
            if (z.module != m) continue;
            if (tier > z.lastTier) z.flickerEnd = Time.time + hitFlickerTime;   // แย่ลง = กะพริบทันที
            z.lastTier = tier;
        }
    }

    float BaseAlpha(ModuleDamage t)
    {
        switch (t)
        {
            case ModuleDamage.Light: return lightAlpha;
            case ModuleDamage.Medium: return mediumAlpha;
            case ModuleDamage.Heavy: return heavyAlpha;
            default: return 0f;
        }
    }

    // BootSequence สั่งความมืดของแผ่นที่ใช้ CanvasGroup นี้ (ใช้ร่วมกับความมืดจากโมดูล โดยเอาค่าที่มืดกว่า)
    public void SetCover(CanvasGroup g, float cover)
    {
        if (g == null) return;
        foreach (Zone z in zones)
            if (z.group == g) z.bootCover = cover;
    }

    void Update()
    {
        float now = Time.time;
        for (int i = 0; i < zones.Length; i++)
        {
            Zone z = zones[i];
            if (z.group == null) continue;

            float target = BaseAlpha(z.lastTier);
            float speed = target > z.current ? darkenSpeed : brightenSpeed;
            z.current = Mathf.MoveTowards(z.current, target, speed * Time.deltaTime);

            // Light: กะพริบสุ่มเป็นช่วงสั้นๆ
            if (z.lastTier == ModuleDamage.Light && now >= z.nextFlicker)
            {
                z.flickerEnd = now + lightFlickerTime;
                z.nextFlicker = now + Random.Range(lightFlickerInterval.x, lightFlickerInterval.y);
            }

            float flicker = 0f;
            float noise = Mathf.PerlinNoise(now * flickerFrequency, i * 17.3f);
            if (now < z.flickerEnd) flicker = noise > 0.5f ? 0.9f : 0f;
            else if (z.lastTier == ModuleDamage.Medium) flicker = noise > 0.75f ? 0.3f : 0f;   // Medium: สั่นไหวตลอด

            z.group.alpha = Mathf.Clamp01(Mathf.Max(z.current, flicker, z.bootCover));
        }
    }
}