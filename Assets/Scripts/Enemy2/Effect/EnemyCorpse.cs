using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyCorpse : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] SpriteRenderer[] sprites;          // ว่างไว้ = หาจากลูกทั้งหมดให้เอง
    [SerializeField] Color corpseTint = new Color(0.25f, 0.25f, 0.28f, 1f); // สีที่คูณทับตอนเป็นศพ
    [SerializeField] int sortingOrderDrop = 10;         // ลด sorting order ให้อยู่ใต้ตัวที่ยังมีชีวิต

    [Header("Timing (วินาที)")]
    [SerializeField] float darkenTime = 0.25f;          // เวลาไล่สีมืดลง
    [SerializeField] float lingerTime = 3f;             // ค้างเป็นศพก่อนเริ่มจาง
    [SerializeField] float fadeTime = 1.5f;             // เวลาจางหาย

    [Header("Hide on death")]
    [SerializeField] GameObject[] hideOnDeath;          // เช่น HP bar

    [Header("End")]
    [SerializeField] bool destroyOnEnd = true;          // false = SetActive(false) เผื่อทำ pooling

    Health health;
    Rigidbody2D rb;
    Collider2D[] colliders;
    bool[] colliderWasEnabled;
    Color[] originalColors;
    int[] originalOrders;
    Behaviour brain;
    EnemyAttack attack;
    Coroutine routine;

    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        brain = GetComponent<IEnemyState>() as Behaviour;
        attack = GetComponent<EnemyAttack>();

        if (sprites == null || sprites.Length == 0)
            sprites = GetComponentsInChildren<SpriteRenderer>(true);

        colliders = GetComponentsInChildren<Collider2D>(true);
        colliderWasEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderWasEnabled[i] = colliders[i].enabled;

        originalColors = new Color[sprites.Length];
        originalOrders = new int[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            originalColors[i] = sprites[i].color;
            originalOrders[i] = sprites[i].sortingOrder;
        }
    }

    void OnEnable()
    {
        health.Died += OnDied;
        Restore();   // กรณี pooling: เปิดกลับมาต้องเป็นตัวปกติ
    }

    void OnDisable()
    {
        health.Died -= OnDied;
        if (routine != null) { StopCoroutine(routine); routine = null; }
    }

    void OnDied()
    {
        if (routine != null) return;

        // 1) เลิกบล็อกทุกอย่าง
        foreach (Collider2D c in colliders) if (c != null) c.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // 2) หยุด AI / โจมตี (ปิด component → OnDisable ถอดออกจาก EnemyBehavior.All)
        if (brain != null) brain.enabled = false;
        if (attack != null) attack.enabled = false;

        // 3) ซ่อน HP bar ฯลฯ
        foreach (GameObject g in hideOnDeath) if (g != null) g.SetActive(false);

        // 4) ให้ศพอยู่ใต้ตัวที่ยังเป็น
        foreach (SpriteRenderer s in sprites) if (s != null) s.sortingOrder -= sortingOrderDrop;

        routine = StartCoroutine(CorpseRoutine());
    }

    IEnumerator CorpseRoutine()
    {
        // มืดลง
        for (float t = 0f; t < darkenTime; t += Time.deltaTime)
        {
            float k = darkenTime > 0f ? t / darkenTime : 1f;
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null)
                    sprites[i].color = Color.Lerp(originalColors[i], Tint(originalColors[i]), k);
            yield return null;
        }
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = Tint(originalColors[i]);

        // ค้างเป็นศพ
        yield return new WaitForSeconds(lingerTime);

        // จางหาย
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            float a = 1f - (fadeTime > 0f ? t / fadeTime : 1f);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color c = Tint(originalColors[i]);
                c.a = originalColors[i].a * a;
                sprites[i].color = c;
            }
            yield return null;
        }

        routine = null;
        if (destroyOnEnd) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    Color Tint(Color c) => new Color(c.r * corpseTint.r, c.g * corpseTint.g, c.b * corpseTint.b, c.a);

    void Restore()
    {
        if (colliders == null) return;   // ยังไม่ผ่าน Awake
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = colliderWasEnabled[i];
        if (rb != null) rb.simulated = true;
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            sprites[i].color = originalColors[i];
            sprites[i].sortingOrder = originalOrders[i];
        }
        foreach (GameObject g in hideOnDeath) if (g != null) g.SetActive(true);
        if (brain != null) brain.enabled = true;
        if (attack != null) attack.enabled = true;
    }
}