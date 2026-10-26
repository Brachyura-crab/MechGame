using System;
using UnityEngine;
using UnityEngine.Rendering;
using EnemyV2;

// ภาพ gate จากภาพที่วาด: ซ้อนหลายชั้น (ล่าง -> บน) แต่ละชั้นมีขอบพลิ้ว/ปื้นโปร่งแสง/glitch ผ่าน shader Gate/RiftLayer
// วางเป็นลูกของ gate (Prefab) และปิดภาพ placeholder เดิม  ถ้าเคยใส่ GateRiftVisual (แบบ mesh) ให้ลบทิ้ง
public class GateRiftLayers : MonoBehaviour
{
    [Serializable]
    public class Layer
    {
        public string name = "Layer";
        public Sprite sprite;
        public bool additive;                                  // true = แสงเรือง (additive) / false = โปร่งแสงปกติ
        [ColorUsage(true, true)] public Color color = Color.white;   // สีคูณ (HDR ได้ ให้ Bloom ช่วย)
        public Vector2 offset;
        public float scale = 1f;

        [Header("ขอบพลิ้ว (บิด UV)")]
        public float warpStrength = 0.02f;                     // ความแรง (หน่วย UV: 0.02 ~ 30 พิกเซลจาก 1500)
        public float warpScale = 3f;                           // ขนาดก้อน (ยิ่งมากยิ่งละเอียด)
        public float warpSpeed = 0.4f;

        [Header("ลายเลื่อนขึ้น")]
        public float driftSpeed = 0.15f;                       // ค่าลบ = เลื่อนลง

        [Header("ปื้นโปร่งแสง")]
        [Range(0f, 1f)] public float erodeMix = 0.5f;          // 0 = ไม่กัดขอบ
        [Range(0f, 1f)] public float erodeAmount = 0.45f;
        public float erodeScale = 5f;
        public float erodeSpeed = 0.2f;
        [Range(0.01f, 0.5f)] public float erodeSoft = 0.2f;
        [Range(0f, 1f)] public float coreProtect = 0.6f;       // กันส่วนทึบตรงกลางไม่ให้โดนกัด

        [Header("การเคลื่อนไหว")]
        public float timeOffset = 0f;                          // หน่วงเวลาของชั้นนี้ (ทำให้ขอบตามกันเป็นระลอก)
        public float swayAmount = 0.05f;                       // แกว่งตำแหน่ง (หน่วยโลก)
        public float swaySpeed = 0.6f;
        public float breathe = 0.03f;                          // หายใจเข้า-ออก (สัดส่วนขนาด)
        public float breatheSpeed = 0.8f;

        [Header("Glitch / ตา")]
        [Range(0f, 2f)] public float glitchMul = 1f;
        public bool followsPlayer = false;                     // ใช้กับชั้นตา

        [NonSerialized] public Transform tf;
        [NonSerialized] public SpriteRenderer sr;
        [NonSerialized] public Material mat;
    }

    [Header("Shader / การวาด")]
    [SerializeField] Shader riftShader;                        // Gate/RiftLayer
    [SerializeField] string sortingLayerName = "Default";
    [SerializeField] int baseOrder = 0;
    [SerializeField] float globalScale = 1f;                   // ขนาดรวม (ผสมกับ Pixels Per Unit ของ sprite)

    [Header("ชั้น (ล่าง -> บน): Crack, Big, Mid, Small, Blob, Eye")]
    [SerializeField] Layer[] layers;

    [Header("การเคลื่อนไหวรวม")]
    [SerializeField] float speed = 1f;
    [SerializeField] float chargeSpeedMul = 2.5f;              // ตอน gate ชาร์จ เร็วขึ้นกี่เท่า

    [Header("Glitch")]
    [SerializeField, Range(0f, 1f)] float baseGlitch = 0.04f;  // glitch เบาๆ ตลอดเวลา
    [SerializeField] Vector2 burstInterval = new Vector2(2.5f, 6f);      // ห่างระหว่างช่วงแรง (วินาที)
    [SerializeField] Vector2 burstDuration = new Vector2(0.12f, 0.35f);
    [SerializeField] float glitchStepHz = 24f;                 // ความถี่ที่ลาย glitch เปลี่ยน
    [SerializeField] float glitchBands = 36f;
    [SerializeField] float glitchShift = 0.06f;                // ระยะแถบเลื่อน (UV)
    [SerializeField] float glitchSplit = 0.006f;               // ระยะแยกสี (UV)
    [SerializeField, Range(0f, 1f)] float glitchDensity = 0.3f;
    [SerializeField] float glitchJitter = 0.08f;               // กระตุกตำแหน่ง (หน่วยโลก)
    [SerializeField, Range(0f, 1f)] float chargeGlitch = 0.45f;
    [SerializeField] float chargeBurstMul = 3f;                // ตอนชาร์จ ช่วงแรงถี่ขึ้นกี่เท่า

