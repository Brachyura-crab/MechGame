using System;
using UnityEngine;
using EnemyV2;

// ภาพ gate เฟส 1: กรอบหยัก + ชั้นด้านใน (เปลี่ยนรูปช้าๆ) + ลูกตาที่หันตามผู้เล่น  สร้าง mesh ด้วยโค้ดทั้งหมด
// วางเป็นลูกของ gate (Prefab) แล้วปิด SpriteRenderer เดิม
public class GateRiftVisual : MonoBehaviour
{
    [Serializable]
    public class Layer
    {
        public string name = "Layer";
        public bool additive = false;                  // true = ใช้ Additive Material (แสงฟุ้ง) / false = ใช้ Alpha Material (สีเรียบทึบ)
        public Color color = Color.white;              // alpha = ความสว่าง/ความทึบ
        [Range(0.05f, 2f)] public float scale = 1f;
        public Vector2 offset;
        [Range(0f, 1f)] public float spikiness = 0.6f; // 0 = วงกลมมนๆ, 1 = หนามแหลมสลับยาวสั้นจัด
        [Range(0f, 0.8f)] public float morphAmount = 0.25f;
        public float morphSpeed = 0.35f;

        [NonSerialized] public Mesh mesh;
        [NonSerialized] public Vector3[] verts;
        [NonSerialized] public Color[] cols;
        [NonSerialized] public float[] baseR;
        [NonSerialized] public float[] baseA;
        [NonSerialized] public float seed;
    }

    [Header("Material")]
    [SerializeField] Material additiveMaterial;        // Mat_GateGlow (Legacy Shaders/Particles/Additive)
    [SerializeField] Material alphaMaterial;           // ชั้นสีเรียบ + ตา  ว่าง = ใช้ Sprites/Default

    [Header("การเรียงลำดับวาด")]
    [SerializeField] string sortingLayerName = "Default";
    [SerializeField] int baseOrder = 0;

    [Header("รูปร่าง")]
    [SerializeField] float size = 3f;                  // รัศมีของชั้นนอกสุด (หน่วยโลก)
    [SerializeField, Range(10, 64)] int pointCount = 28;
    [SerializeField] Vector2 aspect = new Vector2(1f, 0.55f);   // ยืดแนวนอนให้แบนเหมือนรอยแยกบนพื้น
    [SerializeField] float horizontalStretch = 1.2f;   // หนามด้านซ้าย-ขวายาวกว่าด้านบน-ล่าง

    [Header("การขยับ")]
    [SerializeField] float stepHz = 0f;                // 0 = ลื่นไหล, >0 = ขยับเป็นช่วงๆ แบบ glitch (ครั้งต่อวินาที)
    [SerializeField] float chargeSpeedMul = 3f;        // ตอน gate ชาร์จ ขยับเร็วขึ้นกี่เท่า

    [Header("ชั้น (เรียงจากหลังไปหน้า)")]
    [SerializeField] Layer[] layers;

    [Header("ตา")]
    [SerializeField] float eyeHalfWidth = 0.14f;       // สัดส่วนของ size
    [SerializeField] float eyeHalfHeight = 0.34f;
    [SerializeField] float eyeTravel = 0.10f;          // ระยะที่ตัวตาขยับตามผู้เล่น (สัดส่วนของ size)
    [SerializeField] float pupilTravelMul = 1.6f;      // รูม่านตาขยับไกลกว่าตัวตากี่เท่า
    [SerializeField, Range(0.1f, 1f)] float pupilWidth = 0.3f;
    [SerializeField] float eyeFollowSpeed = 6f;
    [SerializeField] Color eyeColor = new Color(0.03f, 0f, 0.07f, 1f);
    [SerializeField] Color pupilColor = new Color(0.6f, 0.02f, 0.1f, 1f);

    [Header("สถานะ")]
    [SerializeField, Range(0f, 1f)] float outOfRangeDim = 0.5f;    // ผู้เล่นอยู่นอกระยะ = จางลงเท่านี้
    [SerializeField] Color chargeColor = new Color(1f, 0.25f, 0.1f, 1f);
    [SerializeField, Range(0f, 2f)] float intensity = 1f;

    Gate gate;
    MaterialPropertyBlock mpb;
    float clock, dim = 1f, chargeT;
    Mesh eyeMesh, pupilMesh;
    Vector3[] eyeVerts = new Vector3[4], pupilVerts = new Vector3[4];
    Color[] eyeCols = new Color[4], pupilCols = new Color[4];
    Vector2 eyeOffset;

