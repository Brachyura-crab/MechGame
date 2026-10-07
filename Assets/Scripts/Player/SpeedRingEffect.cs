using UnityEngine;
using UnityEngine.Serialization;

// สร้างวงคลื่นตอนเริ่ม Dash และตอนเริ่ม Boost (ครั้งเดียวต่อครั้ง)
public class SpeedRingEffect : MonoBehaviour
{
    [System.Serializable]
    public class RingStyle
    {
        public float scale = 1f;                                   // คูณกับ scale ของ prefab
        public Color color = new Color(0.8f, 0.8f, 0.8f, 1f);      // เทาอ่อน ต่ำกว่า Threshold ของ Bloom
        public float backOffset = 0.3f;                            // ถอยหลังจากจุดเกิดตามทิศพุ่ง
    }

    [SerializeField] PlayerMove playerMove;
    [SerializeField] Transform spawnPoint;      // จุดเกิด (เช่น torso) ว่างไว้ = ใช้ตำแหน่งของ object นี้
    [SerializeField] GameObject ringPrefab;
    [SerializeField] float lifetime = 1f;       // วินาที ให้เท่ากับความยาว clip
    [SerializeField] AnimationCurve fadeCurve = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f));   // แกน X = เวลา 0–1, แกน Y = ความทึบ
    [SerializeField] int sortingOrder = -5;     // ต้องอยู่ใต้หุ่น (-1 / 1) แต่เหนือพื้น

    [SerializeField] RingStyle dash  = new RingStyle { scale = 0.5f, backOffset = 0.3f };
    [SerializeField] RingStyle boost = new RingStyle { scale = 0.8f, backOffset = 0.3f };

    bool wasDashing, wasBoosting;

    void LateUpdate()
    {
        if (playerMove == null) return;

        bool dashing = playerMove.IsDashing;
        bool boosting = playerMove.IsBoosting;

        if (dashing && !wasDashing) Spawn(dash);
        if (boosting && !wasBoosting) Spawn(boost);

        wasDashing = dashing;
        wasBoosting = boosting;
    }

    void Spawn(RingStyle style)
    {
        if (ringPrefab == null) return;

        Vector2 dir = playerMove.DashDirection;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 pos = origin - (Vector3)(dir * style.backOffset);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;   // ภาพต้นฉบับถือว่าหันไป +X

        GameObject go = Instantiate(ringPrefab, pos, Quaternion.Euler(0f, 0f, angle));
        go.transform.localScale = ringPrefab.transform.localScale * style.scale;

        SpriteRenderer[] renderers = go.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.color = style.color;
            sr.sortingOrder = sortingOrder;
        }

        StartCoroutine(Fade(renderers, style.color));
        Destroy(go, lifetime);   // เก็บไว้เป็นตัวสำรอง ถ้า component ถูกปิดกลางทาง วงยังถูกลบ
    }

    System.Collections.IEnumerator Fade(SpriteRenderer[] renderers, Color baseColor)
    {
        float t = 0f;
        while (t < lifetime)
        {
            Color c = baseColor;
            c.a = baseColor.a * fadeCurve.Evaluate(t / lifetime);
            foreach (SpriteRenderer sr in renderers)
                if (sr != null) sr.color = c;

            t += Time.deltaTime;
            yield return null;
        }
    }
}