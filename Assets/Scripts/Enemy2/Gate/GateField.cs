using UnityEngine;

namespace EnemyV2
{
    // สนามอันตรายรอบ gate: ผู้เล่นเข้าใกล้ = ช้าลงแบบทวีคูณ + โดนดาเมจต่อเนื่อง (ผ่านเกราะตามปกติ)
    //   ความลึก t = 1 - ระยะ/รัศมี (0 ที่ขอบ → 1 ที่กลาง)
    //   ความเร็ว = minSpeedMul ^ t   ดาเมจ = dps × t^damagePower
    //   ตอน Charging: รัศมีกว้างมาก แต่ดาเมจ/การชะลอเบามาก  gate ปิด/ผู้เล่นตาย = หายทันที
    // ไม่ยุ่งกับศัตรู มีแค่ผู้เล่น  ภาพทำเองได้โดยอ่าน CurrentRadius / Depth01
    [RequireComponent(typeof(Gate))]
    public class GateField : MonoBehaviour
    {
        [SerializeField] GateFieldData data;

        public float CurrentRadius { get; private set; }   // รัศมีตอนนี้ (ตามสถานะ gate)
        public float Depth01 { get; private set; }         // ความลึกของผู้เล่นตอนนี้ 0..1 (0 = อยู่นอกสนาม)
        public float ChargeBlend => ramp;                  // 0 = ปกติ .. 1 = Charging เต็ม

        Gate gate;
        StatBlock slowed;       // stat ของผู้เล่นที่เราใส่ตัวชะลออยู่
        float lastMul = 1f;
        float ramp;
        float tickTimer;

        void Awake()
        {
            gate = GetComponent<Gate>();
            if (data == null)
            {
                Debug.LogError("GateField: ต้องใส่ GateFieldData", this);
                enabled = false;
            }
        }

        void OnDisable() => Release();

        void Update()
        {
            float dt = Time.deltaTime;
            bool open = gate.State != GateState.Closed;
            bool charging = gate.State == GateState.Charging;

            ramp = Mathf.MoveTowards(ramp, charging ? 1f : 0f, dt / data.chargeRampTime);
            CurrentRadius = open ? data.radius * Mathf.Lerp(1f, data.chargeRadiusMul, ramp) : 0f;
            Depth01 = 0f;

            if (!open || !EnemyTarget.Ensure()) { Release(); return; }

            Health ph = EnemyTarget.PlayerHealth;
            if (ph == null || ph.IsDead) { Release(); return; }

            float d = Vector2.Distance(transform.position, EnemyTarget.Transform.position);
            if (d >= CurrentRadius) { Release(); tickTimer = 0f; return; }

            float t = 1f - d / CurrentRadius;
            Depth01 = t;

            // ความเร็ว: ทวีคูณตามความลึก (ตอน Charging ลดแรงลง)
            float strength = Mathf.Lerp(1f, data.chargeSpeedStrength, ramp);
            ApplySlow(ph.Stats, Mathf.Pow(data.minSpeedMul, t * strength));

            // ดาเมจต่อเนื่อง
            tickTimer += dt;
            if (tickTimer >= data.tickInterval)
            {
                tickTimer -= data.tickInterval;
                float dps = data.damagePerSecond
                          * Mathf.Pow(t, data.damagePower)
                          * Mathf.Lerp(1f, data.chargeDamageMul, ramp);
                float amount = dps * data.tickInterval;
                if (amount > 0.0001f)
                {
                    var info = new DamageInfo
                    {
                        baseAmount = amount,
                        type = DamageType.Kinetic,
                        source = gameObject,
                        noCrit = true
                    };
                    DamageCalculator.Deal(info, null, ph);   // ไม่มีผู้โจมตี = ไม่ใช้ DamageMultiplier/คริ แต่หักเกราะตามปกติ
                }
            }
        }

        // ใส่ตัวชะลอ (More) เฉพาะเมื่อค่าเปลี่ยนพอสมควร ไม่ถอดใส่ทุกเฟรม
        void ApplySlow(StatBlock stats, float mul)
        {
            if (slowed == stats && Mathf.Abs(mul - lastMul) < 0.01f) return;

            if (slowed != null) slowed.RemoveModifiersFromSource(this);

            stats.AddModifier(new StatModifier(StatId.MaxSpeed, ModType.More, mul, this));
            if (data.slowBoost) stats.AddModifier(new StatModifier(StatId.BoostSpeed, ModType.More, mul, this));
            if (data.slowDash) stats.AddModifier(new StatModifier(StatId.DashSpeed, ModType.More, mul, this));

            slowed = stats;
            lastMul = mul;
        }

        void Release()
        {
            if (slowed == null) return;
            slowed.RemoveModifiersFromSource(this);
            slowed = null;
            lastMul = 1f;
        }

        void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, data.radius);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, data.radius * data.chargeRadiusMul);
        }
    }
}