    [Header("ตา")]
    [SerializeField] float eyeTravel = 0.35f;                  // ระยะที่ตาขยับตามผู้เล่น (หน่วยโลก)
    [SerializeField] float eyeFollowSpeed = 6f;
    [SerializeField] float eyeFollowRange = 12f;               // ผู้เล่นไกลกว่านี้ = หันเต็มที่
    [SerializeField] float eyeTilt = 6f;                       // เอียงตัวตาไปทางที่มอง (องศา)

    [Header("สถานะ")]
    [SerializeField, Range(0f, 1f)] float outOfRangeDim = 0.5f;
    [SerializeField] Color chargeColor = new Color(1f, 0.3f, 0.2f, 1f);
    [SerializeField, Range(0f, 1f)] float intensity = 1f;

    Gate gate;
    float clock, dim = 1f, chargeT;
    float nextBurst, burstEnd, burstStrength;
    float glitchNow, glitchSeed;
    Vector2 glitchOffset;
    Vector2 eyeDir;

    void Reset()
    {
        layers = new[]
        {
            new Layer { name = "Crack", warpStrength = 0.004f, warpScale = 6f, warpSpeed = 0.6f, driftSpeed = 0f, erodeMix = 0f,
                        swayAmount = 0.02f, breathe = 0.01f, glitchMul = 1.2f, timeOffset = 0f },
            new Layer { name = "Big", warpStrength = 0.022f, warpScale = 3f, warpSpeed = 0.35f, driftSpeed = 0.12f,
                        erodeMix = 0.55f, erodeAmount = 0.42f, erodeScale = 4.5f, swayAmount = 0.06f, breathe = 0.03f, glitchMul = 0.6f, timeOffset = 0.7f },
            new Layer { name = "Mid", warpStrength = 0.03f, warpScale = 3.5f, warpSpeed = 0.4f, driftSpeed = 0.22f,
                        erodeMix = 0.6f, erodeAmount = 0.45f, erodeScale = 5f, swayAmount = 0.08f, breathe = 0.035f, glitchMul = 0.5f, timeOffset = 1.4f },
            new Layer { name = "Small", additive = true, warpStrength = 0.035f, warpScale = 4f, warpSpeed = 0.5f, driftSpeed = 0.3f,
                        erodeMix = 0.5f, erodeAmount = 0.45f, erodeScale = 6f, swayAmount = 0.07f, breathe = 0.04f, glitchMul = 0.4f, timeOffset = 2.1f },
            new Layer { name = "Blob", additive = true, color = new Color(1f, 1f, 1f, 0.9f), warpStrength = 0.02f, warpScale = 2.5f, warpSpeed = 0.3f,
                        driftSpeed = 0.1f, erodeMix = 0.35f, swayAmount = 0.05f, breathe = 0.06f, glitchMul = 0.3f, timeOffset = 2.8f },
            new Layer { name = "Eye", followsPlayer = true, warpStrength = 0f, driftSpeed = 0f, erodeMix = 0f,
                        swayAmount = 0f, breathe = 0f, glitchMul = 0.5f },
        };
#if UNITY_EDITOR
        riftShader = Shader.Find("Gate/RiftLayer");
#endif
    }

