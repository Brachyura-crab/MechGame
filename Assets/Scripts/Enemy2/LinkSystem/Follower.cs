using UnityEngine;

namespace EnemyV2
{
    public enum FollowerState
    {
        Dormant,     // ไม่มีเจ้านาย: นิ่ง ไม่ทำอะไร
        Waking,      // เพิ่งได้เจ้านาย: กำลังตื่น (ยังไม่รับคำสั่ง)
        Linked,      // มีเจ้านาย อยู่ในระยะ: รับคำสั่งและทำตามได้
        Recalling    // มีเจ้านาย แต่ไกลเกินไป: ทิ้งคำสั่ง เดินกลับหาเจ้านาย
    }

    // ติดที่ swarm: จำเจ้านาย คำสั่งปัจจุบัน และสถานะตามระยะ
    // ตัวนี้ไม่เคลื่อนที่เอง: สมองของ swarm อ่าน State / Order / Owner แล้วตัดสินว่าจะทำอย่างไร
    public class Follower : MonoBehaviour
    {
        [SerializeField] LinkProfile profile;

        public LinkSource Owner { get; private set; }
        public FollowerState State { get; private set; } = FollowerState.Dormant;
        public LinkOrder Order { get; private set; } = LinkOrder.Hold();
        public float Awake01 { get; private set; }      // 0 หลับ .. 1 ตื่นเต็ม (ไว้ใช้กับภาพ)
        public float OrphanTime { get; private set; }   // วินาทีที่ไม่มีเจ้านาย
        public bool IsDead => dead;
        public bool CanAct => State == FollowerState.Linked;
        public Vector2 Position => transform.position;

        // เจ้านายเปลี่ยน (ได้ใหม่ / โอน / หลุด) camp ใช้ฟังเพื่อนับจำนวน
        public event System.Action<Follower> OwnerChanged;

        Health health;
        Camera cam;
        bool dead;
        float thinkTimer;

        #if UNITY_EDITOR
            [Header("Debug (อ่านอย่างเดียว)")]
            [SerializeField] string debugOwner;
            [SerializeField] string debugOrder;
        #endif

