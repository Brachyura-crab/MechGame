using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UltimateGauge : MonoBehaviour
{
    [Header("Gauge")]
    [SerializeField] float max = 100f;
    [SerializeField] float passiveRate = 1f;         // เติมเองต่อวินาที
    [SerializeField] float defaultKillCharge = 1f;   // ใช้เมื่อเป้าที่ตายไม่มี Tier

    [Header("Ultimate")]
    [SerializeField] InputActionReference input;     // Skill3 (R)
    [SerializeField] StatusData ultimateStatus;
    [SerializeField] float duration = 10f;

    [Header("Activation FX")]
    [SerializeField] DashShockwave shockwave;
    [SerializeField] float shakeAmplitude = 0.6f;
    [SerializeField] float shakeDuration = 0.5f;

    [SerializeField] UltimateSequence sequence;

    [Header("Debug")]
    [SerializeField] bool debugGui = true;
    [SerializeField] Vector2 debugGuiPos = new Vector2(20f, 170f);

    StatusController status;
    float activeEnd;

    public bool IsCasting { get; private set; }
    public float Current { get; private set; }
    public float Max => max;
    public float Ratio => max > 0f ? Current / max : 0f;
    public bool IsFull => Current >= max;
    // ดูจากสถานะที่ติดอยู่จริง (ถ้าไม่ได้ตั้ง ultimateStatus ใช้ตัวจับเวลาเดิมแทน)
    bool HasStatus => status != null && ultimateStatus != null;
    public bool IsActive => HasStatus ? status.Has(ultimateStatus) : Time.time < activeEnd;
    public float ActiveRemaining => HasStatus ? status.RemainingOf(ultimateStatus) : Mathf.Max(0f, activeEnd - Time.time);
    public float Duration => duration;

    public event Action<float, float> Changed;   // current, max
    public event Action Activated;
    

    void Awake()
    {
        status = GetComponent<StatusController>();
        if (sequence == null) sequence = GetComponent<UltimateSequence>();
    }

    void OnEnable()
    {
        input.action.Enable();
        Health.AnyDied += OnAnyDied;
    }

    void OnDisable() => Health.AnyDied -= OnAnyDied;

    void Update()
    {
        if (PauseMenu.InputBlocked) return;
        AddCharge(passiveRate * Time.deltaTime);

        bool canCast = status == null || status.CanCastSkills;
        if (canCast && IsFull && !IsActive && !IsCasting && input.action.WasPressedThisFrame())
            Activate();
    }

    // ไม่เติมระหว่างอัลติทำงาน
    public void AddCharge(float amount)
    {
        if (amount <= 0f || IsActive || IsCasting) return;

        float before = Current;
        Current = Mathf.Min(max, Current + amount);
        if (Current != before) Changed?.Invoke(Current, max);
    }

    void Activate()
    {
        Current = 0f;
        Changed?.Invoke(Current, max);
        if (sequence != null)
        {
            IsCasting = true;   
            sequence.Play(completed =>
            {
                IsCasting = false;
                if (completed) BeginUltimate();
            });
        }
        else BeginUltimate();
    }

    void BeginUltimate()
    {
        activeEnd = Time.time + duration;
        if (status != null) status.Apply(ultimateStatus, gameObject, duration);
        if (shockwave != null) shockwave.PushUltimate();
        if (CameraShake.Instance != null) CameraShake.Instance.Kick(shakeAmplitude, shakeDuration);
        Activated?.Invoke();
    }

    // นับเฉพาะที่ผู้เล่นเป็นคนฆ่า (ระเบิด missile ก็ส่ง source เป็นผู้เล่น)
    void OnAnyDied(Health victim, DamageResult r)
    {
        if (r.source != gameObject) return;

        StatusController sc = victim.GetComponent<StatusController>();
        TierData tier = sc != null ? sc.Tier : null;
        AddCharge(tier != null ? tier.ultChargeOnKill : defaultKillCharge);
    }

    void OnGUI()
    {
        if (!debugGui || !DebugGate.Visible) return;

        string s = IsActive
            ? "ULT ACTIVE  " + ActiveRemaining.ToString("F1") + "s"
            : IsFull ? "ULT READY  (R)" : "ULT  " + Mathf.FloorToInt(Ratio * 100f) + "%";
        DebugOverlay.Label(s);
    }
}