    void Start()
    {
        gate = GetComponentInParent<Gate>();

        if (riftShader == null)
        {
            Debug.LogError("GateRiftLayers: ต้องใส่ Rift Shader (Gate/RiftLayer)", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < layers.Length; i++) Build(i);
        nextBurst = Time.time + UnityEngine.Random.Range(burstInterval.x, burstInterval.y);
    }

    void OnDestroy()
    {
        if (layers == null) return;
        foreach (var l in layers) if (l.mat != null) Destroy(l.mat);
    }

    void Build(int index)
    {
        Layer L = layers[index];
        if (L.sprite == null) return;

        var go = new GameObject("Rift_" + L.name);
        go.transform.SetParent(transform, false);
        L.tf = go.transform;

        L.sr = go.AddComponent<SpriteRenderer>();
        L.sr.sprite = L.sprite;
        L.sr.sortingLayerName = sortingLayerName;
        L.sr.sortingOrder = baseOrder + index;

        L.mat = new Material(riftShader) { name = "RiftLayer_" + L.name };
        L.mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        L.mat.SetFloat("_DstBlend", L.additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        L.mat.SetFloat("_Aspect", L.sprite.rect.width / L.sprite.rect.height);
        L.sr.sharedMaterial = L.mat;
    }

    // เรียกจากที่อื่นเพื่อสั่ง glitch แรงทันที (เช่น ตอน gate โดนยิงหรือเริ่มชาร์จ)
    public void TriggerGlitch(float strength = 1f, float duration = 0.3f)
    {
        burstStrength = Mathf.Clamp01(strength);
        burstEnd = Time.time + duration;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        bool charging = gate != null && gate.State == GateState.Charging;
        bool inRange = gate == null || gate.PlayerInRange;
        dim = Mathf.MoveTowards(dim, inRange ? 1f : outOfRangeDim, Time.unscaledDeltaTime * 3f);
        chargeT = Mathf.MoveTowards(chargeT, charging ? 1f : 0f, Time.unscaledDeltaTime * 2f);

        clock += dt * speed * Mathf.Lerp(1f, chargeSpeedMul, chargeT);

        UpdateGlitch();
        UpdateEyeDirection();

        Color tint = Color.Lerp(Color.white, chargeColor, chargeT * 0.85f);
        tint.a = dim * intensity;

        for (int i = 0; i < layers.Length; i++) ApplyLayer(layers[i], tint);
    }

    void UpdateGlitch()
    {
        if (Time.time >= nextBurst)
        {
            burstStrength = UnityEngine.Random.Range(0.5f, 1f);
            burstEnd = Time.time + UnityEngine.Random.Range(burstDuration.x, burstDuration.y);
            nextBurst = Time.time + UnityEngine.Random.Range(burstInterval.x, burstInterval.y) / Mathf.Lerp(1f, chargeBurstMul, chargeT);
        }

        float env = Time.time < burstEnd ? burstStrength : 0f;
        glitchNow = Mathf.Clamp01(Mathf.Max(baseGlitch, env) + chargeT * chargeGlitch);
        glitchSeed = Mathf.Floor(Time.time * glitchStepHz);

        float a = Hash(glitchSeed, 1.3f) - 0.5f;
        float b = Hash(glitchSeed, 9.7f) - 0.5f;
        glitchOffset = new Vector2(a, b) * 2f * glitchJitter * glitchNow;
    }

    void UpdateEyeDirection()
    {
        Vector2 target = Vector2.zero;
        if (EnemyTarget.Ensure())
        {
            Vector2 d = (Vector2)EnemyTarget.Transform.position - (Vector2)transform.position;
            float dist = d.magnitude;
            if (dist > 0.01f) target = d / dist * Mathf.Clamp01(dist / Mathf.Max(0.01f, eyeFollowRange));
        }
        eyeDir = Vector2.Lerp(eyeDir, target, 1f - Mathf.Exp(-eyeFollowSpeed * Time.unscaledDeltaTime));
    }

    void ApplyLayer(Layer L, Color tint)
    {
        if (L.sr == null) return;

        float T = clock + L.timeOffset;

        // ตำแหน่ง / ขนาด / การหมุน
        Vector2 sway = new Vector2(Mathf.Sin(T * L.swaySpeed + L.timeOffset * 3f),
                                   Mathf.Cos(T * L.swaySpeed * 0.8f + 1.3f) * 0.5f) * L.swayAmount;
        Vector2 pos = L.offset + sway + glitchOffset * L.glitchMul;
        float rot = 0f;

        if (L.followsPlayer)
        {
            pos += eyeDir * eyeTravel;
            rot = -eyeDir.x * eyeTilt;
        }

        float sc = L.scale * globalScale * (1f + Mathf.Sin(T * L.breatheSpeed) * L.breathe);
        L.tf.localPosition = new Vector3(pos.x, pos.y, 0f);
        L.tf.localRotation = Quaternion.Euler(0f, 0f, rot);
        L.tf.localScale = new Vector3(sc, sc, 1f);

        // สี (ตาไม่เปลี่ยนเป็นสีแดงตามสถานะ แต่ยังจางตามระยะ)
        Color c = L.followsPlayer ? new Color(1f, 1f, 1f, tint.a) : tint;
        L.sr.color = c;

        // ค่า shader
        var m = L.mat;
        m.SetColor("_Color", L.color);
        m.SetFloat("_T", T);
        m.SetFloat("_WarpStrength", L.warpStrength);
        m.SetFloat("_WarpScale", L.warpScale);
        m.SetFloat("_WarpSpeed", L.warpSpeed);
        m.SetFloat("_DriftSpeed", L.driftSpeed);
        m.SetFloat("_ErodeMix", L.erodeMix);
        m.SetFloat("_ErodeAmount", L.erodeAmount);
        m.SetFloat("_ErodeScale", L.erodeScale);
        m.SetFloat("_ErodeSpeed", L.erodeSpeed);
        m.SetFloat("_ErodeSoft", L.erodeSoft);
        m.SetFloat("_CoreProtect", L.coreProtect);
        m.SetFloat("_Glitch", glitchNow * L.glitchMul);
        m.SetFloat("_GlitchSeed", glitchSeed);
        m.SetFloat("_GlitchBands", glitchBands);
        m.SetFloat("_GlitchShift", glitchShift);
        m.SetFloat("_GlitchSplit", glitchSplit);
        m.SetFloat("_GlitchDensity", glitchDensity);
    }

    static float Hash(float a, float b)
    {
        float v = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }
}