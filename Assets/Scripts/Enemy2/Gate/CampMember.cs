using UnityEngine;

namespace EnemyV2
{
    // ติดที่ศัตรูทุกตัวที่ gate สร้าง (swarm และ leader): สังกัด camp ไหน (ถาวร ไม่เปลี่ยนตามการโอนฝูง) และตายแล้วหักพลังงานเท่าไร
    // ถูกลบ/เก็บโดยไม่ได้ตาย (เช่นถูกลบเพราะไม่มีเจ้านาย) จะไม่นับ
    [RequireComponent(typeof(Health))]
    public class CampMember : MonoBehaviour
    {
        [SerializeField] float weight = 1f;       // พลังงานที่หักเมื่อตัวนี้ตาย (swarm 1, leader 5–10)
        [SerializeField] Gate initialGate;        // เฉพาะตัวที่วางในฉากเพื่อทดสอบ

        public Gate Gate { get; private set; }
        public float Weight => weight;

        public Camp Camp { get; private set; }
        public Follower Follower { get; private set; }   // มี = swarm
        public LinkSource Source { get; private set; }   // มี (และไม่มี Follower) = leader
        public bool IsLeader => Source != null && Follower == null;

        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
            TryGetComponent(out Follower f); Follower = f;
            TryGetComponent(out LinkSource s); Source = s;
        }

        void OnEnable()
        {
            health.Died += OnDied;
            health.Damaged += OnDamaged;
        }
        void OnDisable()
        {
            health.Died -= OnDied;
            health.Damaged -= OnDamaged;
            Release();   // ถูกลบ/เก็บโดยไม่ได้ตาย: ไม่นับพลังงาน
        }

        void Start()
        {
            if (initialGate != null && Gate == null) Assign(initialGate);
        }

        public void Assign(Gate g)
        {
            Release();
            Gate = g;
            Camp = g != null ? g.GetComponent<Camp>() : null;
            if (Camp != null) Camp.Register(this);
        }

        void Release()
        {
            if (Camp != null) Camp.Unregister(this);
            Camp = null;
            Gate = null;
        }

        // สมาชิกถูกยิง = camp alert (ผ่าน CampCommander ถ้ามี)
        void OnDamaged(DamageResult r)
        {
            if (Camp == null) return;
            CampCommander cc = Camp.GetComponent<CampCommander>();
            if (cc != null) cc.Alert(transform.position);
        }

        void OnDied()
        {
            Gate g = Gate;
            if (g == null) return;
            Release();                          // นับครั้งเดียว
            g.OnMemberKilled(this);               // ให้ gate ตัดสินว่านับพลังงานหรือไม่
        }
    }
}