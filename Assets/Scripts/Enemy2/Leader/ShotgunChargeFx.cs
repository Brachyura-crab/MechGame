using UnityEngine;

// effect ชาร์จกระสุนของ leader: sprite กระสุนทุกนัดอยู่ที่ปากกระบอก (ทับกันจุดเดียว) หมุนเป็นพัดตามมุมกระจายของปืน
//   ง้าง: fade ชัดขึ้นทีละนัดตาม WindupRatio จนครบ (เหลื่อมกัน) แล้วค้างครบทุกนัดก่อนยิง
//   ยิง: วาบขยายแล้วหาย | ท่าถูกยกเลิก: fade ออกเร็ว | ตาย: หายทันที
// ทางเดียว ShotgunAttack → Fx ไม่กระทบ gameplay
// ใส่บน root ของ leader และให้ sprite เป็นลูกของ root (ไม่ใช่ลูกของ Visual ที่ถูก flip) ภาพกระสุนวาดหันขวา (+X)
[DisallowMultipleComponent]
[RequireComponent(typeof(ShotgunAttack), typeof(Health))]
public class ShotgunChargeFx : MonoBehaviour
{
    [Header("Bullets (เรียงตามลำดับที่ fade ขึ้น)")]
    [SerializeField] SpriteRenderer[] bullets;
    [SerializeField] Transform muzzle;   // จุดปล่อยที่วางใน hierarchy (ว่าง = ใช้จุดจริงของปืน)

    [Header("Charge (หน่วย = สัดส่วนของ windup 0..1)")]
    [SerializeField, Range(0.05f, 1f)] float fadeSpan = 0.3f;    // แต่ละนัดใช้เวลา fade กี่ส่วนของ windup
    [SerializeField, Range(0.3f, 1f)] float fullAt = 0.9f;       // นัดสุดท้ายชัดเต็มเมื่อถึงตรงนี้ (ที่เหลือก่อนยิงคือครบทุกนัดค้างไว้)
    [SerializeField, Range(0.3f, 1f)] float scaleFrom = 0.7f;    // ขนาดตอนเริ่มโผล่ (คูณขนาดเดิม)

    [Header("Fire / Cancel")]
    [SerializeField] float flashTime = 0.12f;
    [SerializeField] float flashScale = 0.4f;        // ขยายเพิ่มตอนวาบ
    [SerializeField] float cancelFadeSpeed = 8f;     // alpha/วินาที ตอนท่าถูกยกเลิก

    [Header("Bloom (ไม่บังคับ)")]
    [SerializeField] string intensityProperty = "";  // ชื่อ property Float ใน material (เช่น _Intensity) ว่าง = ไม่ใช้
    [SerializeField] float peakIntensity = 2.5f;     // ความเข้มตอนชัดเต็ม (>1 เพื่อให้ Bloom จับ)

    ShotgunAttack shotgun;
    Health health;
    MaterialPropertyBlock mpb;
    Color[] baseColor;
    Vector3[] baseScale;
    float[] alpha;
    float[] scale;
    float[] intensity;
    int intensityId;
    bool useIntensity;
    float flashT;
    bool dead;

    void Awake()
    {
        shotgun = GetComponent<ShotgunAttack>();
        health = GetComponent<Health>();

        if (bullets == null || bullets.Length == 0)
        {
            Debug.LogError("ShotgunChargeFx: ต้องใส่ Bullets (sprite กระสุนเรียงตามลำดับที่จะ fade)", this);
            enabled = false;
            return;
        }

        int n = bullets.Length;
        baseColor = new Color[n];
        baseScale = new Vector3[n];
        alpha = new float[n];
        scale = new float[n];
        intensity = new float[n];

        for (int i = 0; i < n; i++)
        {
            if (bullets[i] == null) continue;
            baseColor[i] = bullets[i].color;
            baseScale[i] = bullets[i].transform.localScale;
        }

        if (shotgun.Pellets > 0 && shotgun.Pellets != n)
            Debug.LogWarning($"ShotgunChargeFx: จำนวน sprite ({n}) ไม่เท่า pellets ของปืน ({shotgun.Pellets}) พัดจะไม่ตรงกับกระสุนจริง", this);

        if (!string.IsNullOrEmpty(intensityProperty))
        {
            intensityId = Shader.PropertyToID(intensityProperty);
            Material m = bullets[0] != null ? bullets[0].sharedMaterial : null;
            useIntensity = m != null && m.HasProperty(intensityId);
            if (useIntensity) mpb = new MaterialPropertyBlock();
            else Debug.LogWarning($"ShotgunChargeFx: material ไม่มี property '{intensityProperty}' จึงไม่ปรับความเข้ม", this);
        }
    }

