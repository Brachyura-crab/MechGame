using UnityEngine;

// ขยับหนวดแบบโซ่ท่อนซ้อนกัน: Anchor → Seg_0 → Seg_1 → ... (แต่ละท่อนเป็นลูกของท่อนก่อน)
// โค้ดหมุนเฉพาะ localRotation ของแต่ละท่อน ตำแหน่งข้อต่อคงที่จากที่วางใน prefab
// ท่าทาง: โบกเบา ๆ + ปลายตามช้ากว่าโคน + ลากตามการเคลื่อนที่ + ขดตอน windup + สะบัดตอนโจมตี + ห้อยตอนหลับ/ตาย
[DefaultExecutionOrder(50)]   // ทำงานหลัง EnemyFacing (flip/หมุนเสร็จก่อนแล้วค่อยคำนวณหนวด)
[DisallowMultipleComponent]
public class TentacleController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform visual;            // ลูกที่ถูกหมุนและ flip (ใช้แปลงความเร็วเป็นแกนของตัว)
    [SerializeField] Transform[] anchors;         // จุดยึดหนวดแต่ละเส้น (ว่าง = หาลูกของ visual ที่ชื่อขึ้นต้น "Anchor")
    [SerializeField] string segmentPrefix = "Seg";// ชื่อขึ้นต้นของท่อน (กันไปสับสนกับชิ้นเรืองแสงที่เป็นลูก)
    [SerializeField] SpriteRenderer cullRef;      // sprite ลำตัว: อยู่นอกจอไม่ต้องอัปเดตหนวด

    [Header("Sway (โบกตอนปกติ)")]
    [SerializeField] float swayAmp = 8f;          // องศาต่อท่อน
    [SerializeField] float swayFreq = 1.2f;       // รอบ/วินาที
    [SerializeField] float swayDelay = 0.6f;      // เฟสที่หน่วงต่อท่อน (เรเดียน) ยิ่งมากคลื่นยิ่งวิ่งไปปลายชัด
    [SerializeField] float tipBoost = 0.15f;      // ปลายแกว่งแรงกว่าโคนเท่าไร (ต่อท่อน)

    [Header("Follow (ปลายตามช้ากว่าโคน)")]
    [SerializeField] float follow = 14f;          // ยิ่งมากยิ่งตามทัน
    [SerializeField] float tipLag = 0.5f;         // ปลายตามช้าลงต่อท่อน

    [Header("Move (ตอนเคลื่อนที่)")]
    [SerializeField] float speedAmp = 0.06f;      // ความเร็วเพิ่มแอมพลิจูดโบกเท่าไร
    [SerializeField] float speedFreq = 0.08f;     // ความเร็วเพิ่มความถี่เท่าไร
    [SerializeField] float dragGain = 1.5f;       // องศาต่อท่อนที่เบนตามความเร็วด้านข้าง
    [SerializeField] float dragMax = 12f;
    [SerializeField] float velSmooth = 6f;        // หน่วงความเร็วที่ใช้คำนวณหนวด (ยิ่งน้อยยิ่งนุ่ม ยิ่งมากยิ่งไว) 0 = ไม่หน่วง

    [Header("Coil / Strike")]
    [SerializeField] float coilDeg = 22f;         // องศาต่อท่อนที่ขดเมื่อ coil = 1
    [SerializeField] float strikeKick = 12f;      // แรงสะบัดตอน Strike (หน่วยของ curl/วินาที)
    [SerializeField] float exciteAmp = 1.5f;      // โบกแรงขึ้นเท่าไรหลัง Strike
    [SerializeField] float springK = 70f;
    [SerializeField] float springDamp = 9f;

    [Header("Asleep / Dead")]
    [SerializeField, Range(0f, 1f)] float asleepSway = 0.25f;  // ตัวที่หลับ (Frozen) โบกเบาลงเหลือเท่านี้
    [SerializeField, Range(0f, 1f)] float asleepCurl = 0.3f;   // ห้อยลงเล็กน้อย
    [SerializeField, Range(0f, 1f)] float deadCurl = 0.6f;     // ตายแล้วห้อย/ขดลง
    [SerializeField] float deadSettleTime = 2f;                // ตายแล้วนิ่งนานเท่านี้หยุดอัปเดต
    [SerializeField, Range(0f, 1f)] float deadSpread = 1f;     // ตายแล้วขดออกด้านนอกให้แยกกัน (0 = ทิศเดียวกันเหมือนเดิม, 1 = แยกเต็มที่)
    [SerializeField, Range(0f, 0.5f)] float deadVariation = 0.3f;  // สุ่มความแรงการขดต่อเส้นตอนตาย ± เท่านี้

    class Chain
    {
        public Transform[] segs;
        public float[] baseZ;
        public float[] angle;    // โบก + ขด (ทำให้นุ่มแล้ว)
        public float[] drag;     // ส่วนที่เบนตามความเร็ว (แยกเก็บ เพื่อกลับเครื่องหมายตอน flip)
        public float phase;
        public float dirSign;    // +1 ชี้ไปทางขวาของตัว, -1 ชี้ซ้าย
        public float curlSign;   // ทิศการขด
        public float outSign;    // ทิศขดออกด้านนอก (ใช้ตอนตาย)
        public float deadScale;  // ความแรงขดตอนตายของเส้นนี้
        public Vector2 perp;     // ทิศที่ปลายหนวดเคลื่อนเมื่อหมุนบวก (ตั้งฉากกับทิศหนวด)
    }

    Chain[] chains;
    Rigidbody2D rb;
    Health health;
    IEnemyState behavior;
    ShotgunAttack shotgun;

    float time;
    float curl, curlVel;
    float coilInput, excite;
    bool dead;
    float deadTimer;
    Vector2 smoothVel;
    bool wasMirrored;

    // ---- API สำหรับสคริปต์โจมตีเรียก ----
    public void SetCoil(float v) => coilInput = Mathf.Clamp01(v);   // 0..1 ตอน windup ส่งค่าทุกเฟรม
    public void Strike() { curlVel -= strikeKick; excite = 1f; coilInput = 0f; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInParent<Health>();
        TryGetComponent(out behavior);
        TryGetComponent(out shotgun);
        if (visual == null) visual = transform;

        if (anchors == null || anchors.Length == 0)
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in visual.GetComponentsInChildren<Transform>(true))
                if (c.name.StartsWith("Anchor")) list.Add(c);
            anchors = list.ToArray();
        }

        chains = new Chain[anchors.Length];
        for (int a = 0; a < anchors.Length; a++)
        {
            var segs = new System.Collections.Generic.List<Transform>();
            Transform t = anchors[a];
            while (true)
            {
                Transform next = null;
                foreach (Transform c in t)
                    if (segmentPrefix.Length == 0 || c.name.StartsWith(segmentPrefix)) { next = c; break; }
                if (next == null) break;
                segs.Add(next);
                t = next;
            }

            float z = anchors[a].localEulerAngles.z * Mathf.Deg2Rad;
            var ch = new Chain
            {
                segs = segs.ToArray(),
                baseZ = new float[segs.Count],
                angle = new float[segs.Count],
                drag = new float[segs.Count],
                phase = (a * 2.1f) + Random.value * Mathf.PI * 2f,   // สุ่มต่อตัว ไม่ให้โบกพร้อมกันทั้งฝูง
                dirSign = Mathf.Cos(z) >= 0f ? 1f : -1f,
                curlSign = Mathf.Sin(z) > 0.01f ? 1f : Mathf.Sin(z) < -0.01f ? -1f : (a % 2 == 0 ? 1f : -1f),
                perp = new Vector2(-Mathf.Sin(z), Mathf.Cos(z)),
                outSign = 1f,
                deadScale = 1f
            };
            for (int i = 0; i < segs.Count; i++) ch.baseZ[i] = segs[i].localEulerAngles.z;
            chains[a] = ch;
        }

        ComputeOutwardSigns();
    }

    // ทิศขดออกด้านนอกของแต่ละเส้น (ใช้ตอนตาย ให้หนวดแยกจากกัน ไม่รวบทิศเดียว)
    // ดูมุมที่เบี่ยงจากค่าเฉลี่ยของทุกเส้นก่อน ถ้ามุมเท่ากันหมดดูจากตำแหน่งจุดยึด ถ้ายังเท่ากันสลับตามลำดับ
    void ComputeOutwardSigns()
    {
        int n = anchors.Length;
        if (n == 0) return;

        float sx = 0f, sy = 0f;
        Vector3 center = Vector3.zero;
        for (int a = 0; a < n; a++)
        {
            float z = anchors[a].localEulerAngles.z * Mathf.Deg2Rad;
            sx += Mathf.Cos(z);
            sy += Mathf.Sin(z);
            center += anchors[a].localPosition;
        }
        float meanZ = Mathf.Atan2(sy, sx) * Mathf.Rad2Deg;
        center /= n;

        for (int a = 0; a < n; a++)
        {
            Chain ch = chains[a];
            float diff = Mathf.DeltaAngle(meanZ, anchors[a].localEulerAngles.z);

            float sign;
            if (Mathf.Abs(diff) > 3f) sign = Mathf.Sign(diff);   // เบี่ยงไปทางไหน ขดออกไปทางนั้น
            else
            {
                Vector3 outward = anchors[a].localPosition - center;
                float side = outward.x * ch.perp.x + outward.y * ch.perp.y;
                sign = Mathf.Abs(side) > 0.01f ? Mathf.Sign(side) : (a % 2 == 0 ? 1f : -1f);
            }

            ch.outSign = sign;
            ch.deadScale = 1f + Random.Range(-deadVariation, deadVariation);
        }
    }

    void OnEnable()
    {
        dead = false;
        deadTimer = 0f;
        curl = curlVel = coilInput = excite = 0f;
        smoothVel = Vector2.zero;
        wasMirrored = IsMirrored();
        foreach (Chain ch in chains)
            for (int i = 0; i < ch.angle.Length; i++) { ch.angle[i] = 0f; ch.drag[i] = 0f; }

        if (health != null) health.Died += OnDied;
        if (shotgun != null) shotgun.Fired += OnShotgunFired;
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
        if (shotgun != null) shotgun.Fired -= OnShotgunFired;
    }

    void OnDied() => dead = true;
    void OnShotgunFired(Vector2 dir) => Strike();

    // ภาพถูกสะท้อนอยู่หรือไม่ (flip แกนใดแกนหนึ่ง ไม่ว่าจะหมุนอยู่กี่องศา)
    bool IsMirrored()
    {
        Vector3 x = visual.TransformVector(Vector3.right);
        Vector3 y = visual.TransformVector(Vector3.up);
        return x.x * y.y - x.y * y.x < 0f;
    }

    void LateUpdate()
    {
        if (!dead && health != null && health.IsDead) dead = true;

        if (dead)
        {
            deadTimer += Time.deltaTime;
            if (deadTimer > deadSettleTime) { enabled = false; return; }
        }
        else if (cullRef != null && !cullRef.isVisible) return;   // อยู่นอกจอ ข้าม

        float dt = Time.deltaTime;
        bool asleep = !dead && behavior != null && behavior.Frozen;

        // flip: ภาพสะท้อนทันที แต่ส่วนที่เบนตามความเร็วถูกนิยามตามโลก จึงกลับเครื่องหมายเพื่อให้ท่าต่อเนื่อง (ไม่เด้ง)
        bool mirrored = IsMirrored();
        if (mirrored != wasMirrored)
        {
            wasMirrored = mirrored;
            foreach (Chain ch in chains)
                for (int i = 0; i < ch.drag.Length; i++) ch.drag[i] = -ch.drag[i];
        }

        if (shotgun != null) coilInput = shotgun.IsWindingUp ? shotgun.WindupRatio : 0f;

        // เป้าหมายการขด: ตาย > หลับ > coil จากสคริปต์โจมตี
        float curlTarget = dead ? deadCurl : asleep ? asleepCurl : coilInput;
        curlVel += ((curlTarget - curl) * springK - curlVel * springDamp) * dt;
        curl += curlVel * dt;

        excite = Mathf.MoveTowards(excite, 0f, dt * 2f);

        // ความเร็วที่ใช้คำนวณหนวดถูกหน่วง กันกระตุกตอนเร่ง/ลดความเร็วแรงๆ (แรงถอยตอนยิง, ผลักแยกตัว)
        Vector2 worldVel = rb != null ? rb.linearVelocity : Vector2.zero;
        float vk = velSmooth > 0f ? 1f - Mathf.Exp(-dt * velSmooth) : 1f;
        smoothVel = Vector2.Lerp(smoothVel, worldVel, vk);

        float speed = smoothVel.magnitude;
        Vector3 local = visual.InverseTransformVector(smoothVel);   // ความเร็วในแกนของตัว (รวม flip แล้ว)

        float swayMul = dead ? 0f : asleep ? asleepSway : 1f;
        float ampMul = Mathf.Min(3f, swayMul * (1f + speed * speedAmp + excite * exciteAmp));
        float dragMul = (dead || asleep) ? 0f : 1f;

        time += dt * swayFreq * (1f + speed * speedFreq) * Mathf.PI * 2f;

        foreach (Chain ch in chains)
        {
            int n = ch.segs.Length;

            // ตายแล้วขดออกด้านนอกตามการกระจายของหนวด (deadSpread) แทนทิศเดียวกันหมด
            float sign = dead ? Mathf.Lerp(ch.curlSign, ch.outSign, deadSpread) * ch.deadScale : ch.curlSign;

            for (int i = 0; i < n; i++)
            {
                float wave = Mathf.Sin(time + ch.phase - i * swayDelay) * swayAmp * (1f + i * tipBoost) * ampMul;
                float dragTarget = Mathf.Clamp(-(local.x * ch.perp.x + local.y * ch.perp.y) * dragGain, -dragMax, dragMax) * dragMul;
                float desired = wave + curl * coilDeg * sign;

                float k = 1f - Mathf.Exp(-dt * follow / (1f + i * tipLag));
                ch.angle[i] += (desired - ch.angle[i]) * k;
                ch.drag[i] += (dragTarget - ch.drag[i]) * k;

                ch.segs[i].localRotation = Quaternion.Euler(0f, 0f, ch.baseZ[i] + ch.angle[i] + ch.drag[i]);
            }
        }
    }
}