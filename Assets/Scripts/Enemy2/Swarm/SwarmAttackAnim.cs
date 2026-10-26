using UnityEngine;

namespace EnemyV2
{
    // เล่นภาพท่าโจมตีของ swarm ตามสถานะของ SwarmBrain (ทางเดียว Brain → ภาพ ไม่ส่งผลกับ gameplay)
    //   ง้าง: frame ตาม WindupRatio | เร่ง: ค้างท่าง้าง | โจมตีออก: เล่นท่าโจมตีจนจบ
    //   เร่งจบแต่ไม่โจมตี: ค้างท่างอ holdAfterBoost แล้วย้อนกลับท่าพัก | ตีประชิดตอนยังพัก: เล่นตั้งแต่ frame แรก
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SwarmBrain))]
    [RequireComponent(typeof(EnemyAttack))]
    [RequireComponent(typeof(Health))]
    public class SwarmAttackAnim : MonoBehaviour
    {
        [SerializeField] SwarmAnimData data;
        [SerializeField] SpriteRenderer body;      // SpriteBody
        [SerializeField] SpriteRenderer glow;      // SwarmGlow (ว่าง = ไม่เปลี่ยน glow)
        [SerializeField] SpriteRenderer effect;    // ชั้น effect (ว่าง = ข้าม)

        enum Mode { Rest, Windup, Boost, Hold, Strike, Reverse }

        SwarmBrain brain;
        EnemyAttack attack;
        Health health;

        Mode mode;
        float frame;        // ตำแหน่งต่อเนื่อง (frame ที่แสดง = floor)
        float holdTimer;
        int shown = -1;
        int last;           // index frame สุดท้าย
        int poseIndex;      // index ท่าง้างเต็ม
        bool dead;
        bool valid;

        void Awake()
        {
            brain = GetComponent<SwarmBrain>();
            attack = GetComponent<EnemyAttack>();
            health = GetComponent<Health>();

            if (data == null || body == null || data.FrameCount < 2)
            {
                Debug.LogError("SwarmAttackAnim: ต้องใส่ Data (body อย่างน้อย 2 frame) และ Body", this);
                enabled = false;
                return;
            }

            last = data.FrameCount - 1;
            poseIndex = Mathf.Clamp(data.windupFrames - 1, 0, last - 1);
            valid = true;

            if (data.glow != null && data.glow.Length > 0 && data.glow.Length != data.FrameCount)
                Debug.LogWarning("SwarmAttackAnim: จำนวน frame ของ glow ไม่เท่า body", this);
            if (data.effect != null && data.effect.Length > 0 && data.effect.Length != data.FrameCount)
                Debug.LogWarning("SwarmAttackAnim: จำนวน frame ของ effect ไม่เท่า body", this);
        }

        void OnEnable()
        {
            if (!valid) return;
            attack.Attacked += OnAttacked;
            health.Died += OnDied;
            dead = false;
            mode = Mode.Rest;
            frame = 0f;
            shown = -1;
            Show();
        }

        void OnDisable()
        {
            if (!valid) return;
            attack.Attacked -= OnAttacked;
            health.Died -= OnDied;
        }

        void OnDied()
        {
            dead = true;
            mode = Mode.Rest;
            frame = 0f;
            shown = -1;
            Show();                                          // ศพใช้ท่าพัก
            if (effect != null) effect.enabled = false;
        }

        // โจมตีออกจริง (ตีประชิด หรือพุ่งถึง): ท่าง้างเต็มแล้ว = เล่นท่าโจมตีต่อ ยังไม่ง้าง = เล่นต่อจาก frame ปัจจุบัน
        void OnAttacked()
        {
            if (dead) return;
            bool raised = mode == Mode.Boost || mode == Mode.Hold || frame >= poseIndex;
            frame = raised ? poseIndex + 1 : Mathf.Floor(frame);
            mode = Mode.Strike;
        }

        void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;

            // หลับ/ไม่มีเจ้านาย: กลับท่าพักทันที
            if (brain.Frozen)
            {
                if (mode != Mode.Rest) SetRest();
                return;
            }

            switch (mode)
            {
                case Mode.Rest:
                    if (brain.IsWindingUp) mode = Mode.Windup;
                    else if (brain.IsChasing) EnterBoost();          // ไม่มี windup (boostWindupTime = 0)
                    break;

                case Mode.Windup:
                    if (brain.IsWindingUp)
                        frame = Mathf.Min(poseIndex, Mathf.Floor(brain.WindupRatio * (poseIndex + 1)));
                    else if (brain.IsChasing) EnterBoost();          // ง้างครบแล้วพุ่ง
                    else StartReverse();                             // ถูกขัด (ชะงัก/เป้าเข้าใกล้เกิน)
                    break;

                case Mode.Boost:
                    frame = poseIndex;
                    if (brain.IsWindingUp) mode = Mode.Windup;
                    else if (!brain.IsChasing)
                    {
                        if (brain.IsStaggered) StartReverse();
                        else { mode = Mode.Hold; holdTimer = data.holdAfterBoost; }
                    }
                    break;

                case Mode.Hold:
                    if (brain.IsWindingUp) mode = Mode.Windup;
                    else if (brain.IsChasing) mode = Mode.Boost;
                    else if (brain.IsStaggered) StartReverse();
                    else
                    {
                        holdTimer -= dt;
                        if (holdTimer <= 0f) StartReverse();
                    }
                    break;

                case Mode.Strike:
                    frame += data.strikeFps * dt;
                    if (frame >= last + 1) SetRest();
                    break;

                case Mode.Reverse:
                    if (brain.IsWindingUp) mode = Mode.Windup;
                    else if (brain.IsChasing) EnterBoost();
                    else
                    {
                        frame -= data.reverseFps * dt;
                        if (frame <= 0f) SetRest();
                    }
                    break;
            }

            Show();
        }

        void EnterBoost()
        {
            frame = poseIndex;
            mode = Mode.Boost;
        }

        void StartReverse()
        {
            if (frame <= 0f) { SetRest(); return; }
            mode = Mode.Reverse;
        }

        void SetRest()
        {
            mode = Mode.Rest;
            frame = 0f;
            Show();
        }

        void Show()
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(frame), 0, last);
            if (i == shown) return;
            shown = i;

            body.sprite = data.body[i];

            if (glow != null && data.glow != null && i < data.glow.Length)
                glow.sprite = data.glow[i];

            if (effect != null)
            {
                Sprite e = data.effect != null && i < data.effect.Length ? data.effect[i] : null;
                effect.sprite = e;
                effect.enabled = e != null;
            }
        }
    }
}