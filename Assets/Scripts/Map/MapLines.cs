using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// วาดเส้นบางความหนาคงที่ (grid / วงกลมกลวง) เป็น mesh  พิกัดเป็นพิกเซลจากกึ่งกลาง rect (ตั้ง pivot กลาง ยืดเต็ม)
// ส่วนที่ล้นวงถูก Mask วงกลมของ minimap ตัดให้เอง
[RequireComponent(typeof(CanvasRenderer))]
public class MapLines : MaskableGraphic
{
    struct Seg { public Vector2 a, b; }

    readonly List<Seg> segs = new List<Seg>(256);
    float thickness = 1.5f;

    public void Begin(float thick)
    {
        segs.Clear();
        thickness = thick;
    }

    public void Line(Vector2 a, Vector2 b) => segs.Add(new Seg { a = a, b = b });

    // วงกลมกลวง  viewR = รัศมีวงที่มองเห็นจริง (ข้ามวงที่ไม่แตะวง/ส่วนโค้งที่อยู่ไกลเกินไป)
    public void Circle(Vector2 c, float r, float viewR)
    {
        if (r <= 0.01f) return;

        float cd = c.magnitude;
        if (cd - r > viewR) return;      // อยู่นอกวงทั้งหมด
        if (r - cd > viewR) return;      // ใหญ่คลุมทั้งวงโดยไม่แตะเส้น

        int n = Mathf.Clamp(Mathf.CeilToInt(r * 0.5f), 24, 256);
        float step = Mathf.PI * 2f / n;
        float segLen = r * step;

        Vector2 prev = c + new Vector2(r, 0f);
        for (int i = 1; i <= n; i++)
        {
            float a = i * step;
            Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            Vector2 mid = (prev + p) * 0.5f;
            if (mid.magnitude <= viewR + segLen + thickness)
                segs.Add(new Seg { a = prev, b = p });
            prev = p;
        }
    }

    public void Commit() => SetVerticesDirty();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float h = thickness * 0.5f;
        Color32 col = color;
        int v = 0;

        for (int i = 0; i < segs.Count; i++)
        {
            Vector2 d = segs[i].b - segs[i].a;
            float len = d.magnitude;
            if (len < 0.0001f) continue;
            d /= len;
            Vector2 nrm = new Vector2(-d.y, d.x) * h;

            vh.AddVert(segs[i].a - nrm, col, Vector2.zero);
            vh.AddVert(segs[i].a + nrm, col, Vector2.zero);
            vh.AddVert(segs[i].b + nrm, col, Vector2.zero);
            vh.AddVert(segs[i].b - nrm, col, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v + 2, v + 3, v);
            v += 4;
        }
    }
}