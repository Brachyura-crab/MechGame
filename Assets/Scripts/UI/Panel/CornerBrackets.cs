using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Corner Brackets")]
public class CornerBrackets : MaskableGraphic
{
    [SerializeField] float length = 12f;
    [SerializeField] float thickness = 2f;
    [SerializeField] float inset = 0f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        float L = Mathf.Min(length, r.width * 0.5f, r.height * 0.5f), t = thickness;
        float x0 = r.xMin + inset, x1 = r.xMax - inset, y0 = r.yMin + inset, y1 = r.yMax - inset;

        Quad(vh, x0, y0, x0 + L, y0 + t);  Quad(vh, x0, y0, x0 + t, y0 + L);   // ล่างซ้าย
        Quad(vh, x1 - L, y0, x1, y0 + t);  Quad(vh, x1 - t, y0, x1, y0 + L);   // ล่างขวา
        Quad(vh, x0, y1 - t, x0 + L, y1);  Quad(vh, x0, y1 - L, x0 + t, y1);   // บนซ้าย
        Quad(vh, x1 - L, y1 - t, x1, y1);  Quad(vh, x1 - t, y1 - L, x1, y1);   // บนขวา
    }

    void Quad(VertexHelper vh, float xa, float ya, float xb, float yb)
    {
        int b = vh.currentVertCount;
        Color32 c = color;
        vh.AddVert(new Vector3(xa, ya), c, Vector2.zero);
        vh.AddVert(new Vector3(xa, yb), c, Vector2.zero);
        vh.AddVert(new Vector3(xb, yb), c, Vector2.zero);
        vh.AddVert(new Vector3(xb, ya), c, Vector2.zero);
        vh.AddTriangle(b, b + 1, b + 2);
        vh.AddTriangle(b, b + 2, b + 3);
    }
}