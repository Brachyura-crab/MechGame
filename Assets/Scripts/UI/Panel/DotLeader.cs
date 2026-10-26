using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Dot Leader")]
public class DotLeader : MaskableGraphic
{
    [SerializeField] float dotSize = 2f;
    [SerializeField] float gap = 3f;
    [SerializeField] float yOffset = -4f;   // ลบ = ต่ำกว่ากึ่งกลางแถว (ให้ใกล้เส้นฐานตัวอักษร)
    [SerializeField] float margin = 2f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        float step = dotSize + gap;
        if (step < 0.5f || r.width < dotSize + margin * 2f) return;

        int n = Mathf.Min(Mathf.FloorToInt((r.width - margin * 2f + gap) / step), 600);
        float y0 = r.center.y + yOffset - dotSize * 0.5f;
        var q = new UIVertex[4];
        for (int i = 0; i < n; i++)
        {
            float x0 = r.xMin + margin + i * step;
            q[0] = V(x0, y0);
            q[1] = V(x0, y0 + dotSize);
            q[2] = V(x0 + dotSize, y0 + dotSize);
            q[3] = V(x0 + dotSize, y0);
            vh.AddUIVertexQuad(q);
        }
    }

    UIVertex V(float x, float y)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = new Vector3(x, y);
        v.color = color;
        return v;
    }
}