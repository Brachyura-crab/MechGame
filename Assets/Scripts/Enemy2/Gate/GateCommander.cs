using UnityEngine;

namespace EnemyV2
{
    // ให้ gate สั่ง swarm ของตัวเองได้เหมือน leader (ส่งคำสั่งซ้ำตามช่วงเวลา เพราะคำสั่งหายเมื่อถูกเรียกกลับ)
    //   ผู้เล่นเข้า aggroRange: ส่วนหนึ่ง (attackFraction) Attack ผู้เล่น ที่เหลือป้องกัน gate
    //     ป้องกัน = Block (ยืนขวางระหว่าง gate กับผู้เล่น) ถ้าผู้เล่นใกล้ gate ไม่เกิน blockTriggerDistance ไม่งั้น Guard รอบ gate
    //   ผู้เล่นออกนอก aggroRange + exitBuffer / ตาย: Guard รอบ gate (ตายแล้ว Hold)
    // สั่งเฉพาะ swarm ที่ gate เป็นเจ้าของ ไม่ยุ่งกับ swarm ของ leader
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Gate))]
    public class GateCommander : MonoBehaviour
    {
        [Header("Aggro")]
        [SerializeField] float aggroRange = 16f;          // ผู้เล่นเข้าใกล้ gate ขนาดนี้ = เริ่มโจมตี
        [SerializeField] float exitBuffer = 4f;           // ต้องออกไปไกลกว่านี้อีกถึงเลิก (กันกระพริบที่ขอบ)
        [SerializeField, Range(0f, 1f)] float attackFraction = 0.6f;   // สัดส่วนที่ไปโจมตี (1 = ทั้งหมด) ที่เหลือเฝ้า gate
        [SerializeField] int minAttackFlock = 1;          // มี swarm อย่างน้อยเท่านี้ถึงออกโจมตี

        [Header("Defend")]
        [SerializeField] float guardRadius = 6f;          // เดินวนรอบ gate ตอนไม่มีเรื่อง
        [Header("Timing")]
        [SerializeField] float orderInterval = 0.5f;

        Gate gate;
        LinkSource source;
        float nextOrderTime;
        bool aggro;

        void Awake()
        {
            gate = GetComponent<Gate>();
            source = GetComponent<LinkSource>();
        }

        void OnEnable()
        {
            aggro = false;
            nextOrderTime = 0f;
        }

        void Update()
        {
            if (Time.time < nextOrderTime) return;
            nextOrderTime = Time.time + orderInterval;

            if (gate.State == GateState.Closed || !source.IsActive || source.Count == 0) return;

            Vector2 gatePos = transform.position;

            if (!EnemyTarget.Ensure())
            {
                source.Issue(LinkOrder.Guard(gatePos, guardRadius));
                return;
            }

            Health ph = EnemyTarget.PlayerHealth;
            if (ph != null && ph.IsDead)
            {
                aggro = false;
                source.Issue(LinkOrder.Hold());
                return;
            }

            Transform player = EnemyTarget.Transform;
            float d = Vector2.Distance(gatePos, player.position);

            if (!aggro && d <= aggroRange) aggro = true;
            else if (aggro && d > aggroRange + exitBuffer) aggro = false;

            if (aggro && source.Count < minAttackFlock) aggro = false;

            LinkOrder defend = LinkOrder.Guard(gatePos, guardRadius);

            if (!aggro)
            {
                source.Issue(defend);
                return;
            }

            // ครึ่งแรกของลิสต์ Attack ที่เหลือป้องกัน
            int attackers = Mathf.CeilToInt(source.Count * attackFraction);
            LinkOrder attack = LinkOrder.Attack(player);
            var list = source.Followers;
            for (int i = 0; i < list.Count; i++)
                source.Issue(list[i], i < attackers ? attack : defend);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, aggroRange + exitBuffer);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, guardRadius);
        }
    }
}