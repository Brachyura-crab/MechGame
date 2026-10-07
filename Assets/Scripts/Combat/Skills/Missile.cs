using UnityEngine;

public class Missile : MonoBehaviour
{
    [SerializeField] float radius = 0.2f;              // รัศมีตรวจชนที่หัว missile
    [SerializeField] LayerMask hitMask;                // Enemy (ฉากใส่ทีหลัง) ห้ามใส่ Player
    [SerializeField] GameObject explosionPrefab;       // ภาพระเบิดชั่วคราว (ไม่ใส่ก็ได้)
    [SerializeField] float explosionLifetime = 0.5f;   // วินาที ให้เท่าความยาว clip ระเบิด
    [SerializeField] float explosionVisualScale = 1f;  // ตัวคูณขนาดภาพเทียบวงระเบิดจริง (1 = เท่ากัน)

    Vector2 dir;
    float speed, lifeLeft, age;
    float damage, blastRadius, turnRate, homingDelay, searchRadius, fuseDistance;
    StatusData stun;
    float stunDuration, knockbackSpeed;
    MechStats owner;
    Health target;
    bool hasAcquired, exploded;
    float nextSearchTime;
    ContactFilter2D filter;

    static readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];
    static readonly Collider2D[] overlapBuffer = new Collider2D[128];
    static readonly System.Collections.Generic.List<Health> hitList = new System.Collections.Generic.List<Health>();

    void Awake()
    {
        filter = new ContactFilter2D();
        filter.SetLayerMask(hitMask);
        filter.useTriggers = false;
    }

    public void Launch(Vector2 direction, MissileVolleySkill s, MechStats owner)
    {
        dir = direction.normalized;
        transform.right = dir;
        speed = s.speed;
        lifeLeft = s.lifetime;
        damage = s.damage;
        blastRadius = s.blastRadius;
        turnRate = s.turnRate;
        homingDelay = s.homingDelay;
        searchRadius = s.searchRadius;
        fuseDistance = s.fuseDistance;
        this.owner = owner;
        stun = s.stun;
        stunDuration = s.stunDuration;
        knockbackSpeed = s.knockbackSpeed;
    }

    void Update()
    {
        if (exploded) return;
        float dt = Time.deltaTime;
        age += dt;

        // หลังช่วงบินตรงตอนออก เริ่มหาเป้าและเลี้ยว
        if (age >= homingDelay)
        {
            if ((target == null || target.IsDead) && Time.time >= nextSearchTime) AcquireTarget();

            if (target != null && !target.IsDead)
            {
                Vector2 to = (Vector2)target.transform.position - (Vector2)transform.position;

                if (fuseDistance > 0f && to.magnitude <= fuseDistance)
                {
                    Explode(transform.position);
                    return;
                }

                float cur = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                float want = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                float a = Mathf.MoveTowardsAngle(cur, want, turnRate * dt) * Mathf.Deg2Rad;
                dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                transform.right = dir;
            }
        }

        // เคลื่อนที่ + ตรวจชนแบบกวาด
        Vector2 pos = transform.position;
        Vector2 delta = dir * speed * dt;
        float dist = delta.magnitude;

        if (dist > 0f)
        {
            int n = Physics2D.CircleCast(pos, radius, delta / dist, filter, hitBuffer, dist);
            if (n > 0)
            {
                RaycastHit2D best = hitBuffer[0];
                for (int i = 1; i < n; i++)
                    if (hitBuffer[i].distance < best.distance) best = hitBuffer[i];
                Explode(best.centroid);
                return;
            }
        }

        transform.position = pos + delta;
        lifeLeft -= dt;
        if (lifeLeft <= 0f) Explode(transform.position);   // หมดเวลาก็ระเบิด
    }

    // ครั้งแรก: ศัตรูใกล้ผู้เล่นที่สุด / ครั้งต่อไป (เป้าตาย): ศัตรูใกล้ missile ที่สุด
    void AcquireTarget()
    {
        nextSearchTime = Time.time + 0.25f;
        Vector2 center = (!hasAcquired && owner != null)
            ? (Vector2)owner.transform.position
            : (Vector2)transform.position;

        int n = Physics2D.OverlapCircle(center, searchRadius, filter, overlapBuffer);
        Health best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < n; i++)
        {
            Health h = overlapBuffer[i].GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;
            float d = ((Vector2)h.transform.position - center).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = h; }
        }

        target = best;
        if (best != null) hasAcquired = true;
    }

    void Explode(Vector2 point)
    {
        if (exploded) return;
        exploded = true;

        int n = Physics2D.OverlapCircle(point, blastRadius, filter, overlapBuffer);
        hitList.Clear();

        for (int i = 0; i < n; i++)
        {
            Health h = overlapBuffer[i].GetComponentInParent<Health>();
            if (h == null || h.IsDead || hitList.Contains(h)) continue;   // ศัตรูตัวเดียวอาจมีหลาย Collider
            hitList.Add(h);

            DamageCalculator.Deal(new DamageInfo
            {
                baseAmount = damage,
                type = DamageType.Kinetic,
                source = owner != null ? owner.gameObject : null
            }, owner, h);
        }

        // Stun + Knockback ทุกตัวที่โดน (ทิศผลัก = ออกจากจุดระเบิด)
        for (int i = 0; i < hitList.Count; i++)
        {
            Health h = hitList[i];
            if (h.IsDead) continue;

            StatusController sc = h.GetComponent<StatusController>();
            if (sc == null) continue;

            Vector2 away = (Vector2)h.transform.position - point;
            if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle;   // ทับจุดระเบิดพอดี = สุ่มทิศ

            if (stun != null) sc.Apply(stun, owner != null ? owner.gameObject : null, stunDuration);
            sc.Knockback(away, knockbackSpeed);
        }

        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, point, Quaternion.identity);
            fx.transform.localScale = Vector3.one * blastRadius * 2f * explosionVisualScale;
            Destroy(fx, explosionLifetime);
        }

        ExplosionScreenFx.Trigger(point, blastRadius);

        // แยก trail ออกให้จางเองตามเวลาของมัน ไม่หายวับพร้อม missile
        // TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        // if (trail != null && trail.gameObject != gameObject)
        // {
        //     trail.transform.SetParent(null, true);
        //     trail.emitting = false;
        //     Destroy(trail.gameObject, trail.time + 0.1f);
        // }

        Destroy(gameObject);
    }
}