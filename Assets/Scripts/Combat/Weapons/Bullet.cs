using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Transform visual;              // ลูกที่เป็น sprite เส้นกระสุน
    [SerializeField] float streakLength = 1.2f;     // ความยาวเส้น (ภาพเท่านั้น ไม่เกี่ยวกับการชน)
    [SerializeField] float streakThickness = 0.06f;
    [SerializeField] float radius = 0.08f;          // รัศมีตรวจชนที่หัวกระสุน
    [SerializeField] LayerMask hitMask;             // ใส่ Enemy (และฉากในอนาคต) ห้ามใส่ Player

    [HideInInspector] public Bullet poolKey;

    Vector2 velocity;
    float lifeLeft;
    float damage;
    float critChance;
    DamageType type;
    MechStats owner;

    GameObject impactEffect;
    float impactLifetime;
    int impactOrder;

    float traveled;   // ระยะที่หัวกระสุนบินไปแล้ว
    float length;     // ความยาวเส้นปัจจุบัน (ภาพ)
    bool hitStop;     // ชนแล้ว: หัวหยุด หางไล่ตามเข้ามา

    int hitsLeft;
    readonly System.Collections.Generic.List<Health> alreadyHit = new System.Collections.Generic.List<Health>();
    ContactFilter2D filter;
    static readonly RaycastHit2D[] buffer = new RaycastHit2D[16];

    void Awake()
    {
        filter = new ContactFilter2D();
        filter.SetLayerMask(hitMask);
        filter.useTriggers = false;
    }

    void SetLength(float len)
    {
        visual.localScale = new Vector3(len, streakThickness, 1f);
        visual.localPosition = new Vector3(-len * 0.5f, 0f, 0f); // หัวอยู่ที่จุดกลางของ object หางยื่นไปด้านหลัง
    }

    public void Fire(Vector2 pos, Vector2 dir, WeaponData w, MechStats owner)
    {
        transform.position = pos;
        transform.right = dir;
        velocity = dir * w.bulletSpeed;
        lifeLeft = w.bulletLifetime;
        damage = w.damage;
        critChance = w.critChance;
        type = w.damageType;
        this.owner = owner;
        traveled = 0f;
        hitStop = false;
        length = 0.001f;
        SetLength(length);
        alreadyHit.Clear();
        hitsLeft = w.maxHits < 0 ? int.MaxValue : w.maxHits;
        impactEffect = w.impactEffect;
        impactLifetime = w.impactLifetime;
        impactOrder = w.impactSortingOrder;
    }

    void StopAt(RaycastHit2D hit)
    {
        transform.position = hit.centroid;
        traveled += hit.distance;
        length = Mathf.Min(streakLength, traveled);
        SetLength(length);
        hitStop = true;
    }

    void SpawnImpact(RaycastHit2D hit)
    {
        if (impactEffect == null) return;

        Vector2 n = hit.normal;
        if (n.sqrMagnitude < 0.01f) n = -velocity.normalized;   // กระสุนเริ่มทับ collider พอดี ไม่มี normal
        float angle = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;

        EffectPool.Spawn(impactEffect, hit.point, Quaternion.Euler(0f, 0f, angle),
                         impactLifetime, null, impactOrder);
    }

    static void SortByDistance(int n)
    {
        for (int i = 1; i < n; i++)
        {
            RaycastHit2D key = buffer[i];
            int j = i - 1;
            while (j >= 0 && buffer[j].distance > key.distance)
            {
                buffer[j + 1] = buffer[j];
                j--;
            }
            buffer[j + 1] = key;
        }
    }

        void Update()
    {
        float dt = Time.deltaTime;

        if (hitStop)
        {
            // หัวหยุดที่จุดชน หางไล่ตามเข้ามาจนหมดเส้น
            length -= velocity.magnitude * dt;
            if (length <= 0f) { BulletPool.Release(this); return; }
            SetLength(length);
            return;
        }

        Vector2 pos = transform.position;
        Vector2 delta = velocity * dt;
        float dist = delta.magnitude;

        if (dist > 0f)
        {
            int n = Physics2D.CircleCast(pos, radius, delta / dist, filter, buffer, dist);
            SortByDistance(n);

            for (int i = 0; i < n; i++)
            {
                Health h = buffer[i].collider.GetComponentInParent<Health>();

                if (h == null) { SpawnImpact(buffer[i]); StopAt(buffer[i]); return; }
                if (alreadyHit.Contains(h)) continue;           // ตัวนี้โดนไปแล้ว (กำลังทะลุผ่าน)

                alreadyHit.Add(h);
                DamageCalculator.Deal(new DamageInfo
                {
                    baseAmount = damage,
                    type = type,
                    source = owner != null ? owner.gameObject : null,
                    critChance = critChance
                }, owner, h);

                SpawnImpact(buffer[i]);

                hitsLeft--;
                if (hitsLeft <= 0) { StopAt(buffer[i]); return; }
            }
        }

        transform.position = pos + delta;
        traveled += dist;
        length = Mathf.Min(streakLength, traveled);
        SetLength(length);

        lifeLeft -= dt;
        if (lifeLeft <= 0f) BulletPool.Release(this);
    }
}