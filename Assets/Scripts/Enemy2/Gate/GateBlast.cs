using UnityEngine;

namespace EnemyV2
{
    // เสียบกับ gate: ตอนชาร์จจบ ทำดาเมจ + knockback ใส่ผู้เล่นตามระยะ และเล่นเอฟเฟกต์จอ
    [RequireComponent(typeof(Gate))]
    public class GateBlast : MonoBehaviour
    {
        [SerializeField] GateBlastData data;

        Gate gate;

        public float Radius => data != null ? data.radius : 0f;

        void Awake()
        {
            gate = GetComponent<Gate>();

            if (data == null)
            {
                Debug.LogError("GateBlast: ต้องใส่ GateBlastData", this);
                enabled = false;
            }
        }

        void OnEnable() => gate.Exploded += Explode;
        void OnDisable() => gate.Exploded -= Explode;

        void Explode()
        {
            Vector2 c = transform.position;
            float R = data.radius;

            if (EnemyTarget.Ensure())
            {
                Health ph = EnemyTarget.PlayerHealth;
                Vector2 pp = EnemyTarget.Transform.position;
                float d = Vector2.Distance(c, pp);

                if (ph != null && !ph.IsDead && d <= R)
                {
                    float k = data.falloff.Evaluate(Mathf.Clamp01(d / R));

                    DamageCalculator.Deal(new DamageInfo
                    {
                        baseAmount = ph.Max * data.maxFraction * k,
                        type = DamageType.Kinetic,
                        source = gameObject,
                        noCrit = true
                    }, null, ph);

                    if (ph.TryGetComponent(out StatusController sc))
                    {
                        Vector2 away = pp - c;
                        if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle;
                        sc.Knockback(away, data.knockback * k);
                    }
                }
            }

            ExplosionScreenFx.Trigger(c, R);
        }
    }
}