using UnityEngine;
using UnityEngine.UI;

/// กรอบ/พื้นหลังมุมตัดเฉียง วาดด้วยเมช ใช้แทน Image คู่ Frame + Inner
[AddComponentMenu("UI/Cut Corner Frame")]
public class CutCornerFrame : MaskableGraphic
{
    [Header("Shape")]
    [SerializeField] float cut = 14f;
    [SerializeField] float thickness = 1f;
    [SerializeField] bool cutBottomLeft = true;
    [SerializeField] bool cutBottomRight = false;
    [SerializeField] bool cutTopRight = true;
    [SerializeField] bool cutTopLeft = false;

    [Header("Colors (theme role)")]
    [SerializeField] ThemeRole outlineRole = ThemeRole.InkMid;
    [SerializeField, Range(0f, 1f)] float outlineAlpha = 0.55f;
    [SerializeField] ThemeRole fillRole = ThemeRole.Surface;
    [SerializeField, Range(0f, 1f)] float fillAlpha = 1f;

    [Tooltip("ใช้สีจาก Graphic.color แทน theme role (ให้สคริปต์อื่นสั่งสีได้)")]
    [SerializeField] bool useTint = false;

    readonly Vector2[] outer = new Vector2[8];
    readonly Vector2[] inner = new Vector2[8];

    protected override void OnEnable()
    {
        base.OnEnable();
        UiThemeSource.Changed += SetVerticesDirty;
    }

    protected override void OnDisable()
    {
        UiThemeSource.Changed -= SetVerticesDirty;
        base.OnDisable();
    }

#if UNITY_EDITOR
    protected override void Reset() { base.Reset(); raycastTarget = false; }
#endif

    public void SetStyle(ThemeRole outline, float outlineA, ThemeRole fill, float fillA)
    {
        if (outlineRole == outline && fillRole == fill &&
            Mathf.Approximately(outlineAlpha, outlineA) && Mathf.Approximately(fillAlpha, fillA)) return;
        outlineRole = outline; outlineAlpha = outlineA;
        fillRole = fill; fillAlpha = fillA;
        SetVerticesDirty();
    }

    Color32 Tint(float a)
    {
        Color c = color;
        c.a *= a;
        return c;
    }

    Color32 Resolve(ThemeRole role, float a)
    {
        UiThemeSource src = UiThemeSource.Current;
        Color c = (src != null && src.Theme != null) ? src.Theme.Get(role) : Color.green;
        c.a = a;
        return c;
    }

    int BuildOuter(Rect r)
    {
        float c = Mathf.Min(cut, r.width * 0.5f, r.height * 0.5f);
        int n = 0;
        // ทวนเข็ม: BL -> BR -> TR -> TL
        if (cutBottomLeft)  { outer[n++] = new Vector2(r.xMin, r.yMin + c); outer[n++] = new Vector2(r.xMin + c, r.yMin); }
        else                  outer[n++] = new Vector2(r.xMin, r.yMin);
        if (cutBottomRight) { outer[n++] = new Vector2(r.xMax - c, r.yMin); outer[n++] = new Vector2(r.xMax, r.yMin + c); }
        else                  outer[n++] = new Vector2(r.xMax, r.yMin);
        if (cutTopRight)    { outer[n++] = new Vector2(r.xMax, r.yMax - c); outer[n++] = new Vector2(r.xMax - c, r.yMax); }
        else                  outer[n++] = new Vector2(r.xMax, r.yMax);
        if (cutTopLeft)     { outer[n++] = new Vector2(r.xMin + c, r.yMax); outer[n++] = new Vector2(r.xMin, r.yMax - c); }
        else                  outer[n++] = new Vector2(r.xMin, r.yMax);
        return n;
    }

    void BuildInner(int n, float t)
    {
        for (int i = 0; i < n; i++)
        {
            Vector2 p = outer[i];
            Vector2 d0 = (p - outer[(i + n - 1) % n]).normalized;
            Vector2 d1 = (outer[(i + 1) % n] - p).normalized;
            Vector2 n0 = new Vector2(-d0.y, d0.x);   // ซ้าย = ด้านใน (polygon ทวนเข็ม)
            Vector2 n1 = new Vector2(-d1.y, d1.x);
            float k = 1f + Vector2.Dot(n0, n1);
            inner[i] = p + (n0 + n1) * (t / Mathf.Max(k, 0.0001f));
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width < 2f || r.height < 2f) return;

        int n = BuildOuter(r);
        float t = Mathf.Min(thickness, Mathf.Min(r.width, r.height) * 0.5f - 0.5f);
        t = Mathf.Max(t, 0f);
        BuildInner(n, t);

        // พื้น (fan จากจุดกลาง)
        if (fillAlpha > 0f)
        {
            Color32 fc = useTint ? Tint(fillAlpha) : Resolve(fillRole, fillAlpha);
            vh.AddVert(r.center, fc, Vector2.zero);
            for (int i = 0; i < n; i++) vh.AddVert(inner[i], fc, Vector2.zero);
            for (int i = 0; i < n; i++) vh.AddTriangle(0, 1 + i, 1 + (i + 1) % n);
        }

        // เส้นขอบ (ring ระหว่าง outer กับ inner)
        if (t > 0f && outlineAlpha > 0f)
        {
            Color32 oc = useTint ? Tint(outlineAlpha) : Resolve(outlineRole, outlineAlpha);
            int b = vh.currentVertCount;
            for (int i = 0; i < n; i++) vh.AddVert(outer[i], oc, Vector2.zero);
            for (int i = 0; i < n; i++) vh.AddVert(inner[i], oc, Vector2.zero);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                vh.AddTriangle(b + i, b + j, b + n + j);
                vh.AddTriangle(b + i, b + n + j, b + n + i);
            }
        }
    }
}