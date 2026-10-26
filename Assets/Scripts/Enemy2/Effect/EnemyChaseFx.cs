using UnityEngine;

public enum ChasePhase { Idle, Windup, Chase }

// effect ของ swarm ตอนจะพุ่งไล่:
//  Windup : ลำตัวสั่น (แรงขึ้นเรื่อย ๆ) + หนวดขดตามความคืบหน้า
//  Chase  : sprite additive ตัวแรกวาบครั้งเดียวตอนเริ่ม, ตัวที่สองใหญ่กว่ากระพริบช้า ๆ ตลอดการไล่
// EnemyBehavior เป็นฝ่ายเรียก SetPhase เมื่อ state เปลี่ยน (ทางเดียว Behavior → Fx)
[DisallowMultipleComponent]
public class EnemyChaseFx : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform visual;               // ตัวที่ถูกสั่น (ลูกที่เก็บลำตัวและหนวด)
    [SerializeField] TentacleController tentacles;   // ถ้ามี จะขดตอน windup และสะบัดตอนเริ่ม chase

    [Header("Windup shake")]
    [SerializeField] float shakeAmp = 0.06f;         // ระยะสั่นสูงสุด (หน่วยโลก)
    [SerializeField] float shakeFreq = 35f;          // ความถี่การสั่น
    [SerializeField, Range(0f, 1f)] float shakeStart = 0.3f;  // ความแรงตอนเริ่ม windup (ไปถึง 1 ตอนจบ)
    [SerializeField] float defaultWindup = 0.5f;     // ใช้เมื่อ Behavior ไม่ส่งเวลา windup มา

    [Header("Start burst (วาบตอนเริ่ม chase)")]
    [SerializeField] SpriteRenderer startFx;
    [SerializeField] float startDuration = 0.3f;
    [SerializeField] float startScaleFrom = 0.6f;    // คูณขนาดเดิมของ sprite
    [SerializeField] float startScaleTo = 1.6f;

    [Header("Loop glow (กระพริบช้า ๆ ตลอดการไล่)")]
    [SerializeField] SpriteRenderer loopFx;
    [SerializeField] float pulseFreq = 0.8f;         // รอบต่อวินาที
    [SerializeField, Range(0f, 1f)] float pulseMin = 0.35f;
    [SerializeField, Range(0f, 1f)] float pulseMax = 1f;
    [SerializeField] float pulseScale = 0.1f;        // ขนาดแกว่ง ± เท่านี้
    [SerializeField] float loopFadeIn = 6f;
    [SerializeField] float loopFadeOut = 4f;

    public ChasePhase Phase => phase;

    ChasePhase phase;
    Health health;
    IEnemyState behavior;
    bool dead;

    Vector3 visualBasePos;
    float seed;
    float time;

    float windupT, windupDur;
    float shakeLevel;

    float startT = -1f;
    float startBaseAlpha, loopBaseAlpha;
    Vector3 startBaseScale, loopBaseScale;
    float loopLevel;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInParent<Health>();
        TryGetComponent(out behavior);
        if (visual == null) visual = transform;
        visualBasePos = visual.localPosition;
        seed = Random.value * 100f;

        if (startFx != null) { startBaseAlpha = startFx.color.a; startBaseScale = startFx.transform.localScale; }
        if (loopFx != null) { loopBaseAlpha = loopFx.color.a; loopBaseScale = loopFx.transform.localScale; }
    }

    void OnEnable()
    {
        dead = false;
        phase = ChasePhase.Idle;
        windupT = 0f; shakeLevel = 0f; startT = -1f; loopLevel = 0f;
        visual.localPosition = visualBasePos;
        if (startFx != null) startFx.enabled = false;
        if (loopFx != null) loopFx.enabled = false;
        if (health != null) health.Died += OnDied;
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
        if (visual != null) visual.localPosition = visualBasePos;
    }

    void OnDied()
    {
        dead = true;
        phase = ChasePhase.Idle;
        startT = -1f;
        if (startFx != null) startFx.enabled = false;
        visual.localPosition = visualBasePos;
        shakeLevel = 0f;
    }

    // เรียกจาก EnemyBehavior ทุกครั้งที่ state เปลี่ยน (windupDuration ใช้เฉพาะ Windup)
    public void SetPhase(ChasePhase p, float windupDuration = 0f)
    {
        if (dead || p == phase) return;
        ChasePhase prev = phase;
        phase = p;

        if (p == ChasePhase.Windup)
        {
            windupT = 0f;
            windupDur = windupDuration > 0.01f ? windupDuration : defaultWindup;
        }
        else if (p == ChasePhase.Chase)
        {
            startT = 0f;                          // เริ่ม burst
            if (tentacles != null) tentacles.Strike();
        }
        else if (prev == ChasePhase.Windup && tentacles != null)
        {
            tentacles.SetCoil(0f);                // windup ถูกยกเลิก (เช่นโดน stagger)
        }
    }

    void Update()
    {
        if (!dead && health != null && health.IsDead) OnDied();

        if (behavior != null && !dead)
        {
            ChasePhase want = ChasePhase.Idle;
            if (!behavior.Frozen)
            {
                if (behavior.IsWindingUp) want = ChasePhase.Windup;
                else if (behavior.IsChasing) want = ChasePhase.Chase;
            }
            SetPhase(want);
        }

        float dt = Time.deltaTime;
        time += dt;

        UpdateShake(dt);
        UpdateStart(dt);
        UpdateLoop(dt);
    }

    void UpdateShake(float dt)
    {
        float target = 0f;
        if (phase == ChasePhase.Windup)
        {
            windupT += dt;
            float r = behavior != null ? behavior.WindupRatio : Mathf.Clamp01(windupT / windupDur);
            target = Mathf.Lerp(shakeStart, 1f, r);
            if (tentacles != null) tentacles.SetCoil(r);
        }

        shakeLevel = Mathf.MoveTowards(shakeLevel, target, dt * 20f);

        if (shakeLevel > 0.001f)
        {
            float t = time * shakeFreq;
            float x = Mathf.PerlinNoise(seed, t) - 0.5f;
            float y = Mathf.PerlinNoise(seed + 50f, t) - 0.5f;
            visual.localPosition = visualBasePos + new Vector3(x, y, 0f) * (2f * shakeAmp * shakeLevel);
        }
        else
        {
            visual.localPosition = visualBasePos;
        }
    }

    void UpdateStart(float dt)
    {
        if (startFx == null) return;
        if (startT < 0f) { startFx.enabled = false; return; }

        float t = startT / Mathf.Max(0.01f, startDuration);
        if (t >= 1f) { startT = -1f; startFx.enabled = false; return; }
        startT += dt;

        float ease = 1f - (1f - t) * (1f - t);       // ขยายเร็วแล้วช้าลง
        startFx.enabled = true;
        startFx.transform.localScale = startBaseScale * Mathf.Lerp(startScaleFrom, startScaleTo, ease);
        Color c = startFx.color;
        c.a = startBaseAlpha * Mathf.Pow(1f - t, 1.5f);
        startFx.color = c;
    }

    void UpdateLoop(float dt)
    {
        if (loopFx == null) return;

        bool on = phase == ChasePhase.Chase && !dead;
        float speed = on ? loopFadeIn : (dead ? loopFadeOut * 3f : loopFadeOut);
        loopLevel = Mathf.MoveTowards(loopLevel, on ? 1f : 0f, speed * dt);

        if (loopLevel <= 0.001f) { loopFx.enabled = false; return; }

        float pulse = 0.5f + 0.5f * Mathf.Sin(time * pulseFreq * Mathf.PI * 2f);
        loopFx.enabled = true;
        loopFx.transform.localScale = loopBaseScale * (1f + (pulse - 0.5f) * 2f * pulseScale);
        Color c = loopFx.color;
        c.a = loopBaseAlpha * loopLevel * Mathf.Lerp(pulseMin, pulseMax, pulse);
        loopFx.color = c;
    }
}