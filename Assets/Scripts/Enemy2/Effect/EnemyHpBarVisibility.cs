using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyHpBarVisibility : MonoBehaviour
{
    [SerializeField] WorldBar bar;                 // ลาก object ที่มี WorldBar เข้ามา
    [SerializeField] float showAfterDamage = 2f;   // โชว์หลังโดนดาเมจกี่วิ
    [SerializeField] float hoverLinger = 0.5f;     // ค้างหลังเมาส์ออกกี่วิ
    [SerializeField] float fadeSpeed = 8f;

    Health health;
    SpriteRenderer[] sprites;
    float[] baseAlpha;
    float showUntil;
    float alpha;   // 0..1

    void Awake()
    {
        health = GetComponent<Health>();
        sprites = bar != null ? bar.GetComponentsInChildren<SpriteRenderer>(true)
                              : new SpriteRenderer[0];
        baseAlpha = new float[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) baseAlpha[i] = sprites[i].color.a;
        Apply(0f);
    }

    void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        showUntil = 0f;
        if (sprites != null) Apply(0f);
    }

    void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    void OnDamaged(DamageResult _) => showUntil = Mathf.Max(showUntil, Time.time + showAfterDamage);

    void OnDied() { showUntil = 0f; Apply(0f); }

    // เรียกจาก EnemyHoverDetector ตอนเมาส์ชี้
    public void Hover() => showUntil = Mathf.Max(showUntil, Time.time + hoverLinger);

    void Update()
    {
        float target = (!health.IsDead && Time.time < showUntil) ? 1f : 0f;
        if (Mathf.Approximately(alpha, target)) return;   // ไม่ต้องทำอะไรถ้านิ่งอยู่แล้ว
        Apply(Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime));
    }

    void Apply(float a)
    {
        alpha = a;
        for (int i = 0; i < sprites.Length; i++)
        {
            Color c = sprites[i].color;
            c.a = baseAlpha[i] * a;
            sprites[i].color = c;
        }
    }
}