    // ค่าเริ่มต้นตอนเพิ่ม component (เรียงตามสีในดีไซน์)
    void Reset()
    {
        layers = new[]
        {
            new Layer { name = "Haze",  additive = true,  color = new Color(0.6f, 0.2f, 1f, 0.14f),  scale = 1.3f,  spikiness = 0.4f,  morphAmount = 0.15f, morphSpeed = 0.15f },
            new Layer { name = "Frame", additive = false, color = new Color(0.85f, 0.05f, 0.55f, 1f), scale = 1f,    spikiness = 0.85f, morphAmount = 0.3f,  morphSpeed = 0.25f },
            new Layer { name = "Body",  additive = false, color = new Color(0.58f, 0.05f, 1f, 1f),    scale = 0.78f, spikiness = 0.7f,  morphAmount = 0.25f, morphSpeed = 0.30f },
            new Layer { name = "Mid",   additive = false, color = new Color(0.75f, 0.42f, 1f, 1f),    scale = 0.58f, spikiness = 0.6f,  morphAmount = 0.22f, morphSpeed = 0.40f },
            new Layer { name = "Core",  additive = false, color = new Color(0.95f, 0.88f, 1f, 1f),    scale = 0.36f, spikiness = 0.5f,  morphAmount = 0.2f,  morphSpeed = 0.50f },
        };
    }

