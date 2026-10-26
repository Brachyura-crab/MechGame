using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HudBars : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] bool levelColors = false;   // เปิดแล้วแถบเปลี่ยนสีตามระดับ (ใช้กับแถบในหน้า SYS)
    [SerializeField] Health health;
    [SerializeField] Energy energy;

    [Header("HP bar")]
    [SerializeField] Image hpFill;
    [SerializeField] TMP_Text hpText;

    [Header("Energy bar")]
    [SerializeField] Image energyFill;
    [SerializeField] TMP_Text energyText;

    [Header("Shield bar (วางทับ HP)")]
    [SerializeField] Image shieldFill;
    [SerializeField] TMP_Text shieldText; // เว้นว่างได้ ถ้าไม่อยากแสดงตัวเลขโล่

    [Header("สั่น (ว่างได้) ใส่ RectTransform ของกลุ่มที่อยากให้สั่น")]
    [SerializeField] RectTransform hpShakeTarget;       // HP กับโล่อยู่กลุ่มเดียวกัน ใส่ตัวเดียวกันทั้งสองช่องได้
    [SerializeField] RectTransform shieldShakeTarget;
    [SerializeField] RectTransform energyShakeTarget;   // ห้ามซ้ำกับของ HP/โล่
    [SerializeField] float hitShakeAmplitude = 8f;      // พิกเซล สั่นครั้งเดียวตอนเสีย HP/โล่
    [SerializeField] float hitShakeTime = 0.3f;         // วินาทีที่ใช้จางหมด
    [SerializeField] float energyShakeAmplitude = 3f;   // พิกเซล สั่นต่อเนื่องตอนใช้พลังงาน
    [SerializeField] float energyShakeHold = 0.15f;     // ยังนับว่ากำลังใช้พลังงานอีกกี่วินาทีหลังค่าหยุดลด
    [SerializeField] float shakeFrequency = 40f;

    [Header("Flash ขาวตอนเสีย HP / โล่")]
    [SerializeField] float flashTime = 0.2f;

    bool started;

    UiShake hpShake, shieldShake, energyShake;
    ImageFlash hpFlash, shieldFlash;
    float lastEnergy = -1f;
    float lastEnergyUse = -999f;

    void OnEnable()
    {
        health.Changed += OnHealth;
        energy.Changed += OnEnergy;
        health.ShieldChanged += OnShield;
        health.Damaged += OnDamaged;
        if (started) Refresh();
    }

    void OnDisable()
    {
        health.Changed -= OnHealth;
        energy.Changed -= OnEnergy;
        health.ShieldChanged -= OnShield;
        health.Damaged -= OnDamaged;

        // คืนตำแหน่งและสีที่ถูกสั่น/แฟลชค้างไว้
        hpShake?.Reset();
        shieldShake?.Reset();
        energyShake?.Reset();
        hpFlash?.Reset();
        shieldFlash?.Reset();
    }

    void Start()
    {
        started = true;

        hpShake = hpShakeTarget != null ? new UiShake(hpShakeTarget) : null;
        shieldShake = shieldShakeTarget == null ? null
                    : (shieldShakeTarget == hpShakeTarget ? hpShake : new UiShake(shieldShakeTarget));
        energyShake = energyShakeTarget != null ? new UiShake(energyShakeTarget) : null;
        hpFlash = new ImageFlash(hpFill);
        shieldFlash = new ImageFlash(shieldFill);

        Refresh();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float decay = hitShakeAmplitude / Mathf.Max(0.01f, hitShakeTime);   // ความแรงที่ลดต่อวินาที

        hpShake?.Tick(dt, decay, 0f, shakeFrequency);
        if (shieldShake != null && shieldShake != hpShake) shieldShake.Tick(dt, decay, 0f, shakeFrequency);

        bool usingEnergy = Time.unscaledTime - lastEnergyUse < energyShakeHold;
        energyShake?.Tick(dt, decay, usingEnergy ? energyShakeAmplitude : 0f, shakeFrequency);

        hpFlash?.Tick(dt, flashTime);
        shieldFlash?.Tick(dt, flashTime);
    }

    void Refresh()
    {
        OnHealth(health.Current, health.Max);
        OnEnergy(energy.Current, energy.Max);
        OnShield(health.Shield, health.MaxShield);
    }

    void OnHealth(float cur, float max) => Show(hpFill, hpText, cur, max);
    void OnShield(float cur, float max) => Show(shieldFill, shieldText, cur, max);

    void OnEnergy(float cur, float max)
    {
        if (lastEnergy >= 0f && cur < lastEnergy - 0.0001f) lastEnergyUse = Time.unscaledTime;   // ค่าลด = กำลังใช้
        lastEnergy = cur;
        Show(energyFill, energyText, cur, max);
    }

    // เสีย HP หรือโล่ (แยกกันตามที่ดาเมจเข้าจริง): สั่นครั้งเดียว + แฟลชขาว
    void OnDamaged(DamageResult r)
    {
        if (r.hpDamage > 0f)
        {
            hpShake?.Burst(hitShakeAmplitude);
            hpFlash?.Trigger();
        }
        if (r.shieldDamage > 0f)
        {
            shieldShake?.Burst(hitShakeAmplitude);
            shieldFlash?.Trigger();
        }
    }

    void Show(Image fill, TMP_Text text, float cur, float max)
    {
        if (fill != null)
        {
            float ratio = max > 0f ? cur / max : 0f;
            fill.fillAmount = ratio;
            if (levelColors) fill.color = UiThemeSource.LevelColor(ratio);
        }
        if (text != null) text.text = Mathf.CeilToInt(cur) + " / " + Mathf.CeilToInt(max);
    }

    // สั่นตำแหน่ง UI: Burst = สั่นครั้งเดียวแล้วจาง, sustain = สั่นค้างต่อเนื่อง
    class UiShake
    {
        readonly RectTransform rt;
        readonly Vector2 basePos;
        readonly float seed;
        float burst;

        public UiShake(RectTransform target)
        {
            rt = target;
            basePos = target.anchoredPosition;
            seed = Random.Range(0f, 100f);
        }

        public void Burst(float amplitude) => burst = Mathf.Max(burst, amplitude);

        public void Tick(float dt, float burstDecayPerSec, float sustain, float frequency)
        {
            burst = Mathf.MoveTowards(burst, 0f, burstDecayPerSec * dt);
            float strength = burst + sustain;
            Vector2 offset = Vector2.zero;
            if (strength > 0.001f)
            {
                float t = Time.unscaledTime * frequency;
                float x = (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f;
                offset = new Vector2(x, y) * strength;
            }
            rt.anchoredPosition = basePos + offset;
        }

        public void Reset()
        {
            burst = 0f;
            rt.anchoredPosition = basePos;
        }
    }

    // แฟลชสีรูปไปทางขาวแล้วคืนสีเดิม (เก็บสีเดิมตอนเริ่มแฟลชครั้งแรก ยิงซ้ำระหว่างแฟลชไม่ทับสีเดิม)
    class ImageFlash
    {
        readonly Image img;
        Color baseColor;
        float t;

        public ImageFlash(Image image) => img = image;

        public void Trigger()
        {
            if (img == null) return;
            if (t <= 0f) baseColor = img.color;
            t = 1f;
        }

        public void Tick(float dt, float duration)
        {
            if (img == null || t <= 0f) return;
            t -= dt / Mathf.Max(0.01f, duration);
            if (t <= 0f)
            {
                t = 0f;
                img.color = baseColor;
                return;
            }
            Color c = Color.Lerp(baseColor, Color.white, t);
            c.a = baseColor.a;
            img.color = c;
        }

        public void Reset()
        {
            if (img == null || t <= 0f) return;
            t = 0f;
            img.color = baseColor;
        }
    }
}