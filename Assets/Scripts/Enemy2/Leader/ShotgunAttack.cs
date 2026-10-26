using System;
using UnityEngine;

[RequireComponent(typeof(MechStats), typeof(Health))]
public class ShotgunAttack : MonoBehaviour
{
    [SerializeField] ShotgunAttackData data;

    public float Range => data.range;
    public float WindupMoveScale => data.windupMoveScale;
    public float RecoilSpeed => data.recoilSpeed;
    public float SpreadDegrees => data != null ? data.spreadDegrees : 0f;   // ไว้ให้ effect วางพัดตามมุมจริง
    public int Pellets => data != null ? data.pellets : 0;
    public Vector2 MuzzlePoint => (Vector2)transform.position + Aim * (data != null ? data.muzzleOffset : 0f);   // จุดที่กระสุนออก

    public bool IsWindingUp { get; private set; }
    public float WindupRatio { get; private set; }   // 0..1 ไว้ทำ telegraph (หนวดชู/กะพริบ)
    public Vector2 Aim { get; private set; } = Vector2.right;
    public bool Ready => !IsWindingUp && Time.time >= nextReadyTime && EnemyMotion.CanAttack(status);

    public event Action<Vector2> Fired;   // ส่งทิศที่ยิง

    MechStats mech;
    Health health;
    StatusController status;
    Transform target;
    float windupStart;
    float nextReadyTime;

    void Awake()
    {
        mech = GetComponent<MechStats>();
        health = GetComponent<Health>();
        status = GetComponent<StatusController>();

        if (data == null || data.bulletPrefab == null)
        {
            Debug.LogError("ShotgunAttack: ต้องใส่ Data และ Data ต้องมี Bullet Prefab", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        IsWindingUp = false;
        WindupRatio = 0f;
        // ดีเลย์เริ่มต้นสุ่ม กันตัวใหญ่หลายตัวยิงพร้อมกันตั้งแต่เกิด
        if (data != null) nextReadyTime = Time.time + UnityEngine.Random.Range(0.5f, Mathf.Max(0.6f, data.cooldown * 0.5f));
    }

    public bool TryStart(Transform t)
    {
        if (!Ready || t == null) return false;

        target = t;
        IsWindingUp = true;
        windupStart = Time.time;
        WindupRatio = 0f;
        AimAtTarget();
        return true;
    }

    // ยกเลิกท่า (เช่น ถูกบีบให้ถอย) พักสั้นๆ กันเริ่มซ้ำทันที
    public void Cancel() => CancelInternal(true);

    void CancelInternal(bool penalty)
    {
        if (!IsWindingUp) return;
        IsWindingUp = false;
        WindupRatio = 0f;
        if (penalty) nextReadyTime = Time.time + data.cooldown * 0.25f;
    }

    void Update()
    {
        if (!IsWindingUp) return;

        if (health.IsDead || target == null) { CancelInternal(false); return; }
        if (!EnemyMotion.CanAttack(status)) { CancelInternal(true); return; }   // โดน stun กลางท่า = ยกเลิก

        float t = (Time.time - windupStart) / data.windupTime;
        WindupRatio = Mathf.Clamp01(t);

        if (WindupRatio < data.aimLockRatio) AimAtTarget();   // ล็อกทิศช่วงท้าย ให้ผู้เล่นหลบได้
        if (t >= 1f) Fire();
    }

    void AimAtTarget()
    {
        Vector2 d = (Vector2)target.position - (Vector2)transform.position;
        if (d.sqrMagnitude > 0.0001f) Aim = d.normalized;
    }

    void Fire()
    {
        Vector2 origin = (Vector2)transform.position + Aim * data.muzzleOffset;
        int n = data.pellets;
        float step = n > 1 ? data.spreadDegrees / (n - 1) : 0f;
        float start = -data.spreadDegrees * 0.5f;

        for (int i = 0; i < n; i++)
        {
            float jitter = UnityEngine.Random.Range(-0.15f, 0.15f) * step;   // ให้พัดไม่เรียบเกินไป
            Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, start + step * i + jitter) * (Vector3)Aim);

            Bullet b = BulletPool.Get(data.bulletPrefab);
            b.Fire(origin, dir, data.pellet, mech);
        }

        IsWindingUp = false;
        WindupRatio = 0f;
        nextReadyTime = Time.time + data.cooldown / Mathf.Max(0.1f, mech.Stats.Get(StatId.FireRate));

        Fired?.Invoke(Aim);
    }
}