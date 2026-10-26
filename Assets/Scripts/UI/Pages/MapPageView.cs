using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EnemyV2;

// หน้า MAP สร้างด้วยโค้ด: แผนที่สี่เหลี่ยมใหญ่ (ครอบคลุมพื้นที่ภารกิจ เหนือเป็นบน) + legend แคบด้านขวา
// วางบน Page_Map (ยืดเต็มพื้นที่เนื้อหา ห้ามมี Layout Group)  สี/ขนาดไอคอนอ่านจาก MapStyle ชุดเดียวกับ minimap
public class MapPageView : MonoBehaviour
{
    [SerializeField] MapStyle style;
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;
    [SerializeField] float legendWidth = 200f;
    [SerializeField] float zonePadding = 1.12f;   // เผื่อขอบรอบวงเขตเตือน

    const float Gap = 14f;

    UiKit kit;
    bool built;
    RectTransform mapArea, iconLayer;
    MapLines grid, ringWarn, ringExtract, ringField, ringActive;
    Image playerArrow;
    Sprite circle, arrow;
    readonly List<Image> pool = new List<Image>();
    int used;
    Rigidbody2D playerRb;
    Vector2 lastDir = Vector2.up;
    TextMeshProUGUI scaleText;

    void OnEnable()
    {
        if (!built) Build();
        UiThemeSource.Changed += OnTheme;
    }
    void OnDisable() => UiThemeSource.Changed -= OnTheme;
    void OnTheme() { if (built) kit.Refresh(); }

    void OnDestroy()
    {
        Kill(circle); Kill(arrow);
    }
    static void Kill(Sprite s) { if (s == null) return; if (s.texture != null) Destroy(s.texture); Destroy(s); }

    // ---------- สร้าง ----------
    void Build()
    {
        built = true;
        if (style == null) style = ScriptableObject.CreateInstance<MapStyle>();
        kit = new UiKit { headFont = headFont, monoFont = monoFont };
        circle = MinimapView.MakeCircleSprite(64);
        arrow = MinimapView.MakeArrowSprite(64);

        var root = (RectTransform)transform;

        // กรอบแผนที่
        var frame = kit.Frame(root, "MapFrame", ThemeRole.InkMid, 0.8f, ThemeRole.Background, 0.9f);
        var frt = frame.rectTransform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = new Vector2(-(legendWidth + Gap), 0);

        // พื้นที่วาด (ตัดส่วนล้น)
        mapArea = UiKit.Rect("MapArea", frt);
        UiKit.Stretch(mapArea, 6, 6, 6, 6);
        mapArea.gameObject.AddComponent<RectMask2D>();

        grid = NewLines("Grid", style.gridColor);
        ringWarn = NewLines("RingWarn", style.warnColor);
        ringExtract = NewLines("RingExtract", style.extractionColor);
        ringField = NewLines("RingField", style.fieldColor);
        ringActive = NewLines("RingActive", style.activeColor);

        iconLayer = UiKit.Rect("Icons", mapArea);
        CenterStretch(iconLayer);

        var pa = UiKit.Rect("Player", mapArea);
        pa.anchorMin = pa.anchorMax = pa.pivot = new Vector2(0.5f, 0.5f);
        playerArrow = AddImage(pa);
        playerArrow.sprite = arrow; playerArrow.color = style.playerColor; playerArrow.raycastTarget = false;
        pa.sizeDelta = new Vector2(style.playerSize * 1.4f, style.playerSize * 1.4f);

        var n = kit.Label(frt, "North", "N", 16, ThemeRole.InkMid, true, TextAlignmentOptions.Center);
        n.rectTransform.anchorMin = n.rectTransform.anchorMax = n.rectTransform.pivot = new Vector2(0.5f, 1);
        n.rectTransform.sizeDelta = new Vector2(30, 22); n.rectTransform.anchoredPosition = new Vector2(0, -8);

        scaleText = kit.Label(frt, "Scale", "", 13, ThemeRole.InkDim, false, TextAlignmentOptions.BottomLeft);
        scaleText.rectTransform.anchorMin = scaleText.rectTransform.anchorMax = scaleText.rectTransform.pivot = new Vector2(0, 0);
        scaleText.rectTransform.sizeDelta = new Vector2(240, 20); scaleText.rectTransform.anchoredPosition = new Vector2(14, 10);

        BuildLegend(root);
    }

    MapLines NewLines(string name, Color c)
    {
        var rt = UiKit.Rect(name, mapArea);
        CenterStretch(rt);
        var l = rt.gameObject.AddComponent<MapLines>();
        l.color = c; l.raycastTarget = false;
        return l;
    }

