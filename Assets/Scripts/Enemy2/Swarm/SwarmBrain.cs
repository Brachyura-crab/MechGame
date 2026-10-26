using UnityEngine;

namespace EnemyV2
{
    // สมองของ swarm: อ่าน State/Order จาก Follower แล้วแปลเป็นการเคลื่อนที่และการโจมตี
    //   Dormant/Waking = นิ่ง | Recalling = เดินกลับหาเจ้านาย | Linked = ทำตามคำสั่ง
    // การโจมตี: เข้าระยะเร่ง (จากเป้า) = เร่งความเร็วนาน boostDuration หรือจนถึงระยะหยุดเร่ง แล้วความเร็วลดกลับเอง
    //   ด้วย Deceleration ต่ำ (ไถลเลยเป้า)  โจมตีเมื่ออยู่ในระยะโจมตีและไม่ติด cooldown
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyAttack))]
    [RequireComponent(typeof(Follower))]
    public class SwarmBrain : MonoBehaviour, IEnemyState
    {
        [SerializeField] SwarmData data;

        Rigidbody2D rb;
        Health health;
        MechStats mech;
        StatusController status;
        EnemyAttack attack;
        Follower follower;

        bool started;

        // เร่งความเร็ว
        bool boosting;
        bool boostArmed = true;     // true = เข้าระยะเร่งแล้วเริ่มเร่งได้ (หลังเร่งจบต้องออกนอกระยะเร่งก่อนถึงจะ true อีกครั้ง)
        float boostEnd;
        float boostRearmAt;
        
        // ง้างก่อนเร่ง
        bool windingUp;
        float windupStart, windupEnd;
        float windupLockUntil;      // ห้ามเริ่มง้างใหม่จนถึงเวลานี้ (หลังถูกขัด)

        // ท่าว่าง
        Vector2 wanderOffset;
        float nextWanderTime;

        // ชะงัก
        float staggerUntil;
        float staggerImmuneUntil;   // รวมช่วงที่กำลังชะงักอยู่ + ช่วงภูมิคุ้มกัน

        // แยกตัว
        Vector2 separation;
        float nextThinkTime;
        ContactFilter2D filter;
        readonly Collider2D[] hits = new Collider2D[8];

        // แคชเป้าโจมตี
        Transform cachedTarget;
        Health cachedHealth;

        // ---------- สถานะอ่านอย่างเดียว (ชื่อเดียวกับ EnemyBehavior เก่า ให้เอฟเฟกต์ต่อกลับได้) ----------
        public bool Frozen => follower.State == FollowerState.Dormant || follower.State == FollowerState.Waking;
        public bool IsStaggered => Time.time < staggerUntil;
        public bool IsBoosting => boosting;
        public bool IsChasing => boosting;                    // เฟส "Chase" ของ Fx = ช่วงเร่ง
        public bool IsWindingUp => windingUp;                 // ง้างก่อนเร่ง (Fx ใช้ท่า Windup)
        public float WindupRatio => windingUp
            ? Mathf.Clamp01(Mathf.InverseLerp(windupStart, windupEnd, Time.time))
            : 0f;
        public bool IsIdle => follower.State == FollowerState.Linked && IsIdleOrder(follower.Order.type);

        static bool IsIdleOrder(LinkOrderType t) =>
            t == LinkOrderType.Hold || t == LinkOrderType.Guard || t == LinkOrderType.Assist;

        // ---------- วงจรชีวิต ----------

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            mech = GetComponent<MechStats>();
            status = GetComponent<StatusController>();
            attack = GetComponent<EnemyAttack>();
            follower = GetComponent<Follower>();
            EnemyMotion.WarnIfMissing(status, this);

            if (data == null)
            {
                Debug.LogError("SwarmBrain: ต้องใส่ SwarmData", this);
                enabled = false;
            }
        }

        void Start()
        {
            started = true;     // ครั้งแรกรอ Start เพื่อให้ Health พร้อมก่อน
            ResetState();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            BuildFilter();
            if (started) ResetState();
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
        }

        void BuildFilter()
        {
            filter = new ContactFilter2D();
            filter.SetLayerMask(data.enemyLayers);
            filter.useTriggers = false;
        }

        void ResetState()
        {
            boosting = false;
            windingUp = false;
            windupLockUntil = 0f;
            boostArmed = true;
            boostRearmAt = 0f;
            nextWanderTime = 0f;
            staggerUntil = 0f;
            staggerImmuneUntil = 0f;
            separation = Vector2.zero;
            nextThinkTime = Time.time + Random.value * data.separationInterval;   // กระจายเวลา ไม่คิดพร้อมกันทั้งฝูง

            rb.linearVelocity = Vector2.zero;

            if (health.IsDead)
            {
                if (status != null) status.ClearAll();
                health.ResetFull();
            }
        }

        // ---------- วงจรหลัก ----------

        void FixedUpdate()
        {
            if (data == null) return;
            float dt = Time.fixedDeltaTime;

            if (health.IsDead) { rb.linearVelocity = Vector2.zero; return; }

            // ความสามารถมาจาก StatBlock (โดนสถานะสโลว์/บัฟได้)
            StatBlock s = mech.Stats;
            float maxSpeed = s.Get(StatId.MaxSpeed);
            float accel = s.Get(StatId.Acceleration);
            float decel = s.Get(StatId.Deceleration);
            float cap = IsStaggered ? maxSpeed * data.staggerSpeedRatio : float.MaxValue;
            float cruise = Mathf.Min(maxSpeed * data.cruiseSpeedRatio, cap);

            bool steer = EnemyMotion.CanSteer(status, rb, decel, dt);
            FollowerState st = follower.State;

            // ไม่มีเจ้านาย/กำลังตื่น: นิ่ง (เบรกด้วย Deceleration)
            if (st == FollowerState.Dormant || st == FollowerState.Waking)
            {
                EndBoost();
                if (steer) rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, decel * dt);
                return;
            }

            // ถูกควบคุม (stun/knockback): ให้ EnemyMotion จัดการความเร็ว AI ไม่ยุ่ง
            if (!steer) { EndBoost(); return; }

            if (data.separationRadius > 0f && Time.time >= nextThinkTime) Think();

            Vector2 pos = rb.position;
            Vector2 targetVel;
            float brake = Mathf.Max(decel, data.arriveDecel);   // ปกติ: เบรกให้ทันจุดหมาย
            bool allowStrike = true;                            // โจมตีผู้เล่นที่อยู่ในระยะตามโอกาสได้ไหม
            Health strike = null;                               // เป้าที่โจมตีอยู่ตามคำสั่ง Attack
            float strikeDist = float.MaxValue;

            if (st == FollowerState.Recalling)
            {
                EndBoost();
                allowStrike = false;
                float sp = Mathf.Min(cruise * data.recallSpeedRatio, cap);
                targetVel = Arrive(follower.Owner.Position - pos, 0.5f, sp, brake);
            }
            else
            {
                LinkOrder o = EffectiveOrder();
                if (o.type != LinkOrderType.Attack) EndBoost();

                switch (o.type)
                {
                    case LinkOrderType.Attack:
                        {
                            Vector2 to = (Vector2)o.target.position - pos;
                            float dist = to.magnitude;
                            UpdateBoost(dist);

                            float sp = boosting ? Mathf.Min(maxSpeed * data.boostSpeedRatio, cap) : cruise;
                            bool close = dist <= attack.Range * data.attackStopFactor || dist < 0.0001f;
                            targetVel = (close || windingUp) ? Vector2.zero : to / dist * sp;   // ง้างอยู่ = หยุดนิ่ง

                            // โหมดโจมตี: ใช้ Deceleration ของ Stat ตรงๆ (ต่ำ = ไถลเลยเป้า) แต่ตอนง้างต้องหยุดจริง
                            brake = windingUp ? Mathf.Max(decel, data.arriveDecel) : decel;
                            strike = HealthOf(o.target);
                            strikeDist = dist;
                            break;
                        }

                    case LinkOrderType.Guard:
                        targetVel = Idle(o.point, Mathf.Max(1f, o.radius), cruise, brake);
                        break;

                    case LinkOrderType.Assist:
                        targetVel = Idle(o.target.position, data.assistRadius, cruise, brake);
                        break;

                    case LinkOrderType.Block:
                        {
                            Vector2 p = o.target.position;
                            Vector2 toThreat = o.threat != null ? (Vector2)o.threat.position - p : Vector2.right;
                            Vector2 goal = p + toThreat.normalized * data.blockOffset;
                            targetVel = Arrive(goal - pos, 0.3f, cruise, brake);
                            break;
                        }

                    case LinkOrderType.Retreat:
                        allowStrike = false;
                        targetVel = Arrive(o.point - pos, 0.3f, Mathf.Min(cruise * data.retreatSpeedRatio, cap), brake);
                        break;

                    default: // Hold
                        targetVel = Idle(follower.Owner.Position, data.holdRadius, cruise, brake);
                        break;
                }
            }

            if (!boosting && !windingUp) targetVel += separation * data.separationSpeed;

            // เร่ง/เลี้ยว = Acceleration, ชะลอ = เบรก (ต่ำ = ไถล)
            float a = targetVel.magnitude >= rb.linearVelocity.magnitude ? accel : brake;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVel, a * dt);

            // โจมตี: ตามคำสั่ง Attack ใช้เป้าของคำสั่ง  คำสั่งอื่นโจมตีผู้เล่นที่อยู่ในระยะตามโอกาส
            if (strike == null && allowStrike && EnemyTarget.Ensure())
            {
                Health ph = EnemyTarget.PlayerHealth;
                if (ph != null)
                {
                    strike = ph;
                    strikeDist = Vector2.Distance(pos, EnemyTarget.Transform.position);
                }
            }
            if (strike != null && !strike.IsDead && strikeDist <= attack.Range)
                attack.TryAttack(strike);   // cooldown/ความเร็วโจมตีอยู่ใน EnemyAttack
        }

        // คำสั่งที่ใช้จริง: เป้าหาย/เป้าตายแล้ว = Hold
        LinkOrder EffectiveOrder()
        {
            LinkOrder o = follower.Order;
            bool needsTarget = o.type == LinkOrderType.Attack || o.type == LinkOrderType.Assist || o.type == LinkOrderType.Block;
            if (needsTarget && o.target == null) return LinkOrder.Hold();

            if (o.type == LinkOrderType.Attack)
            {
                Health h = HealthOf(o.target);
                if (h != null && h.IsDead) return LinkOrder.Hold();
            }
            return o;
        }

        Health HealthOf(Transform t)
        {
            if (t == null) return null;
            if (EnemyTarget.Ensure() && t == EnemyTarget.Transform) return EnemyTarget.PlayerHealth;
            if (t != cachedTarget)
            {
                cachedTarget = t;
                cachedHealth = t.GetComponent<Health>();
            }
            return cachedHealth;
        }

        // ---------- เร่งความเร็ว ----------

        void UpdateBoost(float dist)
        {
            // ออกนอกระยะเร่ง (และพ้นช่วงพัก) ถึงจะเร่งรอบใหม่ได้
            if (!boostArmed && dist > data.boostRange && Time.time >= boostRearmAt) boostArmed = true;

            // ง้าง: นิ่งอยู่กับที่ ครบเวลา = เริ่มเร่ง  เป้าเข้าใกล้เกิน/หนีไกลเกิน = ยกเลิก
            if (windingUp)
            {
                if (dist <= data.boostStopRange || dist > data.boostRange * 1.5f) CancelWindup(0f);
                else if (Time.time >= windupEnd) StartBoost();
                return;
            }

            if (!boosting)
            {
                if (boostArmed && Time.time >= windupLockUntil && dist <= data.boostRange && dist > data.boostStopRange
                    && (!data.boostRequiresReady || attack.Ready))
                {
                    boostArmed = false;
                    if (data.boostWindupTime > 0f)
                    {
                        windingUp = true;
                        windupStart = Time.time;
                        windupEnd = windupStart + data.boostWindupTime * Random.Range(0.8f, 1.2f);   // สุ่ม ±20% กันพุ่งพร้อมกันเป๊ะ
                    }
                    else StartBoost();
                }
            }
            else if (dist <= data.boostStopRange || Time.time >= boostEnd)
            {
                EndBoost();
            }
        }

        void StartBoost()
        {
            windingUp = false;
            boosting = true;
            boostEnd = Time.time + data.boostDuration;
        }

        // ยกเลิกการง้าง: เริ่มรอบใหม่ได้ (หลัง lock วินาที) โดยไม่ต้องออกนอกระยะเร่งก่อน
        void CancelWindup(float lockSeconds)
        {
            if (!windingUp) return;
            windingUp = false;
            boostArmed = true;
            windupLockUntil = Time.time + lockSeconds;
        }

        void EndBoost()
        {
            CancelWindup(0f);
            if (!boosting) return;
            boosting = false;
            boostRearmAt = Time.time + data.boostCooldown;
        }

        // ---------- การเคลื่อนที่ช่วยเหลือ ----------

        // ความเร็วเป้าหมายไปยังจุดที่ห่าง to: ชะลอล่วงหน้าให้เบรกทันพอดีที่ระยะ stop
        static Vector2 Arrive(Vector2 to, float stop, float speed, float brake)
        {
            float d = to.magnitude;
            if (d <= stop || d < 0.0001f) return Vector2.zero;

            float v = Mathf.Min(speed, Mathf.Sqrt(2f * brake * (d - stop)));
            return to / d * v;
        }

        // ท่าว่าง: เดินวนในรัศมี radius รอบ anchor (anchor เคลื่อนที่ได้) ไกลเกิน 1.5 เท่ารีบกลับ
        Vector2 Idle(Vector2 anchor, float radius, float cruise, float brake)
        {
            if (Time.time >= nextWanderTime)
            {
                wanderOffset = Random.insideUnitCircle * radius;
                nextWanderTime = Time.time + Random.Range(data.idleRepathMin, data.idleRepathMax);
            }

            Vector2 pos = rb.position;
            float far = Vector2.Distance(pos, anchor);
            float speed = far > radius * 1.5f ? cruise : cruise * data.idleSpeedRatio;
            Vector2 goal = anchor + Vector2.ClampMagnitude(wanderOffset, radius);
            return Arrive(goal - pos, 0.4f, speed, brake);
        }

        // ---------- แยกตัว / ชะงัก ----------

        void Think()
        {
            nextThinkTime = Time.time + data.separationInterval;

            int n = Physics2D.OverlapCircle(rb.position, data.separationRadius, filter, hits);
            Vector2 push = Vector2.zero;

            for (int i = 0; i < n; i++)
            {
                Rigidbody2D other = hits[i].attachedRigidbody;
                if (other == null || other == rb) continue;

                Vector2 away = rb.position - other.position;
                float d = away.magnitude;
                if (d < 0.001f) { push += Random.insideUnitCircle.normalized; continue; }   // ซ้อนทับพอดี สุ่มทิศ
                push += (away / d) * (1f - d / data.separationRadius);
            }

            separation = Vector2.ClampMagnitude(push, 1f);
        }

        void OnDamaged(DamageResult r)
        {
            if (data.staggerTime <= 0f) return;
            if (r.amount < data.staggerMinDamage) return;
            if (Time.time < staggerImmuneUntil) return;     // กำลังชะงัก/ยังมีภูมิคุ้มกัน ไม่ต่อเวลา

            staggerUntil = Time.time + data.staggerTime;
            staggerImmuneUntil = staggerUntil + data.staggerImmunityTime;
            CancelWindup(data.windupRetryDelay);            // ชะงัก = ขัดการง้าง (รอสักพักถึงง้างใหม่)
            EndBoost();                                     // และตัดการเร่ง

            if (status != null && status.IsKnockedBack) return;   // กำลังถูกผลัก ไม่ตัดความเร็ว
            float cap = mech.Stats.Get(StatId.MaxSpeed) * data.staggerSpeedRatio;
            rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, cap);
        }

        // ---------- Gizmo ----------

        void OnDrawGizmosSelected()
        {
            if (data == null) return;

            EnemyAttack a = GetComponent<EnemyAttack>();
            if (a != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, a.Range); }

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, data.boostRange);        // ระยะเร่ง (ความหมาย: วัดจากเป้า วาดรอบตัวเองเพื่อดูขนาด)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.boostStopRange);    // ระยะหยุดเร่ง
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, data.separationRadius);
        }
    }
}