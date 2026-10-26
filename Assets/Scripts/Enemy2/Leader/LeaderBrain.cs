using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    public enum LeaderRole
    {
        Guard,    // ยามรอบ gate: ประจำจุดยาม หนีเมื่อผู้เล่นเข้าใกล้ ยิงเมื่อผู้เล่นอยู่ในระยะ ฝูงประจำ/บัง gate
        Engage    // สู้ผู้เล่น: kite รักษาระยะ ยิง ฝูงโจมตี  (ไม่มี camp = Engage ตลอด)
    }

    // สมองของ leader: เคลื่อนที่/หนี/kite/ยิง ตามบทบาท และส่งคำสั่งให้ฝูง + โอนฝูงช่วย leader ที่กำลังสู้
    // บทบาทมาจาก CampCommander (Engage = ตัวที่ใกล้ผู้เล่นที่สุดตอน alert) ไม่มี camp/commander = Engage
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(MechStats))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(LinkSource))]
    [RequireComponent(typeof(CampMember))]
    public class LeaderBrain : MonoBehaviour, IEnemyState
    {
        [SerializeField] LeaderData data;

        Rigidbody2D rb;
        MechStats mech;
        Health health;
        StatusController status;
        LinkSource source;
        CampMember member;
        ShotgunAttack attack;           // ไม่บังคับ ไม่มี = ไม่ยิง

        Camp cachedCamp;
        CampCommander cachedCommander;
        bool warnedNoCommander;

        bool retreating;
        bool rushing;
        bool returning;
        bool escorting;
        bool guardAggro;
        float swayPhase;
        float calmSince = -999f;
        float slotAngleDeg;             // จุดยามตอนสงบ: มุมบนวงรอบ gate
        float slotOffsetDeg;            // จุดยามตอน alert: เบี่ยงจากเส้น gate→ผู้เล่น (ไม่ให้ยามหลายตัวซ้อนกัน)
        float nextOrderTime;
        float nextTransferTime;
        Vector2 debugGoal;
        Vector2 wanderUnit;             // จุดเดินสุ่ม (ในวงกลมหน่วย คูณรัศมีตอนใช้)
        float nextWanderTime;

        public static readonly List<LeaderBrain> All = new List<LeaderBrain>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();   // กัน static ค้างตอนปิด Reload Domain

        public LeaderRole Role { get; private set; } = LeaderRole.Engage;
        public bool HasCamp { get; private set; }
        public bool Retreating => retreating;
        public bool Returning => returning;
        public float MoveScale { get; private set; } = 1f;      // ลูกซอง windup ชะลอการเคลื่อนที่ (1 = ปกติ)
        public bool IsDead => health != null && health.IsDead;

        // IEnemyState (เอฟเฟกต์อ่าน): leader ไม่หลับ ไม่ไล่ แต่ง้างลูกซองได้
        public bool Frozen => false;
        public bool IsChasing => false;
        public bool IsWindingUp => attack != null && attack.IsWindingUp;
        public float WindupRatio => attack != null ? attack.WindupRatio : 0f;

        public LinkSource Source => source;
        public CampMember Member => member;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            mech = GetComponent<MechStats>();
            health = GetComponent<Health>();
            status = GetComponent<StatusController>();
            source = GetComponent<LinkSource>();
            member = GetComponent<CampMember>();
            TryGetComponent(out attack);

            swayPhase = Random.value * Mathf.PI * 2f;
            slotAngleDeg = Random.value * 360f;
            slotOffsetDeg = Random.Range(-35f, 35f);

            EnemyMotion.WarnIfMissing(status, this);

            if (data == null)
            {
                Debug.LogError("LeaderBrain: ต้องใส่ LeaderData", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            retreating = false;
            rushing = false;
            returning = false;
            escorting = false;
            guardAggro = false;
            MoveScale = 1f;
            calmSince = -999f;
            nextWanderTime = 0f;
            if (!All.Contains(this)) All.Add(this);
            health.Died += OnDied;
            if (attack != null) attack.Fired += OnFired;
        }

        void OnDisable()
        {
            All.Remove(this);
            health.Died -= OnDied;
            if (attack != null) attack.Fired -= OnFired;
        }

        // leader ตาย: เลิกเป็นผู้สั่ง ผู้ตามทั้งหมดกลายเป็นไม่มีเจ้านาย (gate/leader อื่นในระยะเชื่อมจะรับต่อ)
        void OnDied() => source.Deactivate();

        // ---------- วงจรหลัก ----------

        void FixedUpdate()
        {
            if (health.IsDead) { rb.linearVelocity = Vector2.zero; return; }
            if (!EnemyTarget.Ensure()) return;

            StatBlock s = mech.Stats;
            float maxSpeed = s.Get(StatId.MaxSpeed);
            float accelStat = s.Get(StatId.Acceleration);
            float decelStat = s.Get(StatId.Deceleration);
            bool steer = EnemyMotion.CanSteer(status, rb, decelStat, Time.fixedDeltaTime);

            Health ph = EnemyTarget.PlayerHealth;
            bool playerDead = ph != null && ph.IsDead;

            Vector2 pos = rb.position;
            Vector2 playerPos = EnemyTarget.Transform.position;
            Vector2 fromPlayer = pos - playerPos;
            float pDist = fromPlayer.magnitude;
            Vector2 away = pDist > 0.0001f ? fromPlayer / pDist : Vector2.right;

            // camp / commander / บทบาท
            Camp camp = member.Camp;
            Gate gate = member.Gate;
            bool hasCamp = camp != null && gate != null;
            CampCommander cmd = GetCommander(camp);
            HasCamp = hasCamp;

            if (hasCamp && data.leashDistance > 0f)
{
                float gd = Vector2.Distance(pos, gate.transform.position);
                if (!returning && gd > data.leashDistance) returning = true;
                else if (returning && gd < data.leashDistance * data.leashReleaseRatio) returning = false;
            }
            else returning = false;

            bool engage = !hasCamp || (!returning && cmd != null && cmd.IsEngager(member));
            Role = engage ? LeaderRole.Engage : LeaderRole.Guard;

            if (!playerDead && pDist <= data.sightRadius && cmd != null) cmd.Alert(pos);   // เห็นผู้เล่น = camp alert

            // ---------- เป้าหมายการเคลื่อนที่ ----------
            Vector2 station = Vector2.zero;
            if (!engage) station = GuardStation(gate.transform.position, playerPos, cmd != null && cmd.Alerted);

            // หนีผู้เล่น (hysteresis) Guard ใช้ชุด flee Engage ใช้ชุด keep
            float keep = engage ? data.keepDistance : data.fleeDistance;
            float buf = engage ? data.retreatBuffer : data.fleeBuffer;
            if (!retreating && pDist < keep) retreating = true;
            else if (retreating && pDist > keep + buf) { retreating = false; calmSince = Time.time; }

            Vector2 targetVel = Vector2.zero;

            if (!playerDead)
            {
                if (retreating)
                {
                    rushing = false;
                    Vector2 fd = engage ? away : StationFlee(pos, station, away);
                    targetVel = fd * (maxSpeed * data.retreatSpeedRatio);
                }
                else if (engage || Time.time - calmSince >= data.returnDelay)
                {
                    Vector2 pt = engage ? playerPos + away * data.standoffDistance : station;
                    pt += WanderOffset(engage);
                    debugGoal = pt;
                    targetVel = SeekVelocity(pos, pt, maxSpeed);
                }
                else rushing = false;
            }

            // แยกจาก leader ตัวอื่น (ตอนรุมผู้เล่นจะได้ไม่กองจุดเดียว)
            if (!playerDead) targetVel += Separation(pos) * data.leaderSeparationSpeed;

            // ---------- ยิง ----------
            if (attack != null) HandleAttack(pDist, playerDead);
            MoveScale = attack != null && attack.IsWindingUp ? attack.WindupMoveScale : 1f;
            targetVel *= MoveScale;

            float accel = targetVel.sqrMagnitude > 0.0001f ? accelStat : decelStat;
            if (steer) rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVel, accel * Time.fixedDeltaTime);

            // ---------- ฝูง ----------
            CommandFlock(engage, hasCamp, gate, playerPos, pDist, playerDead);
            if (!engage && cmd != null && cmd.Alerted) SupportEngager(cmd);
        }

        CampCommander GetCommander(Camp camp)
        {
            if (camp == null) { cachedCamp = null; cachedCommander = null; return null; }

            if (camp != cachedCamp)
            {
                cachedCamp = camp;
                cachedCommander = camp.GetComponent<CampCommander>();
                if (cachedCommander == null && !warnedNoCommander)
                {
                    warnedNoCommander = true;
                    Debug.LogWarning("LeaderBrain: gate ของ camp นี้ไม่มี CampCommander leader จะเป็น Guard ตลอดและไม่ alert", this);
                }
            }
            return cachedCommander;
        }

        // ---------- การเคลื่อนที่ ----------

        // ไปยังจุด pt: ไกล = Rush, ใกล้ = Cruise ชะลอเข้าหา ถึงแล้วลอยนิ่ง + ส่ายข้างตกแต่ง
        Vector2 SeekVelocity(Vector2 pos, Vector2 pt, float maxSpeed)
        {
            Vector2 toGoal = pt - pos;
            float gDist = toGoal.magnitude;

            if (!rushing && gDist > data.rushStart) rushing = true;
            else if (rushing && gDist < data.rushEnd) rushing = false;

            if (gDist <= data.arriveRadius) return Vector2.zero;

            Vector2 dir = toGoal / gDist;
            float ratio = rushing ? data.rushSpeedRatio : data.cruiseSpeedRatio;
            float speed = maxSpeed * ratio;

            if (!rushing)
                speed *= Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(gDist / Mathf.Max(0.01f, data.slowZone)));

            Vector2 v = dir * speed;

            if (!rushing && data.swayAmp > 0f)
            {
                Vector2 perp = new Vector2(-dir.y, dir.x);
                v += perp * (Mathf.Sin(Time.time * data.swayFreq + swayPhase) * data.swayAmp);
            }
            return v;
        }

        // จุดยาม: ตอนสงบ = จุดบนวงรอบ gate  ตอน alert = จุดบนเส้น gate→ผู้เล่น (เบี่ยงเล็กน้อยต่อตัว)
        Vector2 GuardStation(Vector2 gatePos, Vector2 playerPos, bool alerted)
        {
            Vector2 dir;
            if (alerted)
            {
                Vector2 toPlayer = playerPos - gatePos;
                dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.right;
                dir = Rotate(dir, slotOffsetDeg);
            }
            else dir = Rotate(Vector2.right, slotAngleDeg);

            return gatePos + dir * data.guardRadius;
        }

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), sn = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * sn, v.x * sn + v.y * c);
        }

        // หนีผู้เล่น: ทิศห่างจากผู้เล่น + ดึงเข้าจุดยามเล็กน้อย เกินเขตแล้วตัดแรงที่พาออกนอก
        Vector2 StationFlee(Vector2 pos, Vector2 station, Vector2 away)
        {
            Vector2 off = pos - station;
            float d = off.magnitude;
            Vector2 outDir = d > 0.0001f ? off / d : away;
            Vector2 dir = away;

            if (d > data.maxFleeFromStation)
            {
                float outward = Vector2.Dot(dir, outDir);
                if (outward > 0f) dir -= outDir * outward;      // เหลือแค่เลื่อนไปตามขอบเขต
            }
            else dir -= outDir * (data.stationPull * Mathf.Clamp01(d / data.maxFleeFromStation));

            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;   // ถูกบีบจนมุม = ยืนรับ
        }

        // จุดเดินสุ่มรอบจุดหมาย (เหมือน swarm): สุ่มจุดใหม่ทุก wanderRepathMin..Max วินาที
        // Engage จำกัดรัศมีไม่ให้จุดหมายหลุดเข้าเขตถอย (keepDistance)
        Vector2 WanderOffset(bool engage)
        {
            float r = engage
                ? Mathf.Min(data.engageWanderRadius,
                            Mathf.Max(0f, data.standoffDistance - data.keepDistance - data.arriveRadius - 0.3f))
                : data.guardWanderRadius;
            if (r <= 0.01f) return Vector2.zero;

            if (Time.time >= nextWanderTime)
            {
                wanderUnit = Random.insideUnitCircle;
                nextWanderTime = Time.time + Random.Range(data.wanderRepathMin, data.wanderRepathMax);
            }
            return wanderUnit * r;
        }

        // แรงผลักออกจาก leader ตัวอื่นที่อยู่ใกล้กว่า leaderSpacing (ใกล้ = ผลักแรง) รวมแล้วไม่เกิน 1
        Vector2 Separation(Vector2 pos)
        {
            float spacing = data.leaderSpacing;
            if (spacing <= 0f) return Vector2.zero;

            Vector2 push = Vector2.zero;
            for (int i = 0; i < All.Count; i++)
            {
                LeaderBrain o = All[i];
                if (o == null || o == this || o.IsDead) continue;

                Vector2 away = pos - o.rb.position;
                float d = away.magnitude;
                if (d >= spacing) continue;
                if (d < 0.001f) { push += Random.insideUnitCircle.normalized; continue; }   // ซ้อนทับพอดี สุ่มทิศ
                push += (away / d) * (1f - d / spacing);
            }
            return Vector2.ClampMagnitude(push, 1f);
        }

        // ---------- ยิง ----------

        // ยิงทุกครั้งที่ผู้เล่นอยู่ในระยะและท่าพร้อม (ไม่สนว่ากำลังหนี/ไปประจำที่)
        void HandleAttack(float pDist, bool playerDead)
        {
            if (attack.IsWindingUp)
            {
                if (playerDead) attack.Cancel();
                return;
            }

            if (playerDead) return;
            if (pDist > attack.Range || !attack.Ready) return;

            attack.TryStart(EnemyTarget.Transform);
        }

        // แรงถอยตอนยิง: ดีดสวนทิศยิง แล้วค่อยๆ ไถลหยุดตาม Deceleration
        void OnFired(Vector2 dir) => rb.linearVelocity -= dir * attack.RecoilSpeed;

        // ---------- สั่งฝูง ----------

        // ส่งคำสั่งซ้ำตามช่วงเวลา (คำสั่งของผู้ตามหายเมื่อถูกเรียกกลับ)
        //   Engage: ผู้เล่นอยู่ในระยะ + ฝูงพอ = Attack  ไม่งั้น Hold
        //   Guard : ผู้เล่นเข้าใกล้ gate = Block  ไม่งั้น Guard รอบ gate
        void CommandFlock(bool engage, bool hasCamp, Gate gate, Vector2 playerPos, float pDist, bool playerDead)
        {
            if (Time.time < nextOrderTime) return;
            nextOrderTime = Time.time + data.orderInterval;
            if (!source.IsActive) return;

            LinkOrder order;

            if (playerDead) order = LinkOrder.Hold();
            else if (engage)
            {
                bool go = pDist <= data.engageRange && source.Count >= data.minAttackFlock;
                order = go ? LinkOrder.Attack(EnemyTarget.Transform) : LinkOrder.Hold();
            }
            else
            {
                Vector2 gatePos = gate.transform.position;
                float gd = Vector2.Distance(rb.position, gatePos);
                if (!escorting && gd > data.escortDistance) escorting = true;
                else if (escorting && gd < data.escortDistance * data.escortReleaseRatio) escorting = false;

                float pg = Vector2.Distance(gatePos, playerPos);
                if (!guardAggro && pg <= data.guardAggroRange) guardAggro = true;
                else if (guardAggro && pg > data.guardAggroRange + data.guardAggroBuffer) guardAggro = false;

                if (escorting) order = LinkOrder.Hold();
                else
                {
                    // ฝูงที่ไม่ได้โจมตี: ผู้เล่นใกล้ leader = ยืนบัง leader  ไกล = เดินวนรอบ gate
                    LinkOrder idle = pDist <= data.blockTriggerDistance
                        ? LinkOrder.Block(transform, EnemyTarget.Transform)
                        : LinkOrder.Guard(gatePos, data.guardFlockRadius);

                    if (!guardAggro || data.guardAttackFraction <= 0f) order = idle;
                    else
                    {
                        LinkOrder atk = LinkOrder.Attack(EnemyTarget.Transform);
                        int attackers = Mathf.CeilToInt(source.Count * data.guardAttackFraction);
                        var list = source.Followers;
                        for (int i = 0; i < list.Count; i++)
                            source.Issue(list[i], i < attackers ? atk : idle);
                        return;   // สั่งรายตัวแล้ว ไม่ต้องสั่งรวมต่อท้าย
                    }
                }
            }

            source.Issue(order);
        }

        // Guard โอนผู้ตามให้ Engage ที่ฝูงต่ำกว่าเป้า (เก็บไว้ไม่น้อยกว่า guardMinFlock) = "ไปช่วย leader ตัวอื่น"
        void SupportEngager(CampCommander cmd)
        {
            if (Time.time < nextTransferTime) return;
            nextTransferTime = Time.time + data.transferInterval;

            CampMember eng = cmd.Engager;
            if (eng == null || eng == member || eng.Source == null || !eng.Source.IsActive) return;

            int need = eng.Source.Missing;
            int give = Mathf.Min(need, source.Count - data.guardMinFlock);
            if (give > 0) source.TransferTo(eng.Source, give);
        }

        // ---------- Gizmo ----------

        void OnDrawGizmosSelected()
        {
            if (data == null || !Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, debugGoal);
            Gizmos.DrawWireSphere(debugGoal, 0.3f);

            if (EnemyTarget.Transform != null)
            {
                Vector3 p = EnemyTarget.Transform.position;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(p, Role == LeaderRole.Engage ? data.keepDistance : data.fleeDistance);   // หนีเมื่ออยู่ในวงนี้
                Gizmos.color = new Color(1f, 0.6f, 0f);
                Gizmos.DrawWireSphere(p, data.sightRadius);                                                   // เห็นผู้เล่น = camp alert
            }
        }
    }
}