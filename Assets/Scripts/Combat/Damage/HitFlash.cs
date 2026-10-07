using UnityEngine;

// วาบสีขาวบนตัวละครเมื่อโดนดาเมจ (ศัตรูและผู้เล่นใช้ component เดียวกัน)
// สร้างสำเนา sprite ซ้อนทับ แล้วไล่ความทึบลง ไม่แตะ material เดิมของตัวละคร
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] SpriteRenderer[] targets;   // ชิ้นที่ต้องการให้วาบ (ลำตัว แขน ฯลฯ)
    [SerializeField] Material flashMaterial;     // material สีขาวแบนแบบ Unlit (ดูวิธีทำด้านล่าง)
    [SerializeField] float duration = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] float peakAlpha = 0.85f;
    [Range(0f, 1f)]
    [SerializeField] float restartAt = 0.5f;     // วาบใหม่ได้เมื่อวาบเดิมเหลือไม่เกินสัดส่วนนี้ (กันกะพริบรัวตอนยิงถี่)

    Health health;
    SpriteRenderer[] overlays;
    float timer;

    void Awake()
    {
        health = GetComponent<Health>();

        if (flashMaterial == null || targets == null || targets.Length == 0)
        {
            Debug.LogWarning("HitFlash: ยังไม่ได้ตั้ง Targets หรือ Flash Material", this);
            enabled = false;
            return;
        }

        overlays = new SpriteRenderer[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            GameObject go = new GameObject("HitFlashOverlay");
            go.transform.SetParent(targets[i].transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = flashMaterial;
            sr.sortingLayerID = targets[i].sortingLayerID;
            sr.enabled = false;
            overlays[i] = sr;
        }
    }

    void OnEnable()
    {
        if (health != null) health.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        if (health != null) health.Damaged -= OnDamaged;
        timer = 0f;
        HideAll();
    }

    void OnDamaged(DamageResult r)
    {
        if (timer > duration * restartAt) return;   // กำลังวาบแรงอยู่ ไม่เริ่มซ้ำ
        timer = duration;
    }

    void LateUpdate()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;
        float a = timer > 0f ? peakAlpha * (timer / duration) : 0f;

        for (int i = 0; i < overlays.Length; i++)
        {
            SpriteRenderer ov = overlays[i];
            SpriteRenderer src = targets[i];
            if (ov == null) continue;

            bool on = a > 0f && src != null && src.enabled && src.sprite != null;
            ov.enabled = on;
            if (!on) continue;

            // sprite ของลำตัว/แขนเปลี่ยนตามทิศและท่าทุกเฟรม จึงต้องก๊อปปี้ตามตลอดช่วงวาบ
            ov.sprite = src.sprite;
            ov.flipX = src.flipX;
            ov.flipY = src.flipY;
            ov.sortingOrder = src.sortingOrder + 1;
            ov.color = new Color(1f, 1f, 1f, a);
        }
    }

    void HideAll()
    {
        if (overlays == null) return;
        foreach (SpriteRenderer ov in overlays)
            if (ov != null) ov.enabled = false;
    }
}