    void Start()
    {
        gate = GetComponentInParent<Gate>();

        mpb = new MaterialPropertyBlock();
        mpb.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));   // ค่ากลางของ Particles/Additive (คูณ 2 ในตัว shader)
        mpb.SetColor("_Color", Color.white);
        mpb.SetTexture("_MainTex", Texture2D.whiteTexture);              // พื้นขาวเรียบ ไม่ใช้ลายวงกลมนุ่มของ particle

        float gateSeed = Mathf.Abs(Mathf.Sin(transform.position.x * 12.9898f + transform.position.y * 78.233f)) * 1000f;

        for (int i = 0; i < layers.Length; i++) BuildLayer(layers[i], i, gateSeed + i * 101f);
        BuildEye(layers.Length);
    }

    void OnDestroy()
    {
        if (layers != null)
            foreach (var l in layers) if (l.mesh != null) Destroy(l.mesh);
        if (eyeMesh != null) Destroy(eyeMesh);
        if (pupilMesh != null) Destroy(pupilMesh);
    }

    // ---------- สร้าง ----------

    Material AlphaMat()
    {
        if (alphaMaterial != null) return alphaMaterial;
        var s = Shader.Find("Sprites/Default");
        if (s == null) { Debug.LogWarning("GateRiftVisual: ใส่ Alpha Material ด้วย (หา Sprites/Default ไม่เจอ)", this); return null; }
        alphaMaterial = new Material(s);
        return alphaMaterial;
    }

    MeshRenderer NewRenderer(string objName, Mesh mesh, Material mat, int order)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.sortingLayerName = sortingLayerName;
        mr.sortingOrder = order;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.SetPropertyBlock(mpb);
        return mr;
    }

    static float Hash01(float seed, int i)
    {
        float v = Mathf.Sin(seed * 12.9898f + i * 78.233f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }

    void BuildLayer(Layer L, int index, float seed)
    {
        int n = pointCount;
        L.seed = seed;
        L.verts = new Vector3[n + 1];
        L.cols = new Color[n + 1];
        L.baseR = new float[n];
        L.baseA = new float[n];

        for (int i = 0; i < n; i++)
        {
            float a = Mathf.PI * 2f * i / n;
            float rnd = Hash01(seed, i);
            bool longSpike = (i % 2 == 0);
            float spiky = longSpike ? Mathf.Lerp(0.8f, 1.2f, rnd) : Mathf.Lerp(0.35f, 0.65f, rnd);
            float r = Mathf.Lerp(0.75f, spiky, L.spikiness);
            r *= 1f + horizontalStretch * Mathf.Pow(Mathf.Abs(Mathf.Cos(a)), 4f) * L.spikiness;
            L.baseR[i] = r;
            L.baseA[i] = a;
        }

        var tris = new int[n * 3];
        for (int i = 0; i < n; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i + 1) % n + 1;
        }

        var uv = new Vector2[n + 1];
        for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);

        L.mesh = new Mesh { name = "Rift_" + L.name };
        L.mesh.MarkDynamic();
        L.mesh.vertices = L.verts;
        L.mesh.uv = uv;
        L.mesh.triangles = tris;

        Material mat = L.additive ? additiveMaterial : AlphaMat();
        if (mat == null) { mat = AlphaMat(); Debug.LogWarning("GateRiftVisual: ชั้น " + L.name + " ต้องการ Additive Material", this); }
        NewRenderer("Layer_" + L.name, L.mesh, mat, baseOrder + index);
    }

    void BuildEye(int index)
    {
        var tris = new[] { 0, 1, 2, 0, 2, 3 };
        var uv = new[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f) };

        eyeMesh = new Mesh { name = "Rift_Eye" };
        eyeMesh.MarkDynamic(); eyeMesh.vertices = eyeVerts; eyeMesh.uv = uv; eyeMesh.triangles = tris;
        pupilMesh = new Mesh { name = "Rift_Pupil" };
        pupilMesh.MarkDynamic(); pupilMesh.vertices = pupilVerts; pupilMesh.uv = uv; pupilMesh.triangles = tris;

        // ตาเป็นสีดำ ต้องใช้ Alpha Material เสมอ (additive วาดสีดำไม่ได้)
        NewRenderer("Eye", eyeMesh, AlphaMat(), baseOrder + index);
        NewRenderer("Pupil", pupilMesh, AlphaMat(), baseOrder + index + 1);
    }

    // ---------- วงจร ----------

    void Update()
    {
        if (layers == null || layers[0].mesh == null) return;
        float dt = Time.deltaTime;

        bool charging = gate != null && gate.State == GateState.Charging;
        bool inRange = gate == null || gate.PlayerInRange;
        dim = Mathf.MoveTowards(dim, inRange ? 1f : outOfRangeDim, Time.unscaledDeltaTime * 3f);
        chargeT = Mathf.MoveTowards(chargeT, charging ? 1f : 0f, Time.unscaledDeltaTime * 2f);

        clock += dt * Mathf.Lerp(1f, chargeSpeedMul, chargeT);
        float c = stepHz > 0f ? Mathf.Floor(clock * stepHz) / stepHz : clock;

        for (int i = 0; i < layers.Length; i++) UpdateLayer(layers[i], c);
        UpdateEye(c, dt);
    }

    Color Tinted(Color baseColor)
    {
        Color t = Color.Lerp(baseColor, chargeColor, chargeT * 0.85f);
        t.a = baseColor.a * dim * intensity;
        return t;
    }

    void UpdateLayer(Layer L, float c)
    {
        int n = pointCount;
        float t = c * L.morphSpeed;
        Vector2 off = L.offset * size;
        L.verts[0] = off;
        Color col = Tinted(L.color);
        L.cols[0] = col;

        for (int i = 0; i < n; i++)
        {
            float noise = Mathf.PerlinNoise(L.seed + i * 0.37f, t);
            float noiseA = Mathf.PerlinNoise(L.seed + 50f + i * 0.51f, t * 0.8f);
            float r = Mathf.Max(0.1f, L.baseR[i] * (1f + L.morphAmount * (noise * 2f - 1f)));
            float a = L.baseA[i] + (noiseA - 0.5f) * 0.12f;
            Vector2 p = new Vector2(Mathf.Cos(a) * r * aspect.x, Mathf.Sin(a) * r * aspect.y) * (L.scale * size);
            L.verts[i + 1] = p + off;
            L.cols[i + 1] = col;
        }

        L.mesh.vertices = L.verts;
        L.mesh.colors = L.cols;
        L.mesh.RecalculateBounds();
    }

    void UpdateEye(float c, float dt)
    {
        // ทิศไปหาผู้เล่น (ถ้าไม่มี = มองตรงกลาง)
        Vector2 target = Vector2.zero;
        if (EnemyTarget.Ensure())
        {
            Vector2 d = (Vector2)EnemyTarget.Transform.position - (Vector2)transform.position;
            float dist = d.magnitude;
            if (dist > 0.01f) target = d / dist * Mathf.Clamp01(dist / 12f);   // ใกล้ขึ้นหันชัดขึ้น
        }
        eyeOffset = Vector2.Lerp(eyeOffset, target, 1f - Mathf.Exp(-eyeFollowSpeed * Time.unscaledDeltaTime));

        float breathe = 1f + 0.05f * Mathf.Sin(c * 2f);
        float w = eyeHalfWidth * size, h = eyeHalfHeight * size * breathe;
        Vector2 body = new Vector2(eyeOffset.x * aspect.x, eyeOffset.y * aspect.y) * (eyeTravel * size);
        Vector2 pup = body * pupilTravelMul;

        SetDiamond(eyeVerts, body, w, h);
        SetDiamond(pupilVerts, pup, w * pupilWidth, h * 0.75f);

        Color ec = eyeColor; ec.a *= dim;
        Color pc = pupilColor; pc.a *= dim;
        Color pcT = Color.Lerp(pc, chargeColor, chargeT); pcT.a = pc.a;
        for (int i = 0; i < 4; i++) { eyeCols[i] = ec; pupilCols[i] = pcT; }

        eyeMesh.vertices = eyeVerts; eyeMesh.colors = eyeCols; eyeMesh.RecalculateBounds();
        pupilMesh.vertices = pupilVerts; pupilMesh.colors = pupilCols; pupilMesh.RecalculateBounds();
    }

    static void SetDiamond(Vector3[] v, Vector2 center, float halfW, float halfH)
    {
        v[0] = center + new Vector2(0f, halfH);
        v[1] = center + new Vector2(halfW, 0f);
        v[2] = center + new Vector2(0f, -halfH);
        v[3] = center + new Vector2(-halfW, 0f);
    }
}