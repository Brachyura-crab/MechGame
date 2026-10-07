using UnityEngine;

[RequireComponent(typeof(MechAim))]
[RequireComponent(typeof(MechStats))]
public class AutoAim : MonoBehaviour
{
    [SerializeField] LayerMask enemyMask;
    [SerializeField] float cursorRadius = 6f;    // เลือกเป้าที่อยู่ใกล้เมาส์ไม่เกินเท่านี้
    [SerializeField] float maxRange = 30f;       // ไกลสุดจากตัวผู้เล่น
    [SerializeField] float stickiness = 0.7f;    // < 1 = เป้าเดิมได้เปรียบ กันล็อกเด้งไปมาระหว่างสองตัว
    [SerializeField] float selectInterval = 0.1f;
    [SerializeField] bool useLead = true;        // ดักยิงตามความเร็วเป้า (ปิดไว้ทดสอบเทียบได้)
    [SerializeField] Transform reticle;          // วงแสดงเป้าที่ล็อก (ไม่ใส่ก็ได้)

    MechAim aim;
    MechStats mech;
    ContactFilter2D filter;

    Health target;
    Vector2 lastPos;
    Vector2 targetVel;
    bool justLocked;

    Rigidbody2D targetRb;
    float lastSampleTime;
    int assistedShots;

    float nextSelect;

    static readonly Collider2D[] buffer = new Collider2D[64];

    public bool Enabled => mech.Stats.Get(StatId.AimAssist) > 0f;

    void Awake()
    {
        aim = GetComponent<MechAim>();
        mech = GetComponent<MechStats>();
        filter = new ContactFilter2D();
        filter.SetLayerMask(enemyMask);
        filter.useTriggers = false;
    }

    void Update()
    {
        if (!Enabled)
        {
            target = null;
        }
        else
        {
            if (Time.time >= nextSelect)
            {
                nextSelect = Time.time + selectInterval;
                Select();
            }

            if (target != null)
            {
                Vector2 pos = target.transform.position;

                if (justLocked)
                {
                    targetRb = target.GetComponent<Rigidbody2D>();
                    targetVel = Vector2.zero;
                    lastPos = pos;
                    lastSampleTime = Time.time;
                    justLocked = false;
                }

                if (targetRb != null)
                {
                    targetVel = targetRb.linearVelocity;   // มี Rigidbody = ใช้ความเร็วจริง
                }
                else if (Time.time - lastSampleTime >= 0.05f)
                {
                    // ไม่มี Rigidbody = วัดจากตำแหน่งที่เปลี่ยนในช่วงเวลาหนึ่ง
                    Vector2 v = (pos - lastPos) / (Time.time - lastSampleTime);
                    targetVel = Vector2.Lerp(targetVel, v, 0.5f);
                    lastPos = pos;
                    lastSampleTime = Time.time;
                }
            }
        }

        if (reticle != null)
        {
            bool show = target != null && !target.IsDead;
            if (reticle.gameObject.activeSelf != show) reticle.gameObject.SetActive(show);
            if (show) reticle.position = target.transform.position;
        }
    }

    void Select()
    {
        Vector2 cursor = aim.AimPoint;
        Vector2 me = transform.position;

        int n = Physics2D.OverlapCircle(cursor, cursorRadius, filter, buffer);
        Health best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < n; i++)
        {
            Health h = buffer[i].GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;

            Vector2 p = h.transform.position;
            if ((p - me).sqrMagnitude > maxRange * maxRange) continue;

            float score = (p - cursor).sqrMagnitude;
            if (h == target) score *= stickiness * stickiness;
            if (score < bestScore) { bestScore = score; best = h; }
        }

        if (best != target)
        {
            target = best;
            justLocked = true;
        }
    }

    // อาวุธเรียกตอนยิง: ได้ทิศที่ยิงไปหาเป้า (ดักยิงถ้าเปิด) คืน false = ไม่มีเป้า ให้ยิงตามเมาส์
    public bool TryGetAimDirection(Vector2 from, float projectileSpeed, float maxFlightTime, out Vector2 dir)
    {
        dir = Vector2.zero;
        if (!Enabled || target == null || target.IsDead) return false;

        Vector2 t = target.transform.position;
        Vector2 point = t;

        if (useLead && Intercept(t - from, targetVel, projectileSpeed, out float time) && time <= maxFlightTime)
            point = t + targetVel * time;

        Vector2 to = point - from;
        if (to.sqrMagnitude < 0.01f) return false;

        assistedShots++;
        Debug.DrawLine(from, point, Color.cyan, 0.6f);   // เส้นที่ยิงไปหาจุดดัก
        Debug.DrawLine(t, point, Color.yellow, 0.6f);    // ระยะที่ดักล่วงหน้าจากตำแหน่งเป้าจริง

        dir = to.normalized;
        return true;
    }

    // แก้สมการ |d + v*t| = speed*t หาเวลาที่กระสุนพบเป้า (เอาค่าบวกที่น้อยสุด)
    static bool Intercept(Vector2 d, Vector2 v, float speed, out float t)
    {
        t = 0f;
        float a = Vector2.Dot(v, v) - speed * speed;
        float b = 2f * Vector2.Dot(d, v);
        float c = Vector2.Dot(d, d);

        if (Mathf.Abs(a) < 0.0001f)
        {
            if (Mathf.Abs(b) < 0.0001f) return false;
            t = -c / b;
            return t > 0f;
        }

        float disc = b * b - 4f * a * c;
        if (disc < 0f) return false;

        float sq = Mathf.Sqrt(disc);
        float t1 = (-b - sq) / (2f * a);
        float t2 = (-b + sq) / (2f * a);

        if (t1 > 0f && (t1 < t2 || t2 <= 0f)) t = t1;
        else if (t2 > 0f) t = t2;
        else return false;
        return true;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(20f, 196f, 700f, 24f),
            "AutoAim stat=" + mech.Stats.Get(StatId.AimAssist)
            + " target=" + (target != null ? target.name : "none")
            + " vel=" + targetVel.ToString("F1")
            + " assistedShots=" + assistedShots);
    }
}