using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(StatusController))]
public class PlayerDeath : MonoBehaviour
{
    [SerializeField] StatusData deadStatus;          // ห้าม Move + Attack + Skills ตลอดไป
    [SerializeField] SpriteRenderer body;            // sprite ของหุ่น (ลูกที่จะสั่น/ซ่อน)
    [SerializeField] GameObject explosionPrefab;
    [SerializeField] float dyingDuration = 1.2f;
    [SerializeField] float shakeStart = 0.05f;       // ความแรงสั่นตอนเริ่ม -> ตอนท้าย
    [SerializeField] float shakeEnd = 0.3f;
    [SerializeField] float explosionInterval = 0.07f;
    [SerializeField] float explosionSpread = 1.2f;   // รัศมีที่ระเบิดเล็กสุ่มขึ้นรอบตัว
    [SerializeField] float finalBlastScale = 6f;

    Health health;
    StatusController status;

    public float DyingDuration => dyingDuration;

    void Awake()
    {
        health = GetComponent<Health>();
        status = GetComponent<StatusController>();
    }

    void OnEnable() => health.Died += OnDied;
    void OnDisable() => health.Died -= OnDied;

    void OnDied() => StartCoroutine(Dying());

    IEnumerator Dying()
    {
        if (deadStatus != null) status.Apply(deadStatus, gameObject, 9999f);   // ยกเลิกการควบคุมทั้งหมด

        Transform t = body != null ? body.transform : null;
        Vector3 basePos = t != null ? t.localPosition : Vector3.zero;

        const float finalDelay = 0.15f;                       // ระเบิดใหญ่ก่อนหยุดเกมเล็กน้อย จะได้ค้างอยู่บนหน้าจอ
        float shakeTime = dyingDuration - finalDelay;
        float elapsed = 0f;
        float nextBlast = 0f;

        while (elapsed < shakeTime)
        {
            float k = elapsed / shakeTime;

            if (t != null)
                t.localPosition = basePos + (Vector3)(Random.insideUnitCircle * Mathf.Lerp(shakeStart, shakeEnd, k));

            if (elapsed >= nextBlast)
            {
                nextBlast = elapsed + explosionInterval;
                SpawnBlast((Vector2)transform.position + Random.insideUnitCircle * explosionSpread, Random.Range(1f, 2.5f));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        SpawnBlast(transform.position, finalBlastScale);
        if (body != null)
        {
            body.enabled = false;              // หุ่นแตกหายไป
            body.transform.localPosition = basePos;
        }
    }

    void SpawnBlast(Vector2 pos, float scale)
    {
        if (explosionPrefab == null) return;
        GameObject fx = Instantiate(explosionPrefab, pos, Quaternion.identity);
        fx.transform.localScale = Vector3.one * scale;
        Destroy(fx, 0.3f);
    }
}