        void Awake()
        {
            TryGetComponent(out health);   // Health ไม่บังคับ (หุ่นทดสอบไม่ต้องมี)

            if (profile == null)
            {
                Debug.LogError("Follower: ต้องใส่ LinkProfile", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            dead = false;
            State = FollowerState.Dormant;
            Order = LinkOrder.Hold();
            Awake01 = 0f;
            OrphanTime = 0f;
            thinkTimer = Random.value * profile.thinkInterval;   // สุ่มเฟส กันทุกตัวคิดพร้อมกัน

            if (health != null) health.Died += OnDied;
        }

        void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
            DetachFromOwner();
        }

        void OnDied()
        {
            dead = true;
            DetachFromOwner();
        }

        // ---------- ใช้โดย LinkSource ----------

        internal void OnAttached(LinkSource s)
        {
            bool wasOwned = Owner != null;
            Owner = s;
            Order = LinkOrder.Hold();
            OrphanTime = 0f;

            if (wasOwned)
            {
                State = FollowerState.Linked;   // โอนจากเจ้านายเดิม: ตื่นอยู่แล้ว ไม่ต้องรอ
                ApplyRange();                   // ไกลเกินวงของเจ้านายใหม่ = ถูกเรียกกลับทันที
            }
            else State = FollowerState.Waking;

            OwnerChanged?.Invoke(this);
        }

        internal void OnDetached(LinkSource s)
        {
            if (Owner != s) return;
            ClearOwner();
        }

        // ---------- ใช้โดยสมอง/ผู้สั่ง ----------

        // ผู้สั่งส่งคำสั่ง: รับได้เฉพาะตอน Linked (ตื่นแล้วและอยู่ในระยะ)
        public bool TryOrder(LinkOrder order)
        {
            if (dead || State != FollowerState.Linked) return false;
            Order = order;
            return true;
        }

        // gate เสกแล้วผูกเลย: ไม่ต้องรอตื่น
        public bool SpawnLinkedTo(LinkSource s)
        {
            Awake01 = 1f;
            return s != null && s.TryAttach(this);
        }

        // ---------- วงจร ----------

        void Update()
        {
            if (dead) return;

            #if UNITY_EDITOR
                    debugOwner = Owner != null ? Owner.name : "(ไม่มี)";
                    debugOrder = Order.type.ToString();
            #endif

            float dt = Time.deltaTime;
            bool owned = Owner != null;

            if (!owned && State != FollowerState.Dormant) ClearOwner();   // เจ้านายถูกทำลายไปเงียบๆ

            // ตื่น/หลับค่อยๆ (Waking -> Linked/Recalling เมื่อตื่นเต็ม)
            float target = owned ? 1f : 0f;
            float time = owned ? profile.wakeTime : profile.sleepTime;
            Awake01 = Mathf.MoveTowards(Awake01, target, dt / Mathf.Max(0.01f, time));
            if (State == FollowerState.Waking && Awake01 >= 1f) ApplyRange();

            if (!owned) OrphanTime += dt;

            thinkTimer -= dt;
            if (thinkTimer > 0f) return;
            thinkTimer += profile.thinkInterval;

            Think(owned);
        }

        void Think(bool owned)
        {
            if (owned)
            {
                if (State != FollowerState.Waking) ApplyRange();
                return;
            }

            // ไม่มีเจ้านาย: หาผู้สั่งที่ระยะเชื่อมครอบคลุม ไม่เจอก็ดูว่าถึงเวลาลบหรือยัง
            LinkSource s = LinkRegistry.FindCovering(Position);
            if (s != null && s.TryAttach(this)) return;

            if (ShouldDespawn()) Destroy(gameObject);   // Destroy ไม่ผ่าน Died จึงไม่นับเป็นการฆ่า
        }

        // Linked <-> Recalling ตามระยะจากเจ้านาย (มี hysteresis: เรียกกลับที่ recallRatio, ปล่อยที่ releaseRatio)
        void ApplyRange()
        {
            if (Owner == null) return;

            LinkProfile op = Owner.Profile;
            float cmd = op.commandRadius;
            float d = Vector2.Distance(Owner.Position, Position);
            bool tooFar = d >= cmd * op.recallRatio;
            bool close = d <= cmd * op.releaseRatio;

            if (State == FollowerState.Waking)
            {
                State = tooFar ? FollowerState.Recalling : FollowerState.Linked;
            }
            else if (State == FollowerState.Linked && tooFar)
            {
                State = FollowerState.Recalling;
                Order = LinkOrder.Hold();   // ทิ้งคำสั่งเดิม
            }
            else if (State == FollowerState.Recalling && close)
            {
                State = FollowerState.Linked;
            }
        }

        // ตัวที่ไม่มีเจ้านาย ไม่มีใครมาเชื่อม: ลบเมื่อผ่านเวลา + ไกลผู้เล่น + นอกจอ (ครบทุกเงื่อนไข)
        bool ShouldDespawn()
        {
            if (OrphanTime < profile.orphanLifetime) return false;

            if (EnemyTarget.Ensure())
            {
                float r = profile.orphanDespawnDistance;
                Vector2 pp = EnemyTarget.Transform.position;
                if ((pp - Position).sqrMagnitude < r * r) return false;
            }

            return !IsOnScreen();
        }

        bool IsOnScreen()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return false;

            Vector3 vp = cam.WorldToViewportPoint(transform.position);
            float m = profile.offscreenMargin;
            return vp.x > -m && vp.x < 1f + m && vp.y > -m && vp.y < 1f + m;
        }

        // ---------- ภายใน ----------

        void DetachFromOwner()
        {
            LinkSource o = Owner;
            if (o != null) o.Detach(this);                                // ฝั่ง LinkSource จะเรียก OnDetached กลับมา
            else if (State != FollowerState.Dormant) ClearOwner();
        }

        void ClearOwner()
        {
            Owner = null;
            State = FollowerState.Dormant;
            Order = LinkOrder.Hold();
            OrphanTime = 0f;
            OwnerChanged?.Invoke(this);
        }

        #if UNITY_EDITOR
            // เส้นเชื่อมไปหาเจ้านาย: ฟ้า = gate, เหลือง = leader (เปิด Gizmos ใน Scene/Game view)
            void OnDrawGizmos()
            {
                if (!Application.isPlaying || Owner == null) return;
                Gizmos.color = Owner.GetComponent<Gate>() != null ? Color.cyan : Color.yellow;
                Gizmos.DrawLine(transform.position, Owner.transform.position);
            }
        #endif
    }
}