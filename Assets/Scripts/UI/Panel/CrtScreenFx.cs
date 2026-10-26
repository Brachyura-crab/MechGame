using UnityEngine;
using UnityEngine.UI;

/// เอฟเฟกต์จอ CRT: scanline, vignette, เส้นสแกนกวาด, กะพริบ
/// วางบน object ที่ stretch ทับพื้นที่จอ และเป็นลูกลำดับสุดท้ายของ Panel
[RequireComponent(typeof(RectMask2D))]
public class CrtScreenFx : MonoBehaviour
{
    public static bool GlobalEnabled = true;   // ผูกกับตัวเลือกเกมทีหลังได้

    [Header("Scanlines")]
    [SerializeField] bool scanlines = true;
    [SerializeField, Range(0f, 1f)] float scanlineAlpha = 0.80f;

    [Header("Vignette")]
    [SerializeField] bool vignette = true;
    [SerializeField, Range(0f, 1f)] float vignetteStrength = 0.60f;

    [Header("Sweep")]
    [SerializeField] bool sweep = true;
    [SerializeField] float sweepHeight = 110f;
    [SerializeField] float sweepPeriod = 7f;
    [SerializeField, Range(0f, 0.5f)] float sweepAlpha = 0.03f;
    [SerializeField] ThemeRole sweepRole = ThemeRole.InkLow;

    [Header("Flicker")]
    [SerializeField] bool flicker = true;
    [SerializeField] CanvasGroup flickerTarget;   // CanvasGroup ของ object ที่ครอบหน้าทั้งหมด (อย่าใช้ตัวเดียวกับเอฟเฟกต์พับจอ)
    [SerializeField, Range(0f, 0.3f)] float flickerDepth = 0.06f;
    [SerializeField] Vector2 flickerGap = new Vector2(2f, 10f);

    RectTransform rt;
    Canvas rootCanvas;
    RawImage scanImg, vigImg, sweepImg;
    Texture2D scanTex, vigTex, sweepTex;
    float nextFlicker, dipUntil;
    public void SetFlickerTarget(CanvasGroup g) => flickerTarget = g;

    void OnEnable()
    {
        rt = (RectTransform)transform;
        Build();
        UiThemeSource.Changed += ApplyTheme;
        ApplyTheme();
        nextFlicker = Time.unscaledTime + Random.Range(flickerGap.x, flickerGap.y);
    }

    void OnDisable()
    {
        UiThemeSource.Changed -= ApplyTheme;
        if (flickerTarget != null) flickerTarget.alpha = 1f;
    }

    void OnDestroy()
    {
        if (scanTex != null) Destroy(scanTex);
        if (vigTex != null) Destroy(vigTex);
        if (sweepTex != null) Destroy(sweepTex);
    }

    RawImage MakeRaw(string n, Texture tex)
    {
        Transform t = transform.Find(n);
        GameObject go = t != null ? t.gameObject : new GameObject(n, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(transform, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        var img = go.GetComponent<RawImage>();
        img.texture = tex;
        img.raycastTarget = false;
        go.hideFlags = HideFlags.DontSave;
        return img;
    }

    void Build()
    {
        // scanline: พิกเซลละ 1 เส้นทุก 3 พิกเซล
        scanTex = new Texture2D(1, 3, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
        scanTex.SetPixels32(new[] { new Color32(255,255,255,255), new Color32(255,255,255,0), new Color32(255,255,255,0) });
        scanTex.Apply();

        // vignette
        const int N = 128;
        vigTex = new Texture2D(N, N, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1.3f, d));
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        vigTex.SetPixels32(px); vigTex.Apply();

        // sweep: ไล่ระดับแนวตั้ง โปร่ง-สว่าง-โปร่ง
        const int S = 64;
        sweepTex = new Texture2D(1, S, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        var sp = new Color32[S];
        for (int i = 0; i < S; i++)
            sp[i] = new Color32(255, 255, 255, (byte)(Mathf.Sin((i + 0.5f) / S * Mathf.PI) * 255f));
        sweepTex.SetPixels32(sp); sweepTex.Apply();

        vigImg   = MakeRaw("Fx_Vignette",  vigTex);
        sweepImg = MakeRaw("Fx_Sweep",     sweepTex);
        scanImg  = MakeRaw("Fx_Scanlines", scanTex);

        // sweep ใช้ anchor ด้านบน เลื่อนลงด้วยโค้ด
        var sr = (RectTransform)sweepImg.transform;
        sr.anchorMin = new Vector2(0f, 1f); sr.anchorMax = new Vector2(1f, 1f);
        sr.pivot = new Vector2(0.5f, 1f);
        sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;
    }

    void ApplyTheme()
    {
        if (sweepImg == null) return;
        UiThemeSource src = UiThemeSource.Current;
        Color c = (src != null && src.Theme != null) ? src.Theme.Get(sweepRole) : Color.green;
        c.a = sweepAlpha;
        sweepImg.color = c;
    }

    void LateUpdate()
    {
        bool on = GlobalEnabled;
        scanImg.enabled  = on && scanlines;
        vigImg.enabled   = on && vignette;
        sweepImg.enabled = on && sweep;

        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        Rect r = rt.rect;

        if (scanImg.enabled)
        {
            float pxH = r.height * rootCanvas.scaleFactor;
            float pxW = Mathf.Max(1f, r.width * rootCanvas.scaleFactor);
            scanImg.uvRect = new Rect(0f, 0f, 1f, pxH / 3f);
            scanImg.color = new Color(0f, 0f, 0f, scanlineAlpha);
        }

        if (vigImg.enabled)
            vigImg.color = new Color(0f, 0f, 0f, vignetteStrength);

        if (sweepImg.enabled)
        {
            var sr = (RectTransform)sweepImg.transform;
            sr.sizeDelta = new Vector2(0f, sweepHeight);
            float t = (Time.unscaledTime % Mathf.Max(0.1f, sweepPeriod)) / Mathf.Max(0.1f, sweepPeriod);
            sr.anchoredPosition = new Vector2(0f, Mathf.Lerp(sweepHeight, -r.height, t));
            ApplyTheme();
        }

        if (flickerTarget != null)
        {
            if (!(on && flicker)) { flickerTarget.alpha = 1f; return; }
            float now = Time.unscaledTime;
            if (now >= nextFlicker)
            {
                dipUntil = now + 0.12f;
                nextFlicker = now + Random.Range(flickerGap.x, flickerGap.y);
            }
            flickerTarget.alpha = now < dipUntil
                ? 1f - flickerDepth * (0.5f + 0.5f * Mathf.Sin(now * 90f))
                : 1f;
        }
    }
}