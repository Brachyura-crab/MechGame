using UnityEngine;

namespace EnemyV2
{
    // หุ่น swarm สำหรับทดสอบแกน link (ลบทิ้งได้เมื่อเสร็จ): ขยับตาม State/Order ของ Follower แบบง่ายที่สุด
    // ไม่ใช้ Rigidbody/Collider  สีบอกสถานะ: เทา=ไม่มีเจ้านาย เหลือง=กำลังตื่น เขียว=รับคำสั่งได้ แดง=ถูกเรียกกลับ
    [RequireComponent(typeof(Follower))]
    public class DebugFollowerMover : MonoBehaviour
    {
        [SerializeField] float speed = 6f;
        [SerializeField] float recallSpeedMultiplier = 1.5f;
        [SerializeField] float holdRadius = 3f;       // Hold: อยู่ในรัศมีนี้รอบเจ้านาย
        [SerializeField] float attackStopDistance = 1.5f;
        [SerializeField] float blockOffset = 2f;      // Block: ยืนห่างจากผู้ถูกป้องกันไปทางภัยเท่านี้
        [SerializeField] bool move = true;

        Follower follower;
        SpriteRenderer sr;

        public static Color StateColor(FollowerState s)
        {
            switch (s)
            {
                case FollowerState.Waking: return new Color(1f, 0.9f, 0.2f, 1f);
                case FollowerState.Linked: return new Color(0.3f, 1f, 0.4f, 1f);
                case FollowerState.Recalling: return new Color(1f, 0.3f, 0.3f, 1f);
                default: return new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        void Awake()
        {
            follower = GetComponent<Follower>();
            TryGetComponent(out sr);
        }

        void Update()
        {
            Tint();

            if (!move) return;

            if (follower.State == FollowerState.Recalling)
            {
                MoveToward(follower.Owner.Position, 0.5f, speed * recallSpeedMultiplier);
                return;
            }

            if (!follower.CanAct) return;   // หลับ/กำลังตื่น: นิ่ง

            LinkOrder o = follower.Order;
            bool needsTarget = o.type == LinkOrderType.Attack || o.type == LinkOrderType.Assist || o.type == LinkOrderType.Block;
            if (needsTarget && o.target == null) o = LinkOrder.Hold();   // เป้าหมายหายไป = Hold

            switch (o.type)
            {
                case LinkOrderType.Hold:
                    MoveToward(follower.Owner.Position, holdRadius, speed);
                    break;

                case LinkOrderType.Guard:
                    MoveToward(o.point, Mathf.Max(0.5f, o.radius), speed);
                    break;

                case LinkOrderType.Attack:
                case LinkOrderType.Assist:
                    MoveToward(o.target.position, attackStopDistance, speed);
                    break;

                case LinkOrderType.Block:
                    {
                        Vector2 p = o.target.position;
                        Vector2 toThreat = o.threat != null ? (Vector2)o.threat.position - p : new Vector2(1f, 0f);
                        MoveToward(p + toThreat.normalized * blockOffset, 0.3f, speed);
                        break;
                    }

                case LinkOrderType.Retreat:
                    MoveToward(o.point, 0.3f, speed);
                    break;
            }
        }

        void MoveToward(Vector2 goal, float stopDistance, float spd)
        {
            Vector2 pos = transform.position;
            if (Vector2.Distance(pos, goal) <= stopDistance) return;

            Vector2 next = Vector2.MoveTowards(pos, goal, spd * Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        void Tint()
        {
            if (sr == null) return;
            Color c = StateColor(follower.State);
            c.a = Mathf.Lerp(0.35f, 1f, follower.Awake01);
            sr.color = c;
        }
    }
}