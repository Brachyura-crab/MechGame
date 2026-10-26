using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ตัวช่วยสร้าง UI ด้วยโค้ด + ระบบสีตามธีม (สีเปลี่ยนตามธีมอัตโนมัติ)
public class UiKit
{
    struct Item { public Graphic g; public ThemeRole role; public float a; }

    readonly List<Item> items = new List<Item>();
    public TMP_FontAsset headFont;   // Chakra Petch
    public TMP_FontAsset monoFont;   // Share Tech Mono

    public void Bind(Graphic g, ThemeRole role, float alpha = 1f)
    {
        items.Add(new Item { g = g, role = role, a = alpha });
        Apply(g, role, alpha);
    }

    // ใช้ตอนธีมเปลี่ยน
    public void Refresh()
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].g != null) Apply(items[i].g, items[i].role, items[i].a);
    }

    static void Apply(Graphic g, ThemeRole r, float a)
    {
        Color c = UiThemeSource.Role(r); c.a = a; g.color = c;
    }

    // ---------- จัดวาง ----------
    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }

    // วางจากมุมซ้ายบนของ parent
    public static void TopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // ---------- ชิ้นส่วน ----------
    public CutCornerFrame Frame(Transform parent, string name, ThemeRole outline, float oa, ThemeRole fill, float fa)
    {
        var rt = Rect(name, parent);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var f = rt.gameObject.AddComponent<CutCornerFrame>();
        f.raycastTarget = false;
        f.SetStyle(outline, oa, fill, fa);
        return f;
    }

    public Image Box(Transform parent, string name, ThemeRole role, float alpha)
    {
        var rt = Rect(name, parent);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        Bind(img, role, alpha);
        return img;
    }

    public TextMeshProUGUI Label(Transform parent, string name, string text, float size, ThemeRole role,
                                 bool head = false, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        var rt = Rect(name, parent);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        var font = head ? headFont : monoFont;
        if (font != null) t.font = font;
        t.text = text; t.fontSize = size; t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        Bind(t, role);
        return t;
    }

    public void SetColor(Graphic g, Color c) { g.color = c; }
}