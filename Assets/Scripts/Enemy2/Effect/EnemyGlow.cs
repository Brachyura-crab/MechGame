using UnityEngine;

// เปิด/ปิดชิ้นส่วนเรืองแสง (material additive) แบบค่อย ๆ จาง
// ปิดเมื่อ: ตาย, Frozen (swarm หลับ), หรือหยุดนิ่งนานเกิน idleDelay
[DisallowMultipleComponent]
public class EnemyGlow : MonoBehaviour
{
    [SerializeField] SpriteRenderer[] glowParts;

    [Header("Fade")]
    [SerializeField] float fadeInSpeed = 4f;     // alpha/วินาที ตอนเปิด
    [SerializeField] float fadeOutSpeed = 3f;    // ตอนปิด (ตอนตายจะเร็วกว่า 3 เท่า)

    [Header("Idle")]
    [SerializeField] bool offWhenIdle = true;
    [SerializeField] float idleSpeed = 0.3f;     // ช้ากว่านี้ถือว่าหยุด
    [SerializeField] float idleDelay = 0.4f;     // หยุดนานเท่านี้ถึงดับ (กันกะพริบตอนชะงักสั้น ๆ)

    Rigidbody2D rb;
    Health health;
    IEnemyState behavior;
    float[] baseAlpha;
    float level;       // 0..1 ความสว่างปัจจุบัน
    float idleTimer;
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInParent<Health>();
        TryGetComponent(out behavior);

        baseAlpha = new float[glowParts.Length];
        for (int i = 0; i < glowParts.Length; i++)
            baseAlpha[i] = glowParts[i] != null ? glowParts[i].color.a : 1f;
    }

    void OnEnable()
    {
        dead = false;
        idleTimer = 0f;
        level = 0f;
        if (health != null) health.Died += OnDied;
        Apply();
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
    }

    void OnDied() => dead = true;

    void Update()
    {
        bool on = !dead && !(health != null && health.IsDead);

        if (on && behavior != null && behavior.Frozen) on = false;

        if (on && offWhenIdle && rb != null)
        {
            if (rb.linearVelocity.sqrMagnitude < idleSpeed * idleSpeed) idleTimer += Time.deltaTime;
            else idleTimer = 0f;
            if (idleTimer >= idleDelay) on = false;
        }

        float speed = on ? fadeInSpeed : (dead ? fadeOutSpeed * 3f : fadeOutSpeed);
        level = Mathf.MoveTowards(level, on ? 1f : 0f, speed * Time.deltaTime);
        Apply();
    }

    void Apply()
    {
        for (int i = 0; i < glowParts.Length; i++)
        {
            SpriteRenderer sr = glowParts[i];
            if (sr == null) continue;

            sr.enabled = level > 0.001f;   // ดับสนิทแล้วไม่ต้องวาด
            Color c = sr.color;
            c.a = baseAlpha[i] * level;
            sr.color = c;
        }
    }
}