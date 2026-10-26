using UnityEngine;
using UnityEngine.UI;

/// สามเหลี่ยมทึบ ใช้แทนตัวอักษร ▶ ◀ (สีตั้งผ่าน UiKit.Bind ได้)
public class TriangleGraphic : MaskableGraphic
{
    public bool pointLeft;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Vector2 a, b, c;
        if (!pointLeft)
        {
            a = new Vector2(r.xMin, r.yMax); b = new Vector2(r.xMin, r.yMin); c = new Vector2(r.xMax, r.center.y);
        }
        else
        {
            a = new Vector2(r.xMax, r.yMax); b = new Vector2(r.xMax, r.yMin); c = new Vector2(r.xMin, r.center.y);
        }
        Color32 col = color;
        vh.AddVert(a, col, Vector4.zero);
        vh.AddVert(b, col, Vector4.zero);
        vh.AddVert(c, col, Vector4.zero);
        vh.AddTriangle(0, 1, 2);
    }
}