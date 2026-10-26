using System.Collections.Generic;
using UnityEngine;

public class LightningBomb : MonoBehaviour
{
    [SerializeField] float radius = 0.2f;            // รัศมีตรวจชนของตัวระเบิด
    [SerializeField] LayerMask hitMask;              // Enemy (ฉากใส่ทีหลัง) ห้ามใส่ Player
    [SerializeField] GameObject explosionPrefab;     // ภาพระเบิดชั่วคราว
    [SerializeField] float explosionLifetime = 0.5f;   // วินาที ให้เท่าความยาว clip ระเบิด
    [SerializeField] float explosionVisualScale = 1f;  // ตัวคูณขนาดภาพเทียบวงระเบิดจริง (1 = เท่ากัน)

    Vector2 dir;
    float speed, travelLeft, damage, blastRadius;
    StatusData stun, shieldOff;
    float stunDuration, shieldOffDuration;
    MechStats owner;
    bool exploded;
    float slowZone, minSpeed;
    AnimationCurve slowCurve;
    bool explodeOnContact;
    ContactFilter2D filter;

    static readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];
    static readonly List<Health> hitList = new List<Health>();

    void Awake()
    {
        filter = new ContactFilter2D();
        filter.SetLayerMask(hitMask);
        filter.useTriggers = false;
    }

    public void Launch(Vector2 direction, float distance, LightningBombSkill s, MechStats owner)
    {
        dir = direction.normalized;
        transform.right = dir;
        speed = s.speed;
        travelLeft = distance;
        damage = s.damage;
        blastRadius = s.blastRadius;
        stun = s.stun;
        stunDuration = s.stunDuration;
        shieldOff = s.shieldOff;
        shieldOffDuration = s.shieldOffDuration;
        this.owner = owner;
        slowZone = Mathf.Min(s.slowDistance, distance * 0.7f);
        slowCurve = s.slowCurve;
        minSpeed = s.minSpeed;
        explodeOnContact = s.explodeOnContact;
    }

    void Update()
    {
        if (exploded) return;

        float speedNow = speed;
        if (slowCurve != null && slowZone > 0.0001f && travelLeft < slowZone)
            speedNow = Mathf.Max(minSpeed, speed * slowCurve.Evaluate(travelLeft / slowZone));

        float step = Mathf.Min(speedNow * Time.deltaTime, travelLeft);   // ไม่บินเลยจุดเป้า
        Vector2 pos = transform.position;

        if (step > 0f && explodeOnContact)
        {
            int n = Physics2D.CircleCast(pos, radius, dir, filter, hitBuffer, step);
            if (n > 0)
            {
                RaycastHit2D best = hitBuffer[0];
                for (int i = 1; i < n; i++)
                    if (hitBuffer[i].distance < best.distance) best = hitBuffer[i];
                Explode(best.centroid);
                return;
            }
        }

        transform.position = pos + dir * step;
        travelLeft -= step;
        if (travelLeft <= 0.0001f) Explode(transform.position);
    }

    void Explode(Vector2 point)
    {
        if (exploded) return;
        exploded = true;

        AreaDamage.Collect(point, blastRadius, filter, hitList);
        GameObject src = owner != null ? owner.gameObject : null;

        for (int i = 0; i < hitList.Count; i++)
        {
            Health h = hitList[i];

            // ติดสถานะก่อน เพื่อให้ดาเมจของระเบิดเองทะลุโล่เข้า HP
            StatusController sc = h.GetComponent<StatusController>();
            if (sc != null)
            {
                if (shieldOff != null) sc.Apply(shieldOff, src, shieldOffDuration);
                if (stun != null) sc.Apply(stun, src, stunDuration);
            }

            DamageCalculator.Deal(new DamageInfo
            {
                baseAmount = damage,
                type = DamageType.Kinetic,
                source = src
            }, owner, h);
        }

        AreaDamage.SpawnBlastFx(explosionPrefab, point, blastRadius, explosionVisualScale, explosionLifetime);

        // แยก trail ออกให้จางเอง (ถ้าตัวระเบิดมี trail) ไม่หายวับพร้อมตัวระเบิด
        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null && trail.gameObject != gameObject)
        {
            trail.transform.SetParent(null, true);
            trail.emitting = false;
            Destroy(trail.gameObject, trail.time + 0.1f);
        }

        ExplosionScreenFx.Trigger(point, blastRadius, 1f, false);   // จอมืดตามระยะ ไม่สั่น

        Destroy(gameObject);
    }
}