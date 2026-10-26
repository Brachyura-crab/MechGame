using UnityEngine;

// หลอดเลือด/โล่เหนือหัวศัตรู
// ขนาดและตำแหน่ง "เต็มหลอด" (HP 100%) อ่านจาก Transform ของ Fill ที่ตั้งไว้ใน Inspector
// สคริปต์ปรับเฉพาะแกน X ตามเปอร์เซ็นต์ (ชิดซ้าย) แกน Y/Z คงตามที่ตั้งไว้
public class WorldBar : MonoBehaviour
{
    [SerializeField] Health health;
    [SerializeField] Transform hpFill;
    [SerializeField] Transform shieldFill;

    Vector3 hpScale, hpPos;
    Vector3 shieldScale, shieldPos;
    bool started;

    void Awake()
    {
        if (hpFill != null) { hpScale = hpFill.localScale; hpPos = hpFill.localPosition; }
        if (shieldFill != null) { shieldScale = shieldFill.localScale; shieldPos = shieldFill.localPosition; }
    }

    void OnEnable()
    {
        health.Changed += OnHp;
        health.ShieldChanged += OnShield;
        if (started) Refresh();
    }

    void OnDisable()
    {
        health.Changed -= OnHp;
        health.ShieldChanged -= OnShield;
    }

    void Start()
    {
        started = true;
        Refresh();
    }

    void Refresh()
    {
        OnHp(health.Current, health.Max);
        OnShield(health.Shield, health.MaxShield);
    }

    void OnHp(float cur, float max) => SetFill(hpFill, cur, max, hpScale, hpPos);
    void OnShield(float cur, float max) => SetFill(shieldFill, cur, max, shieldScale, shieldPos);

    static void SetFill(Transform fill, float cur, float max, Vector3 fullScale, Vector3 fullPos)
    {
        if (fill == null) return;

        float ratio = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
        float w = fullScale.x * ratio;

        fill.localScale = new Vector3(w, fullScale.y, fullScale.z);
        fill.localPosition = new Vector3(fullPos.x - fullScale.x * 0.5f + w * 0.5f, fullPos.y, fullPos.z);
    }
}