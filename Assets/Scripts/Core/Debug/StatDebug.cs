using UnityEngine;
using UnityEngine.InputSystem;

public class StatDebug : MonoBehaviour
{
    MechStats mech;
    Health health;
    [SerializeField] Health dummy;
    readonly object slowSource = new object();
    readonly object buffSource = new object();
    readonly object resistSource = new object();

    void Awake()
    {
        mech = GetComponent<MechStats>();
        health = GetComponent<Health>();
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
        var kb = Keyboard.current;
        if (kb == null) return;
        var s = mech.Stats;

        if (kb.digit1Key.wasPressedThisFrame)
        {
            s.AddModifier(new StatModifier(StatId.MaxSpeed, ModType.More, 0.5f, slowSource));
            Log("ใส่ Slow (More x0.5)", s);
        }
        if (kb.digit2Key.wasPressedThisFrame)
        {
            s.RemoveModifiersFromSource(slowSource);
            Log("ถอด Slow", s);
        }
        if (kb.digit3Key.wasPressedThisFrame)
        {
            s.AddModifier(new StatModifier(StatId.MaxSpeed, ModType.Increased, 0.5f, buffSource));
            Log("ใส่ Buff (Increased +50%)", s);
        }
        if (kb.digit4Key.wasPressedThisFrame)
        {
            s.RemoveModifiersFromSource(buffSource);
            Log("ถอด Buff", s);
        }

        if (kb.digit5Key.wasPressedThisFrame) health.Damage(10f);
        if (kb.digit6Key.wasPressedThisFrame) health.Heal(10f);

        if (kb.digit7Key.wasPressedThisFrame)
            DamageCalculator.Deal(new DamageInfo { baseAmount = 20f, type = DamageType.Kinetic, source = gameObject }, mech, health);
        if (kb.digit8Key.wasPressedThisFrame)
            mech.Stats.AddModifier(new StatModifier(StatId.ResistKinetic, ModType.Flat, 0.5f, resistSource));
        if (kb.digit9Key.wasPressedThisFrame)
            mech.Stats.RemoveModifiersFromSource(resistSource);

        if (kb.digit0Key.wasPressedThisFrame)
            DamageCalculator.Deal(new DamageInfo { baseAmount = 20f, type = DamageType.Kinetic, source = gameObject }, mech, dummy);
    }

    void Log(string action, StatBlock s)
    {
        Debug.Log(action + " -> MaxSpeed = " + s.Get(StatId.MaxSpeed));
    }
}