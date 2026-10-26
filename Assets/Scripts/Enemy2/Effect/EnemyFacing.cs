using UnityEngine;

public enum FacingMode
{
    Rotate,   // หมุนตามทิศเต็ม 360° + flip แกน Y เมื่อหันซ้าย (swarm)
    Tilt      // ไม่หมุน: หันซ้าย/ขวาด้วย flip แกน X แล้วเอียงหัวขึ้น/ลงเล็กน้อยตามทิศ (leader)
}

// คุมการหันของ sprite (ภาพวาดหันขวา +X) ใช้ได้กับทั้ง swarm และ leader
// หมุน/ปรับเฉพาะ visual ไม่แตะ root จึงไม่กระทบ Rigidbody, collider หรือแถบเลือด
[DisallowMultipleComponent]
public class EnemyFacing : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] Transform visual;

    [Header("Mode")]
    [SerializeField] FacingMode mode = FacingMode.Rotate;

    [Header("Direction source")]
    [SerializeField] float minMoveSpeed = 0.3f;       // ช้ากว่านี้ถือว่าหยุด คงทิศเดิม
    [SerializeField] float facePlayerDistance = 5f;   // ใกล้ผู้เล่นกว่านี้หันหาผู้เล่นแทนทิศเดิน (0 = ใช้ทิศเดินอย่างเดียว)
        [SerializeField] float holdAfterFire = 0.5f;      // หลังยิง เมินทิศจากความเร็ว (แรงถีบ) กี่วินาที จะไม่ flip กลับหลัง

    [Header("Rotate mode")]
    [SerializeField] float turnSpeed = 540f;          // องศา/วินาที

    [Header("Tilt mode")]
    [SerializeField] float maxTilt = 15f;             // เอียงหัวสูงสุดทั้งขึ้นและลง (องศา)
    [SerializeField] float tiltRatio = 0.35f;         // ทิศแนวตั้งเต็ม (90°) แปลงเป็นเอียงเท่าไร
    [SerializeField] float tiltSpeed = 120f;          // องศา/วินาที ความเร็วในการเอียง
    [SerializeField] Transform counterRotate;                  // กลุ่มที่ไม่อยากให้เอียงตามตัว (เช่น TentacleRoot)
    [SerializeField, Range(0f, 1f)] float counterRatio = 1f;   // 1 = ชี้ตรงลงตลอด, 0.5 = เอียงตามตัวครึ่งหนึ่ง

    [Header("Flip")]
    [Range(0f, 0.5f)]
    [SerializeField] float flipDeadZone = 0.1f;       // กันสลับถี่ตอนทิศอยู่ตรงขึ้น/ลง

    // Rotate: มุมของภาพ  Tilt: มุมเอียง (บวก = หัวเงยขึ้น เมื่อหันขวา)
    public float AngleDeg => angle;
    // Rotate: flip แกน Y อยู่  Tilt: หันซ้ายอยู่
    public bool Flipped => flipped;
    public Vector2 Forward
    {
        get
        {
            float r = angle * Mathf.Deg2Rad;
            if (mode == FacingMode.Rotate) return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
            return new Vector2(flipped ? -Mathf.Cos(r) : Mathf.Cos(r), Mathf.Sin(r));
        }
    }

    Rigidbody2D rb;
    Health health;
    IEnemyState behavior;
    ShotgunAttack shotgun;
    float holdUntil;
    Vector3 baseScale;
    float angle;
    bool flipped;
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInParent<Health>();
        TryGetComponent(out behavior);
        TryGetComponent(out shotgun);
        if (visual == null) visual = transform;
        baseScale = visual.localScale;
    }

    void OnEnable()
    {
        dead = false;
        if (health != null) health.Died += OnDied;
        holdUntil = 0f;
        if (shotgun != null) shotgun.Fired += OnFired;

        Transform t = EnemyTarget.Transform;
        Vector2 toPlayer = t != null ? (Vector2)t.position - (Vector2)transform.position : Vector2.right;

        if (mode == FacingMode.Rotate)
        {
            angle = AngleTo(toPlayer);
            flipped = Mathf.Cos(angle * Mathf.Deg2Rad) < 0f;
        }
        else
        {
            angle = 0f;
            flipped = toPlayer.x < 0f;
        }
        Apply();
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
        if (shotgun != null) shotgun.Fired -= OnFired;
    }

    void OnDied() => dead = true;

    void OnFired(Vector2 dir) => holdUntil = Time.time + holdAfterFire;

    void LateUpdate()
    {
        if (dead || (health != null && health.IsDead)) return;   // ศพคงท่าเดิม

        bool has = TryGetDesiredDir(out Vector2 dir);

        if (mode == FacingMode.Rotate)
        {
            if (has) angle = Mathf.MoveTowardsAngle(angle, AngleTo(dir), turnSpeed * Time.deltaTime);

            // flip ตามมุมปัจจุบันของภาพ (ไม่ใช่มุมเป้าหมาย) ตัวจึงตั้งตรงเสมอระหว่างหมุน
            float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
            if (cos < -flipDeadZone) flipped = true;
            else if (cos > flipDeadZone) flipped = false;
        }
        else
        {
            float targetTilt = 0f;
            if (has)
            {
                if (dir.x < -flipDeadZone) flipped = true;
                else if (dir.x > flipDeadZone) flipped = false;

                // เงยตามทิศขึ้น ก้มตามทิศลง ไม่ว่าจะหันซ้ายหรือขวา
                float vertical = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
                targetTilt = Mathf.Clamp(vertical * tiltRatio, -maxTilt, maxTilt);
            }
            angle = Mathf.MoveTowards(angle, targetTilt, tiltSpeed * Time.deltaTime);
        }

        Apply();
    }

    // ทิศที่อยากหัน (ปกติคือ normalized) false = ไม่มีทิศใหม่
    bool TryGetDesiredDir(out Vector2 dir)
    {
        dir = Vector2.zero;

        // ตัวที่หลับ (ไม่มี leader) อยู่นิ่ง ไม่หันตามอะไรทั้งนั้น
        if (behavior != null && behavior.Frozen) return false;

        Vector2 pos = transform.position;

        Transform t = EnemyTarget.Transform;
        if (facePlayerDistance > 0f && t != null)
        {
            Vector2 to = (Vector2)t.position - pos;
            float sqr = to.sqrMagnitude;
            if (sqr <= facePlayerDistance * facePlayerDistance && sqr > 0.0001f)
            {
                dir = to.normalized;
                return true;
            }
        }

        if (rb != null && Time.time >= holdUntil)
        {
            Vector2 v = rb.linearVelocity;
            if (v.sqrMagnitude > minMoveSpeed * minMoveSpeed)
            {
                dir = v.normalized;
                return true;
            }
        }

        return false;
    }

    static float AngleTo(Vector2 d) => Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

    void Apply()
    {
        if (mode == FacingMode.Rotate)
        {
            visual.rotation = Quaternion.Euler(0f, 0f, angle);
            visual.localScale = new Vector3(baseScale.x, flipped ? -baseScale.y : baseScale.y, baseScale.z);
        }
        else
        {
            // flip แกน X เพื่อหันซ้าย; เมื่อหันซ้ายต้องกลับเครื่องหมายมุมเพื่อให้หัวเงยขึ้นจริง
            float sign = flipped ? -1f : 1f;
            visual.rotation = Quaternion.Euler(0f, 0f, angle * sign);
            visual.localScale = new Vector3(baseScale.x * sign, baseScale.y, baseScale.z);
            if (counterRotate != null)
            counterRotate.localRotation = Quaternion.Euler(0f, 0f, -angle * counterRatio);
        }
    }
}