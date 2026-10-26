using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    // camp ของ gate (ติดบน object เดียวกับ Gate):
    //  1) ผู้เล่นเข้า spawnRadius ครั้งแรก = จัดทัพให้ครบ (leader ก่อน แล้ว swarm ผูกกับ leader ที่ฝูงยังไม่เต็ม ที่เหลือผูกกับ gate)
    //  2) หลังจากนั้นเติมตัวที่ขาดจนครบ (swarm ที่เติมผูกกับ gate)  นับ swarm เฉพาะตัวที่มีเจ้านาย
    //  3) ปรับสมดุล: leader ที่ฝูงต่ำกว่าเป้า ขอกำลังเสริมจาก gate / leader อื่น
    //  4) gate ชาร์จ/ปิด = หยุดเติม  gate ระเบิด = final wave (leader ก่อน แล้ว swarm ผูกกับ leader นั้น)
    [RequireComponent(typeof(Gate))]
    public class Camp : MonoBehaviour
    {
        public static readonly List<Camp> All = new List<Camp>();
        public static bool SpawningEnabled = true;   // ปุ่ม debug เปิด/ปิดการเกิดทุก camp

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
            SpawningEnabled = true;
        }

        [SerializeField] CampData data;
        [SerializeField] GameObject leaderPrefab;   // ว่างได้ (ยังไม่มีสมอง leader): camp จะเสกเฉพาะ swarm ผูกกับ gate
        [SerializeField] GameObject swarmPrefab;
        [SerializeField] Transform enemiesRoot;

        readonly List<CampMember> members = new List<CampMember>();

        Gate gate;
        bool deployed;                  // จัดทัพแล้ว
        bool forceDeploy;               // debug: จัดทัพทันทีแม้ผู้เล่นอยู่ไกล
        int burstLeaders, burstSwarm;   // จำนวนที่ยังต้องจัดทัพ
        float nextCheck, nextLeaderTime, nextSwarmTime, nextBalance;
        int leaders, swarm, orphans;    // swarm = เฉพาะที่มีเจ้านาย

        public bool Deployed => deployed;
        public int LeaderCount => leaders;
        public int SwarmCount => swarm;
        public int OrphanCount => orphans;
        public int MemberCount => members.Count;
        public CampData Data => data;
        public IReadOnlyList<CampMember> Members => members;

        void Awake()
        {
            gate = GetComponent<Gate>();

            if (data == null || swarmPrefab == null)
            {
                Debug.LogError("Camp: ต้องใส่ CampData และ swarmPrefab", this);
                enabled = false;
                return;
            }

            if (swarmPrefab.GetComponent<Follower>() == null)
                Debug.LogError("Camp: swarmPrefab ไม่มี Follower", this);
            if (swarmPrefab.GetComponent<CampMember>() == null)
                Debug.LogError("Camp: swarmPrefab ต้องมี CampMember (ไม่งั้นการฆ่าไม่หักพลังงาน)", this);

            if (leaderPrefab == null)
            {
                Debug.LogWarning("Camp: ไม่มี leaderPrefab จะเสกเฉพาะ swarm ผูกกับ gate (ไม่มี final wave)", this);
            }
            else
            {
                if (leaderPrefab.GetComponent<LinkSource>() == null)
                    Debug.LogError("Camp: leaderPrefab ไม่มี LinkSource", this);
                if (leaderPrefab.GetComponent<CampMember>() == null)
                    Debug.LogError("Camp: leaderPrefab ต้องมี CampMember", this);
            }
        }

        void OnEnable()
        {
            All.Add(this);
            if (gate != null) gate.Exploded += OnGateExploded;
        }

        void OnDisable()
        {
            All.Remove(this);
            if (gate != null) gate.Exploded -= OnGateExploded;
        }

        // ---------- ทะเบียนสมาชิก (CampMember เรียกเอง) ----------

        internal void Register(CampMember m)
        {
            if (!members.Contains(m)) members.Add(m);
        }

        internal void Unregister(CampMember m) => members.Remove(m);

        static int TotalAlive()
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++) n += All[i].members.Count;
            return n;
        }

        // ---------- วงจร ----------

        void Update()
        {
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + data.checkInterval;
                Recount();
            }

            if (!SpawningEnabled || gate.State != GateState.Active) return;   // ชาร์จ/ปิดแล้ว = หยุดเติมทันที
            if (!EnemyTarget.Ensure()) return;
            Health ph = EnemyTarget.PlayerHealth;
            if (ph != null && ph.IsDead) return;

            if (!deployed)
            {
                if (!forceDeploy && !PlayerInSpawnRange()) return;
                BeginDeploy();
            }

            if (burstLeaders + burstSwarm > 0) { RunDeploy(); return; }

            Refill();
            Balance();
        }

        bool PlayerInSpawnRange() =>
            Vector2.Distance(transform.position, EnemyTarget.Transform.position) <= data.spawnRadius;

        // นับ: leader ที่ยังอยู่, swarm ที่มีเจ้านาย, ตัวค้าง (ไม่มีเจ้านาย ไม่นับเข้า camp)
        void Recount()
        {
            int l = 0, s = 0, o = 0;
            for (int i = members.Count - 1; i >= 0; i--)
            {
                CampMember m = members[i];
                if (m == null) { members.RemoveAt(i); continue; }

                if (m.IsLeader) l++;
                else if (m.Follower != null)
                {
                    if (m.Follower.Owner != null) s++;
                    else o++;
                }
            }
            leaders = l; swarm = s; orphans = o;
        }

        // ---------- จัดทัพครั้งแรก ----------

        void BeginDeploy()
        {
            deployed = true;
            Recount();
            burstLeaders = leaderPrefab != null ? Mathf.Max(0, data.leaderTarget - leaders) : 0;
            burstSwarm = Mathf.Max(0, data.swarmTarget - swarm);
        }

        // ทยอยเสกต่อเฟรม leader ให้ครบก่อน แล้วค่อย swarm
        void RunDeploy()
        {
            int budget = data.spawnPerFrame;

            for (; budget > 0 && burstLeaders > 0; budget--, burstLeaders--)
                SpawnLeader(LeaderPoint(), true);
            if (burstLeaders > 0) return;

            for (; budget > 0 && burstSwarm > 0; budget--, burstSwarm--)
            {
                LinkSource owner = PickDeployOwner();
                if (owner == null) { burstSwarm = 0; break; }   // ไม่มีที่ผูกแล้ว (gate เต็ม): ที่เหลือให้การเติมทำทีหลัง

                Vector2 around = owner == gate.Source ? GatePoint() : owner.Position + Random.insideUnitCircle * data.deployScatter;
                SpawnSwarm(around, owner, true);
            }
        }

        // leader ที่ฝูงขาดมากสุดก่อน หมดแล้วค่อย gate
        LinkSource PickDeployOwner()
        {
            LinkSource best = null;
            int bestMissing = 0;

            for (int i = 0; i < members.Count; i++)
            {
                CampMember m = members[i];
                if (m == null || !m.IsLeader) continue;

                LinkSource s = m.Source;
                if (s.IsActive && s.CanAccept && s.Missing > bestMissing)
                {
                    best = s;
                    bestMissing = s.Missing;
                }
            }
            if (best != null) return best;

            return gate.Source.CanAccept ? gate.Source : null;
        }

        // ---------- เติมกลับ / ปรับสมดุล ----------

        void Refill()
        {
            if (TotalAlive() >= data.maxAlive || members.Count >= data.maxAlivePerGate) return;

            if (leaderPrefab != null && leaders < data.leaderTarget && Time.time >= nextLeaderTime)
            {
                SpawnLeader(LeaderPoint(), true);
                nextLeaderTime = Time.time + data.leaderRefillInterval;
            }

            // swarm ที่เติมผูกกับ gate ถ้า gate เต็ม (รอให้ leader ขอไปก่อน)
            if (swarm < data.swarmTarget && Time.time >= nextSwarmTime && gate.Source.CanAccept)
            {
                SpawnSwarm(GatePoint(), gate.Source, true);
                nextSwarmTime = Time.time + data.swarmRefillInterval;
            }
        }

        // leader ที่ฝูงต่ำกว่าเป้า ขอจาก gate / leader อื่น (ภายหลังสมอง leader เรียกเองได้ ปิดด้วย autoBalance)
        void Balance()
        {
            if (!data.autoBalance || Time.time < nextBalance) return;
            nextBalance = Time.time + data.balanceInterval;

            for (int i = 0; i < members.Count; i++)
            {
                CampMember m = members[i];
                if (m == null || !m.IsLeader) continue;
                if (m.Source.IsActive && m.Source.Missing > 0) m.Source.RequestMissing();
            }
        }

        // ---------- Final wave ----------

        // gate ระเบิด (ก่อนปิด): หยุดจัดทัพที่ค้าง แล้วเสก leader ก่อน swarm ผูกกับ leader เหล่านั้น
        // ไม่ผูกกับ gate เพราะ gate กำลังปิดและปล่อยผู้ตามทั้งหมด  ไม่ลงทะเบียน camp (camp จบแล้ว ไม่นับ ไม่ติดเพดาน)
        void OnGateExploded()
        {
            burstLeaders = 0;
            burstSwarm = 0;

            if (leaderPrefab == null) return;   // ไม่มี leader = ไม่มีใครให้ swarm ผูก

            Vector2 c = transform.position;
            var finalOwners = new List<LinkSource>();

            for (int i = 0; i < data.finalLeaders; i++)
            {
                LinkSource s = SpawnLeader(c + Random.insideUnitCircle * data.finalScatter, false);
                if (s != null) finalOwners.Add(s);
            }
            if (finalOwners.Count == 0) return;

            for (int i = 0; i < data.finalSwarm; i++)
            {
                LinkSource owner = finalOwners[i % finalOwners.Count];
                SpawnSwarm(c + Random.insideUnitCircle * data.finalScatter, owner, false);
            }
        }

        // ---------- สร้างศัตรู ----------

        Vector2 GatePoint() => (Vector2)transform.position + Random.insideUnitCircle * data.gateScatter;

        Vector2 LeaderPoint()
        {
            Vector2 gp = transform.position;
            Vector2 toPlayer = EnemyTarget.Transform != null
                ? ((Vector2)EnemyTarget.Transform.position - gp).normalized
                : Vector2.zero;
            return gp + toPlayer * data.leaderOffset + Random.insideUnitCircle * data.leaderScatter;
        }

        // คืน LinkSource ของ leader ที่เสก (ใช้ผูก swarm final wave)
        LinkSource SpawnLeader(Vector2 pos, bool register)
        {
            GameObject go = Instantiate(leaderPrefab, pos, Quaternion.identity, enemiesRoot);
            if (register && go.TryGetComponent(out CampMember cm)) cm.Assign(gate);
            if (register) leaders++;
            return go.GetComponent<LinkSource>();
        }

        void SpawnSwarm(Vector2 pos, LinkSource owner, bool register)
        {
            GameObject go = Instantiate(swarmPrefab, pos, Quaternion.identity, enemiesRoot);
            if (register && go.TryGetComponent(out CampMember cm)) cm.Assign(gate);

            if (go.TryGetComponent(out Follower f) && f.SpawnLinkedTo(owner) && register) swarm++;
        }

        // ---------- ใช้โดยแผง debug ----------

        public void ForceDeploy() => forceDeploy = true;

        public void SpawnLeaderNow()
        {
            if (leaderPrefab != null) SpawnLeader(LeaderPoint(), true);
        }

        public int SpawnSwarmNow(int count)
        {
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                if (!gate.Source.CanAccept) break;
                SpawnSwarm(GatePoint(), gate.Source, true);
                n++;
            }
            return n;
        }

        public void ClearMembers()
        {
            foreach (CampMember m in new List<CampMember>(members))
                if (m != null) Destroy(m.gameObject);     // Destroy ไม่ผ่าน Died จึงไม่หักพลังงาน
        }
    }
}