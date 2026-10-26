using UnityEngine;
using UnityEngine.InputSystem;

public class StatDebug : MonoBehaviour
{
    MechStats mech;
    Health health;
    [SerializeField] Health dummy;
    [SerializeField] StatusData stunData;
    [SerializeField] StatusData slowData;
    StatusController playerStatus;
    readonly object slowSource = new object();
    readonly object buffSource = new object();
    readonly object resistSource = new object();
    UltimateGauge gauge;

    void Awake()
    {
        mech = GetComponent<MechStats>();
        health = GetComponent<Health>();
        playerStatus = GetComponent<StatusController>();
        gauge = GetComponent<UltimateGauge>();
    }

    void OnEnable() => health.Damaged += OnDamaged;
    void OnDisable() => health.Damaged -= OnDamaged;

    void OnDamaged(DamageResult r)
    {
        Debug.Log("โดนดาเมจ " + r.amount.ToString("F1") + (r.isCrit ? " (CRIT)" : "")
            + " -> โล่ " + health.Shield.ToString("F1") + " / HP " + health.Current.ToString("F1"));
    }

    void Update()
    {
        if (!DebugGate.Enabled) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.jKey.wasPressedThisFrame) health.Damage(10f);      // เจ็บ 10 (ข้ามสูตร)
        if (kb.lKey.wasPressedThisFrame) health.Damage(60f);      // เจ็บ 60 (ทะลุโล่ถึง HP ไว้ทดสอบโมดูลเสีย)
        if (kb.hKey.wasPressedThisFrame) health.Heal(10f);
        if (kb.kKey.wasPressedThisFrame) health.Damage(9999f);    // ฆ่าตัวเอง

        if (kb.tKey.wasPressedThisFrame) playerStatus.Apply(stunData, gameObject);
        if (kb.yKey.wasPressedThisFrame) playerStatus.Apply(slowData, gameObject);
        if (kb.uKey.wasPressedThisFrame) gauge.AddCharge(100f);

        if (kb.digit0Key.wasPressedThisFrame)                      // ยิง dummy 20
            DamageCalculator.Deal(new DamageInfo { baseAmount = 20f, type = DamageType.Kinetic, source = gameObject }, mech, dummy);
    }

    void Log(string action, StatBlock s)
    {
        Debug.Log(action + " -> MaxSpeed = " + s.Get(StatId.MaxSpeed));
    }
}