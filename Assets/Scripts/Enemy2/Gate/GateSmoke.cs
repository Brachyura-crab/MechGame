using UnityEngine;
using EnemyV2;

/// ควันหลายเส้นเหนือ Gate (สร้าง SpriteRenderer ลูกให้เองตอน Start)
/// วางวัตถุนี้เป็นลูกของ Gate ตรงตำแหน่ง "ยอด" ของ Gate
public class GateSmoke : MonoBehaviour
{
    [System.Serializable]
    public class Strand
    {
        public string name = "Strand";
        [ColorUsage(true, true)] public Color color = Color.white;
        [Tooltip("ติ๊ก = เรืองแสง (Additive), ไม่ติ๊ก = ทึบ (Alpha) ใช้กับเส้นสีเข้ม")]
        public bool additive = false;
        [Tooltip("จุดเริ่ม เทียบกับวัตถุนี้ หน่วย = เท่าของ Gate Height")]
        public Vector2 startOffset = Vector2.zero;
        [Tooltip("ความกว้างของพื้นที่ควัน (เท่าของ Gate Height)")]
        public float width = 0.8f;
        [Tooltip("ความสูง (เท่าของ Gate Height) 1 = เท่า Gate")]
        public float heightMul = 1f;
        public float baseWidth = 0.3f;
        public float tipWidth = 0.1f;
        public float swayAmount = 0.12f;
        public float swaySpeed = 0.8f;
        public float rise = 0.35f;
        [Range(0f, 1f)] public float fadeStart = 0.1f;   // เริ่มจางที่ความสูงเท่าไหร่
        [Range(0f, 1f)] public float fadeEnd = 0.9f;     // จางเต็มที่ที่ความสูงเท่าไหร่
        [Range(0.2f, 4f)] public float fadeCurve = 1f;   // >1 อยู่นานแล้วหายเร็วตอนท้าย, <1 จางเร็วตอนต้น
        [Range(0f, 1f)] public float fadeNoise = 0.3f;   // ขอบการจางไม่เรียบ
        [Range(0f, 1f)] public float tipAlpha = 0.15f;     // ความทึบที่ปลาย (จางลงตามความสูง)
        [Range(0, 1)] public float seed = 0f;
    }

    [SerializeField] Shader smokeShader;
    [Tooltip("ความสูงของ Gate ใน world unit (ควัน 1 เท่า = ค่านี้)")]
    [SerializeField] float gateHeight = 3f;
    [SerializeField] string sortingLayerName = "Default";
    [SerializeField] int baseOrder = 0;

    [Header("ตอน Gate กำลังชาร์จ")]
    [SerializeField] float chargeSpeedBoost = 1.5f;   // เร็วขึ้นสูงสุดกี่เท่า (บวกเพิ่ม)
    [SerializeField] float chargeSwayBoost = 1.0f;    // แกว่งแรงขึ้น
    [SerializeField] float chargeHeightBoost = 0.25f; // สูงขึ้น

    [SerializeField] Strand[] strands;

    class Rt { public SpriteRenderer sr; public Material mat; public float t; }
    Rt[] rt;
    Gate gate;
    static Sprite whiteSprite;

    void Reset()
    {
        strands = new[]
        {
            new Strand { name = "Dark",  color = new Color(0.12f, 0.02f, 0.30f, 0.95f), additive = false,
                         startOffset = new Vector2(0f, 0f),       seed = 0.1f },
            new Strand { name = "Mid",   color = new Color(0.55f, 0.20f, 0.80f, 0.9f),  additive = true,
                         startOffset = new Vector2(-0.06f, -0.12f), seed = 0.5f },
            new Strand { name = "Light", color = new Color(0.95f, 0.65f, 0.95f, 0.9f),  additive = true,
                         startOffset = new Vector2(0.06f, -0.24f),  seed = 0.9f },
        };
    }

    void Start()
    {
        gate = GetComponentInParent<Gate>();

        if (smokeShader == null) smokeShader = Shader.Find("Gate/RiftSmoke");
        if (smokeShader == null)
        {
            Debug.LogError("GateSmoke: ไม่พบ shader Gate/RiftSmoke (ใส่ในช่อง Smoke Shader)");
            enabled = false;
            return;
        }

        if (whiteSprite == null)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            // pivot ล่างกลาง, 1 unit = 4 px => ขนาด 1x1, ขยายขึ้นจากโคน
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 4f,
                                        0, SpriteMeshType.FullRect);
        }

        rt = new Rt[strands.Length];
        for (int i = 0; i < strands.Length; i++)
        {
            var s = strands[i];
            var go = new GameObject("Smoke_" + s.name);
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSprite;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = baseOrder + i;

            var mat = new Material(smokeShader);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", s.additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sr.sharedMaterial = mat;

            rt[i] = new Rt { sr = sr, mat = mat, t = s.seed * 10f };
        }
    }

    void Update()
    {
        if (rt == null) return;

        float charge = gate != null ? Mathf.Clamp01(gate.ChargeRatio) : 0f;
        float speedMul = 1f + charge * chargeSpeedBoost;
        float swayMul = 1f + charge * chargeSwayBoost;
        float heightMulC = 1f + charge * chargeHeightBoost;

        for (int i = 0; i < rt.Length; i++)
        {
            var s = strands[i];
            var r = rt[i];

            r.t += Time.deltaTime * speedMul;

            float h = gateHeight * s.heightMul * heightMulC;
            float w = gateHeight * s.width;

            var tr = r.sr.transform;
            tr.localPosition = new Vector3(s.startOffset.x * gateHeight, s.startOffset.y * gateHeight, 0f);
            tr.localScale = new Vector3(w, h, 1f);

            var m = r.mat;
            m.SetColor("_Color", s.color);
            m.SetFloat("_T", r.t);
            m.SetFloat("_Seed", s.seed);
            m.SetFloat("_Aspect", w / Mathf.Max(h, 0.001f));
            m.SetFloat("_Rise", s.rise);
            m.SetFloat("_SwayAmp", s.swayAmount * swayMul);
            m.SetFloat("_SwaySpeed", s.swaySpeed);
            m.SetFloat("_BaseWidth", s.baseWidth);
            m.SetFloat("_TipWidth", s.tipWidth);
            m.SetFloat("_TipAlpha", s.tipAlpha);
            m.SetFloat("_FadeStart", s.fadeStart);
            m.SetFloat("_FadeEnd", s.fadeEnd);
            m.SetFloat("_FadePower", s.fadeCurve);
            m.SetFloat("_FadeNoise", s.fadeNoise);
        }
    }

    void OnDestroy()
    {
        if (rt == null) return;
        foreach (var r in rt) if (r != null && r.mat != null) Destroy(r.mat);
    }
}