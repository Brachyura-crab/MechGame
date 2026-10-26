using UnityEngine;

[RequireComponent(typeof(MechStats))]
public class EnemyAttack : MonoBehaviour
{
    [SerializeField] AttackData data;

    MechStats mech;
    StatusController status;
    float nextAttackTime;

    public event System.Action Attacked;   // ยิงทุกครั้งที่โจมตีออกจริง (ภาพ/เสียงฟัง)
    public float Range => data != null ? data.range : 0f;
    public bool Ready => data != null && Time.time >= nextAttackTime && EnemyMotion.CanAttack(status);

    void Awake()
    {
        mech = GetComponent<MechStats>();
        status = GetComponent<StatusController>();
    }

    // เปิดใช้ทุกครั้ง (รวมจาก pool) สุ่มหน่วงเริ่มต้น กันฝูงโจมตีพร้อมกันเป๊ะ
    void OnEnable() => ResetCooldown();

    public void Init(AttackData d)
    {
        data = d;
        ResetCooldown();
    }

    void ResetCooldown()
    {
        nextAttackTime = Time.time + (data != null ? Random.value * data.cooldown : 0f);
    }

    // Behavior เป็นคนเรียก คืน true ถ้าโจมตีจริง
    public bool TryAttack(Health target)
    {
        if (!Ready || target == null) return false;

        float rate = Mathf.Max(0.1f, mech.Stats.Get(StatId.FireRate));
        nextAttackTime = Time.time + data.cooldown / rate;

        var info = new DamageInfo
        {
            baseAmount = data.damage,
            type = data.damageType,
            source = gameObject
        };
        DamageCalculator.Deal(info, mech, target);
        Attacked?.Invoke();
        return true;
    }
}