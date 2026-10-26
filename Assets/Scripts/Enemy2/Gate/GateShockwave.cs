using UnityEngine;
using EnemyV2;

/// วงแรงกระแทกตอน Gate ระเบิด ขยายถึงรัศมีของ GateBlast
/// วางบน GameObject เดียวกับ Gate
[RequireComponent(typeof(Gate))]
public class GateShockwave : MonoBehaviour
{
    [SerializeField] Shader waveShader;
    [SerializeField] string sortingLayerName = "Default";
    [SerializeField] int sortingOrder = 50;

    [Header("ขนาด / เวลา")]
    [Tooltip("ใช้เมื่อไม่มี GateBlast บน object นี้")]
    [SerializeField] float fallbackRadius = 4f;
    [SerializeField] float radiusMul = 1f;
    [SerializeField] float duration = 0.6f;
    [Tooltip("น้อยกว่า 1 = พุ่งเร็วตอนแรกแล้วช้าลง")]
    [Range(0.2f, 3f)] [SerializeField] float expandCurve = 0.5f;

    [Header("หน้าตา")]
    [ColorUsage(true, true)] [SerializeField] Color color = new Color(1.6f, 0.9f, 1.8f, 1f);
    [Range(0.01f, 1f)] [SerializeField] float trailWidth = 0.25f;
    [SerializeField] float fadeCurve = 1.5f;
    [Range(0f, 1f)] [SerializeField] float innerGlow = 0.15f;

    Gate gate;
    GateBlast blast;
    static Sprite whiteSprite;

    void Awake()
    {
        gate = GetComponent<Gate>();
        blast = GetComponent<GateBlast>();
    }

    void OnEnable() { if (gate == null) gate = GetComponent<Gate>(); gate.Exploded += Spawn; }
    void OnDisable() { if (gate != null) gate.Exploded -= Spawn; }

    void Spawn()
    {
        if (waveShader == null) waveShader = Shader.Find("Gate/RiftShockwave");
        if (waveShader == null)
        {
            Debug.LogError("GateShockwave: ไม่พบ shader Gate/RiftShockwave (ใส่ในช่อง Wave Shader)", this);
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
            // pivot กลาง ขนาด 1x1 unit
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f,
                                        0, SpriteMeshType.FullRect);
        }

        float radius = (blast != null && blast.Radius > 0f ? blast.Radius : fallbackRadius) * radiusMul;

        // วัตถุแยกอิสระ ไม่เป็นลูกของ Gate เพราะ Gate จะถูกลบหลังปิด
        var go = new GameObject("GateShockwave");
        go.transform.position = transform.position;
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        var mat = new Material(waveShader);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Width", trailWidth);
        mat.SetFloat("_Fade", fadeCurve);
        mat.SetFloat("_Flash", innerGlow);
        mat.SetFloat("_Prog", 0f);
        sr.sharedMaterial = mat;

        var wave = go.AddComponent<Wave>();
        wave.mat = mat;
        wave.duration = Mathf.Max(0.05f, duration);
        wave.expandCurve = expandCurve;
    }

    class Wave : MonoBehaviour
    {
        public Material mat;
        public float duration, expandCurve;
        float age;

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            mat.SetFloat("_Prog", Mathf.Pow(t, expandCurve));

            if (t >= 1f)
            {
                Destroy(mat);
                Destroy(gameObject);
            }
        }
    }
}