using System.Collections.Generic;
using UnityEngine;

// ผลักศัตรูรอบตัวหุ่นตอนเริ่ม Dash และตอนเริ่ม Boost (ครั้งเดียวต่อครั้ง)
// ศูนย์กลางอยู่ที่ตัวหุ่น ไม่เกี่ยวกับตำแหน่งวงคลื่นภาพ
public class DashShockwave : MonoBehaviour
{
    [System.Serializable]
    public class ShockwaveStyle
    {
        public float radius = 3f;
        public float knockbackSpeed = 12f;   // ก่อนหักค่า knockbackResist ของศัตรู
    }

    [SerializeField] PlayerMove playerMove;
    [SerializeField] Transform centerPoint;   // ว่างไว้ = ใช้ตำแหน่งของ PlayerMove
    [SerializeField] LayerMask hitMask;       // Enemy เท่านั้น ห้ามใส่ Player

    [Tooltip("แกน X = ระยะ/รัศมี (0 ถึง 1), แกน Y = ตัวคูณความแรง")]
    [SerializeField] AnimationCurve falloff = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(1f, 0.3f));

    [SerializeField] ShockwaveStyle dash  = new ShockwaveStyle { radius = 2.5f, knockbackSpeed = 10f };
    [SerializeField] ShockwaveStyle boost = new ShockwaveStyle { radius = 4f,   knockbackSpeed = 16f };
    [SerializeField] ShockwaveStyle ultimate = new ShockwaveStyle { radius = 5f, knockbackSpeed = 18f };

    static readonly Collider2D[] overlapBuffer = new Collider2D[128];
    static readonly List<Health> hitList = new List<Health>();

    ContactFilter2D filter;
    bool wasDashing, wasBoosting;

    void Awake()
    {
        filter = new ContactFilter2D();
        filter.SetLayerMask(hitMask);
        filter.useTriggers = false;
    }

    void LateUpdate()
    {
        if (playerMove == null) return;

        bool dashing = playerMove.IsDashing;
        bool boosting = playerMove.IsBoosting;

        if (dashing && !wasDashing) Push(dash);
        if (boosting && !wasBoosting) Push(boost);

        wasDashing = dashing;
        wasBoosting = boosting;
    }

    Vector2 Center => centerPoint != null ? (Vector2)centerPoint.position
                                          : (Vector2)playerMove.transform.position;

    void Push(ShockwaveStyle style)
    {
        Vector2 center = Center;
        int n = Physics2D.OverlapCircle(center, style.radius, filter, overlapBuffer);
        hitList.Clear();

        for (int i = 0; i < n; i++)
        {
            Health h = overlapBuffer[i].GetComponentInParent<Health>();
            if (h == null || h.IsDead || hitList.Contains(h)) continue;   // ศัตรูตัวเดียวอาจมีหลาย Collider
            hitList.Add(h);

            StatusController sc = h.GetComponent<StatusController>();
            if (sc == null) continue;

            Vector2 away = (Vector2)h.transform.position - center;
            float dist = away.magnitude;
            if (dist < 0.0001f) away = Random.insideUnitCircle;   // ทับตัวหุ่นพอดี = สุ่มทิศ

            float k = falloff.Evaluate(Mathf.Clamp01(dist / style.radius));
            sc.Knockback(away, style.knockbackSpeed * k);
        }
    }

    public void PushUltimate() => Push(ultimate);

    void OnDrawGizmosSelected()
    {
        Vector3 c = centerPoint != null ? centerPoint.position
                  : playerMove != null ? playerMove.transform.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(c, dash.radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(c, boost.radius);
    }
}