    static Image AddImage(RectTransform rt)
    {
        rt.gameObject.AddComponent<CanvasRenderer>();
        return rt.gameObject.AddComponent<Image>();
    }

    static void CenterStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
    }

    void BuildLegend(RectTransform root)
    {
        var f = kit.Frame(root, "Legend", ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        var rt = f.rectTransform;
        rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 0.5f);
        rt.offsetMin = new Vector2(-legendWidth, 0); rt.offsetMax = Vector2.zero;

        var h = kit.Label(rt, "Heading", "LEGEND", 16, ThemeRole.InkMid, true);
        UiKit.TopLeft(h.rectTransform, 14, 10, 160, 22);

        float y = 44;
        Entry(rt, ref y, "PLAYER", style.playerColor, arrow, 14);
        Entry(rt, ref y, "DROP / EXTRACT", style.extractionColor, null, 10);
        Entry(rt, ref y, "GATE", style.gateColor, circle, 14);
        Entry(rt, ref y, "SEALED GATE", Dim(style.gateColor), circle, 10);
        Entry(rt, ref y, "HOSTILE", style.swarmColor, circle, 8);
        Entry(rt, ref y, "LEADER", style.leaderColor, circle, 12);
        y += 8;
        Entry(rt, ref y, "MISSION ZONE", style.warnColor, null, 0);
        Entry(rt, ref y, "EXTRACT ZONE", style.extractionColor, null, 0);
        Entry(rt, ref y, "GATE FIELD", style.fieldColor, null, 0);
        Entry(rt, ref y, "GATE RANGE", style.activeColor, null, 0);
    }

    static Color Dim(Color c) { c.a = 0.4f; return c; }

    // size 0 = สัญลักษณ์เส้น (วง)
    void Entry(RectTransform parent, ref float y, string text, Color col, Sprite sprite, float size)
    {
        var sw = UiKit.Rect("Sw", parent);
        sw.anchorMin = sw.anchorMax = sw.pivot = new Vector2(0, 1);
        var img = AddImage(sw);
        img.raycastTarget = false; img.color = col; img.sprite = sprite;
        if (size > 0) { sw.sizeDelta = new Vector2(size, size); sw.anchoredPosition = new Vector2(14 + (16 - size) * 0.5f, -(y + 3 + (16 - size) * 0.5f)); }
        else { sw.sizeDelta = new Vector2(16, 2); sw.anchoredPosition = new Vector2(14, -(y + 10)); }

        var t = kit.Label(parent, "T", text, 13, ThemeRole.InkMid, true, TextAlignmentOptions.TopLeft);
        UiKit.TopLeft(t.rectTransform, 40, y, legendWidth - 46, 20);
        y += 28;
    }

    // ---------- อัปเดต ----------
    void LateUpdate()
    {
        if (!built) return;
        used = 0;

        var mm = MissionManager.Instance;
        bool haveTarget = EnemyTarget.Ensure();
        if (mm == null || !haveTarget) { ClearAll(); HideUnused(); return; }

        Vector2 origin = mm.Origin;
        float radius = Mathf.Max(5f, mm.WarnRadius * zonePadding);
        Rect r = mapArea.rect;
        float ppu = Mathf.Min(r.width, r.height) * 0.5f / radius;   // px ต่อหน่วยโลก
        float view = Mathf.Max(r.width, r.height);

        DrawGrid(origin, ppu, r);
        DrawRings(mm, origin, ppu, view);
        scaleText.text = "GRID " + Mathf.RoundToInt(style.gridSpacing) + " M";

        Vector2 pp = EnemyTarget.Transform.position;
        bool dead = EnemyTarget.PlayerHealth != null && EnemyTarget.PlayerHealth.IsDead;

        // ศัตรู: เฉพาะที่อยู่ในรัศมีเดียวกับ minimap รอบผู้เล่น
        if (!dead)
        {
            var list = MapIconSource.All;
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (!s.IsAlive || (s.Position - pp).sqrMagnitude > style.visibleRadius * style.visibleRadius) continue;
                bool leader = s.Type == MapIconType.Leader;
                Show(ToLocal(s.Position, origin, ppu), circle, leader ? style.leaderColor : style.swarmColor,
                     leader ? style.leaderSize : style.swarmSize);
            }
        }

        // ประตู: ที่ยังอยู่ (ตามสถานะ) และที่ปิดแล้ว (หม่น)
        var infos = mm.GateInfos;
        for (int i = 0; i < infos.Count; i++)
        {
            if (!infos[i].closed) continue;
            Show(ToLocal(infos[i].position, origin, ppu), circle, Dim(style.gateColor), style.gateSize * 0.7f);
        }
        var gates = Gate.All;
        for (int i = 0; i < gates.Count; i++)
        {
            var g = gates[i];
            if (g == null || g.State == GateState.Closed) continue;
            float size = style.gateSize * 1.2f;
            if (g.State == GateState.Charging) size *= 1f + 0.25f * Mathf.Sin(Time.unscaledTime * style.pulseSpeed);
            Show(ToLocal(g.transform.position, origin, ppu), circle, style.gateColor, size);
        }

        // จุดส่ง
        float es = style.extractionSize * 1.3f;
        if (mm.Phase == MissionPhase.Extract) es *= 1f + 0.25f * Mathf.Sin(Time.unscaledTime * style.pulseSpeed);
        Show(ToLocal(origin, origin, ppu), null, style.extractionColor, es);

        UpdatePlayer(pp, origin, ppu);
        HideUnused();
    }

    static Vector2 ToLocal(Vector2 world, Vector2 origin, float ppu) => (world - origin) * ppu;

    void UpdatePlayer(Vector2 pp, Vector2 origin, float ppu)
    {
        if (playerRb == null) playerRb = EnemyTarget.Transform.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            Vector2 v = playerRb.linearVelocity;
            if (v.sqrMagnitude > style.arrowMinSpeed * style.arrowMinSpeed) lastDir = v.normalized;
        }
        var rt = playerArrow.rectTransform;
        rt.anchoredPosition = ToLocal(pp, origin, ppu);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(lastDir.y, lastDir.x) * Mathf.Rad2Deg - 90f);
        playerArrow.transform.SetAsLastSibling();
    }

    void DrawGrid(Vector2 origin, float ppu, Rect r)
    {
        grid.Begin(style.lineThickness);
        float s = style.gridSpacing;
        if (s > 0.01f)
        {
            float hx = r.width * 0.5f / ppu, hy = r.height * 0.5f / ppu;
            int x0 = Mathf.FloorToInt((origin.x - hx) / s), x1 = Mathf.CeilToInt((origin.x + hx) / s);
            for (int k = x0; k <= x1; k++)
            {
                float x = (k * s - origin.x) * ppu;
                grid.Line(new Vector2(x, -r.height), new Vector2(x, r.height));
            }
            int y0 = Mathf.FloorToInt((origin.y - hy) / s), y1 = Mathf.CeilToInt((origin.y + hy) / s);
            for (int k = y0; k <= y1; k++)
            {
                float y = (k * s - origin.y) * ppu;
                grid.Line(new Vector2(-r.width, y), new Vector2(r.width, y));
            }
        }
        grid.Commit();
    }

    void DrawRings(MissionManager mm, Vector2 origin, float ppu, float view)
    {
        float th = style.lineThickness;
        ringWarn.Begin(th); ringExtract.Begin(th); ringField.Begin(th); ringActive.Begin(th);

        ringWarn.Circle(Vector2.zero, mm.WarnRadius * ppu, view * 2f);
        ringExtract.Circle(Vector2.zero, mm.ExtractRadius * ppu, view * 2f);

        var gates = Gate.All;
        for (int i = 0; i < gates.Count; i++)
        {
            var g = gates[i];
            if (g == null || g.State == GateState.Closed) continue;
            Vector2 c = ToLocal(g.transform.position, origin, ppu);
            if (g.TryGetComponent(out GateField gf)) ringField.Circle(c, gf.CurrentRadius * ppu, view * 2f);
            if (g.Data != null) ringActive.Circle(c, g.Data.activeRadius * ppu, view * 2f);
        }
        ringWarn.Commit(); ringExtract.Commit(); ringField.Commit(); ringActive.Commit();
    }

    void ClearAll()
    {
        grid.Begin(1f); grid.Commit();
        ringWarn.Begin(1f); ringWarn.Commit();
        ringExtract.Begin(1f); ringExtract.Commit();
        ringField.Begin(1f); ringField.Commit();
        ringActive.Begin(1f); ringActive.Commit();
    }

    // ---------- icon pool ----------
    void Show(Vector2 local, Sprite sprite, Color col, float size)
    {
        Image im;
        if (used < pool.Count) im = pool[used];
        else
        {
            var rt = UiKit.Rect("i", iconLayer);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            im = AddImage(rt);
            im.raycastTarget = false;
            pool.Add(im);
        }
        used++;
        if (!im.gameObject.activeSelf) im.gameObject.SetActive(true);
        im.sprite = sprite; im.color = col;
        im.rectTransform.sizeDelta = new Vector2(size, size);
        im.rectTransform.anchoredPosition = local;
    }

    void HideUnused()
    {
        for (int i = used; i < pool.Count; i++)
            if (pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);
    }
}