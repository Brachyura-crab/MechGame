using UnityEngine;

namespace EnemyV2
{
    public enum LinkOrderType
    {
        Hold,      // ไม่มีคำสั่งเฉพาะ: อยู่ใกล้เจ้านาย
        Guard,     // ประจำที่จุด point ในรัศมี radius
        Attack,    // เข้าโจมตี target
        Block,     // เข้าไปบัง target (ผู้ถูกป้องกัน) จาก threat (ภัย)
        Assist,    // ไปช่วย target (ผู้สั่งหรือฝูงอื่น)
        Retreat    // ถอยไปจุด point
    }

    // คำสั่งจากผู้สั่งถึงผู้ตาม: บอกแค่ "ทำอะไรกับใคร/ที่ไหน" ส่วน "ทำอย่างไร" เป็นหน้าที่สมองของ swarm
    public struct LinkOrder
    {
        public LinkOrderType type;
        public Transform target;     // เป้าหมายที่เคลื่อนที่ได้ (ถ้าไม่มีใช้ point)
        public Transform threat;     // ใช้กับ Block
        public Vector2 point;
        public float radius;

        public Vector2 TargetPosition => target != null ? (Vector2)target.position : point;

        public static LinkOrder Hold() => new LinkOrder { type = LinkOrderType.Hold };

        public static LinkOrder Guard(Vector2 point, float radius) =>
            new LinkOrder { type = LinkOrderType.Guard, point = point, radius = radius };

        public static LinkOrder Attack(Transform target) =>
            new LinkOrder { type = LinkOrderType.Attack, target = target };

        public static LinkOrder Block(Transform protectedTarget, Transform threat) =>
            new LinkOrder { type = LinkOrderType.Block, target = protectedTarget, threat = threat };

        public static LinkOrder Assist(Transform ally) =>
            new LinkOrder { type = LinkOrderType.Assist, target = ally };

        public static LinkOrder Retreat(Vector2 point) =>
            new LinkOrder { type = LinkOrderType.Retreat, point = point };

        public override string ToString() => type.ToString();
    }
}