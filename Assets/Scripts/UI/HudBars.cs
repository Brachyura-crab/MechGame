using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HudBars : MonoBehaviour
{
    [Header("Source")]
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
    bool started;

    void OnEnable()
    {
        health.Changed += OnHealth;
        energy.Changed += OnEnergy;
        health.ShieldChanged += OnShield;
        if (started) Refresh();
    }

    void OnDisable()
    {
        health.Changed -= OnHealth;
        energy.Changed -= OnEnergy;
        health.ShieldChanged -= OnShield;
    }

    void Start()
    {
        started = true;
        Refresh();
    }

    void Refresh()
    {
        OnHealth(health.Current, health.Max);
        OnEnergy(energy.Current, energy.Max);
        OnShield(health.Shield, health.MaxShield);
    }

    void OnHealth(float cur, float max) => Show(hpFill, hpText, cur, max);
    void OnEnergy(float cur, float max) => Show(energyFill, energyText, cur, max);
    void OnShield(float cur, float max) => Show(shieldFill, shieldText, cur, max);

    void Show(Image fill, TMP_Text text, float cur, float max)
    {
        if (fill != null) fill.fillAmount = max > 0f ? cur / max : 0f;
        if (text != null) text.text = Mathf.CeilToInt(cur) + " / " + Mathf.CeilToInt(max);
    }
}