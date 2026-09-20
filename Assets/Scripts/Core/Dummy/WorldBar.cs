using UnityEngine;

public class WorldBar : MonoBehaviour
{
    [SerializeField] Health health;
    [SerializeField] Transform hpFill;
    [SerializeField] Transform shieldFill;
    [SerializeField] float width = 1.2f;
    [SerializeField] float hpHeight = 0.15f;
    [SerializeField] float shieldHeight = 0.06f;

    bool started;

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

    void OnHp(float cur, float max) => SetFill(hpFill, cur, max, hpHeight, 0f);

    // โล่เป็นแถบบางวางทับด้านบนของแถบเลือด
    void OnShield(float cur, float max) =>
        SetFill(shieldFill, cur, max, shieldHeight, hpHeight * 0.5f - shieldHeight * 0.5f);

    void SetFill(Transform fill, float cur, float max, float height, float y)
    {
        float ratio = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
        fill.localScale = new Vector3(width * ratio, height, 1f);
        fill.localPosition = new Vector3(-width * 0.5f + width * ratio * 0.5f, y, 0f);
    }
}