    void OnEnable()
    {
        dead = false;
        flashT = 0f;
        HideAll();
        health.Died += OnDied;
        shotgun.Fired += OnFired;
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
        if (shotgun != null) shotgun.Fired -= OnFired;
    }

    void OnDied()
    {
        dead = true;
        HideAll();
    }

    void OnFired(Vector2 dir)
    {
        if (!dead) flashT = flashTime;
    }

    void LateUpdate()
    {
        if (dead) return;

        float dt = Time.deltaTime;
        int n = bullets.Length;

        if (shotgun.IsWindingUp)
        {
            flashT = 0f;
            float r = shotgun.WindupRatio;
            float stagger = n > 1 ? Mathf.Max(0f, fullAt - fadeSpan) / (n - 1) : 0f;

            for (int i = 0; i < n; i++)
            {
                float t = Mathf.Clamp01((r - i * stagger) / fadeSpan);
                alpha[i] = t * t * (3f - 2f * t);                       // smoothstep
                scale[i] = Mathf.Lerp(scaleFrom, 1f, alpha[i]);
                intensity[i] = Mathf.Lerp(1f, peakIntensity, alpha[i]);
            }
        }
        else if (flashT > 0f)
        {
            flashT -= dt;
            float f = Mathf.Clamp01(flashT / Mathf.Max(0.01f, flashTime));   // 1 → 0
            for (int i = 0; i < n; i++)
            {
                alpha[i] = f;
                scale[i] = 1f + (1f - f) * flashScale;
                intensity[i] = peakIntensity;
            }
        }
        else
        {
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                alpha[i] = Mathf.MoveTowards(alpha[i], 0f, cancelFadeSpeed * dt);
                scale[i] = Mathf.Lerp(scaleFrom, 1f, alpha[i]);
                intensity[i] = Mathf.Lerp(1f, peakIntensity, alpha[i]);
                if (alpha[i] > 0.001f) any = true;
            }
            if (!any) { HideAll(); return; }
        }

        Apply();
    }

    void Apply()
    {
        int n = bullets.Length;

        Vector2 o = muzzle != null ? (Vector2)muzzle.position : shotgun.MuzzlePoint;
        Vector2 aim = shotgun.Aim;
        float aimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float spread = shotgun.SpreadDegrees;
        float step = n > 1 ? spread / (n - 1) : 0f;
        float z = transform.position.z;

        for (int i = 0; i < n; i++)
        {
            SpriteRenderer sr = bullets[i];
            if (sr == null) continue;

            float a = alpha[i];
            sr.enabled = a > 0.001f;
            if (!sr.enabled) continue;

            // ทุกนัดอยู่จุดเดียวกัน (ปากกระบอก) หมุนเป็นพัดเหมือนที่ Fire() ยิงจริง
            float ang = aimAngle - spread * 0.5f + step * i;
            sr.transform.SetPositionAndRotation(new Vector3(o.x, o.y, z), Quaternion.Euler(0f, 0f, ang));
            sr.transform.localScale = baseScale[i] * scale[i];

            Color c = baseColor[i];
            c.a = baseColor[i].a * a;
            sr.color = c;

            if (useIntensity)
            {
                sr.GetPropertyBlock(mpb);
                mpb.SetFloat(intensityId, intensity[i]);
                sr.SetPropertyBlock(mpb);
            }
        }
    }

    void HideAll()
    {
        if (bullets == null) return;
        for (int i = 0; i < bullets.Length; i++)
        {
            if (alpha != null) alpha[i] = 0f;
            if (bullets[i] != null) bullets[i].enabled = false;
        }
    }
}