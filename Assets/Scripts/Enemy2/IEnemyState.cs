// สถานะอ่านอย่างเดียวของสมองศัตรู (SwarmBrain / LeaderBrain) ที่เอฟเฟกต์ใช้ดู
// อยู่ใน global namespace (ไม่ใส่ namespace) เพื่อให้สคริปต์เอฟเฟกต์เดิมใช้ได้โดยไม่ต้อง using เพิ่ม
public interface IEnemyState
{
    bool Frozen { get; }          // หลับ/ยังไม่มีเจ้านาย (หนวดขดเบาๆ, glow ปิด, ไม่หัน)
    bool IsWindingUp { get; }     // กำลังง้างก่อนโจมตี
    bool IsChasing { get; }       // กำลังเร่งเข้าหาเป้า (เฟส Chase ของ EnemyChaseFx)
    float WindupRatio { get; }    // 0..1 ความคืบหน้าของการง้าง
}