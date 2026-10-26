using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้าเลือกด่านแบบแผนที่โหนด ใช้ข้อมูลจาก MissionCatalog + เซฟ
public class MissionSelectView : MenuScreen
{
    [Header("Scenes (ต้องอยู่ใน Build Settings)")]
    [SerializeField] string backScene = "UnitSelect";

    const float MapX = 28f, MapY = 68f, MapW = 800f, MapH = 572f;
    const float DetX = 844f, DetW = 408f;
    const float MarginX = 70f, MarginTop = 60f, MarginBottom = 70f;

    enum NodeState { Cleared, Open, Locked }

    GameConfig cfg;
    readonly List<MissionNode> nodes = new List<MissionNode>();
    int sel;
    CutCornerFrame ring;
    ScrollRect mapSR;
    Vector2? mapScroll;                 // null = ยังไม่เคยเลื่อน (เล็งโหนดที่เลือกตอนเปิด)
    float minX, maxX, minY, maxY, contentW, contentH;
    const float SX = MapW - MarginX * 2f, SY = MapH - MarginTop - MarginBottom;   // ขนาดต่อ 1.0 ของ mapPos

    static readonly string[] ThreatNames = { "", "LOW", "MODERATE", "HIGH", "SEVERE", "EXTREME" };

    protected override void OnBuild()
    {
        cfg = GameConfig.Instance;
        if (cfg != null && cfg.missions != null && cfg.missions.nodes != null)
            foreach (var n in cfg.missions.nodes) if (n != null) nodes.Add(n);

        // เริ่มที่ด่านที่เลือกไว้ ไม่งั้นด่านแรกที่เปิดและยังไม่ผ่าน ไม่งั้นด่านแรกที่เปิด
        sel = 0;
        int firstOpen = -1;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (GameSession.SelectedMission == nodes[i]) { sel = i; firstOpen = -2; break; }
            if (firstOpen == -1 && StateOf(nodes[i]) == NodeState.Open) firstOpen = i;
        }
        if (firstOpen >= 0) sel = firstOpen;

        string unit = "-";
        var m = GameSession.SelectedMech;
        if (m != null) unit = m.data != null ? m.data.shortName : m.id;
        BuildHeader("MISSION SELECT", "STEP 2 / 2    UNIT: " + unit);

        Rebuild();
    }

    NodeState StateOf(MissionNode n)
    {
        if (SaveService.IsCleared(n)) return NodeState.Cleared;
        return SaveService.IsUnlocked(n) ? NodeState.Open : NodeState.Locked;
    }

    MissionNode Cur => sel >= 0 && sel < nodes.Count ? nodes[sel] : null;

    // ---------- สร้างหน้า ----------
    void Rebuild()
    {
        if (mapSR != null) mapScroll = mapSR.content.anchoredPosition;
        ring = null;
        ResetDyn();

        if (nodes.Count == 0)
        {
            L(dyn, "NO MISSIONS: ตั้ง GameConfig + MissionCatalog ก่อน", 28, 68, 800, 40, 18, ThemeRole.Warn);
            BuildFooter();
            return;
        }
        BuildMap();
        BuildDetail();
        BuildFooter();
    }

    void ComputeBounds()
    {
        minX = 0f; maxX = 1f; minY = 0f; maxY = 1f;
        foreach (var n in nodes)
        {
            minX = Mathf.Min(minX, n.mapPos.x); maxX = Mathf.Max(maxX, n.mapPos.x);
            minY = Mathf.Min(minY, n.mapPos.y); maxY = Mathf.Max(maxY, n.mapPos.y);
        }
        contentW = MarginX * 2f + (maxX - minX) * SX;
        contentH = MarginTop + MarginBottom + (maxY - minY) * SY;
    }

    Vector2 NodePos(MissionNode n)
    {
        float x = MarginX + (n.mapPos.x - minX) * SX;
        float y = MarginTop + (maxY - n.mapPos.y) * SY;
        return new Vector2(x, y);
    }

    void BuildMap()
    {
        ComputeBounds();

        var card = Card(dyn, MapX, MapY, MapW, MapH, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        var p = card.rectTransform;

        // viewport (ตัดส่วนที่เลยกรอบ) + content ที่ใหญ่ตามโหนด
        var vp = UiKit.Rect("MapView", p);
        UiKit.TopLeft(vp, 0, 0, MapW, MapH);
        vp.gameObject.AddComponent<RectMask2D>();
        vp.gameObject.AddComponent<CanvasRenderer>();
        var vimg = vp.gameObject.AddComponent<Image>();
        vimg.color = Color.clear; vimg.raycastTarget = true;   // รับการลาก/ล้อเมาส์บนพื้นว่าง

        float cw = Mathf.Max(contentW, MapW), ch = Mathf.Max(contentH, MapH);
        var c = UiKit.Rect("MapContent", vp);
        c.anchorMin = c.anchorMax = c.pivot = new Vector2(0, 1);
        c.sizeDelta = new Vector2(cw, ch);
        c.anchoredPosition = Vector2.zero;

        mapSR = vp.gameObject.AddComponent<ScrollRect>();
        mapSR.viewport = vp; mapSR.content = c;
        mapSR.horizontal = cw > MapW; mapSR.vertical = ch > MapH;
        mapSR.movementType = ScrollRect.MovementType.Clamped;
        mapSR.scrollSensitivity = 30f;

        // กริด
        for (float x = 40; x < cw; x += 40) B(c, x, 0, 1, ch, ThemeRole.InkMid, 0.09f);
        for (float y = 40; y < ch; y += 40) B(c, 0, y, cw, 1, ThemeRole.InkMid, 0.09f);

        // เส้นเชื่อม (วาดก่อน โหนดจะทับ)
        foreach (var a in nodes)
        {
            if (a.connectsTo == null) continue;
            foreach (var b in a.connectsTo)
            {
                if (b == null || !nodes.Contains(b)) continue;
                bool live = StateOf(a) != NodeState.Locked && StateOf(b) != NodeState.Locked;
                Line(c, NodePos(a), NodePos(b), live);
            }
        }

        // โหนด
        for (int i = 0; i < nodes.Count; i++)
        {
            int k = i;
            var n = nodes[i];
            var st = StateOf(n);
            Vector2 pos = NodePos(n);
            bool on = i == sel;

            if (on)
            {
                ring = kit.Frame(c, "Ring", ThemeRole.InkBright, 1f, ThemeRole.InkBright, 0f);
                UiKit.TopLeft(ring.rectTransform, pos.x - 30, pos.y - 30, 60, 60);
            }

            CutCornerFrame f;
            string icon;
            ThemeRole fg;
            if (st == NodeState.Cleared)
            {
                f = kit.Frame(c, "Node", ThemeRole.InkBright, 1f, ThemeRole.InkMid, 0.38f);
                icon = "OK"; fg = ThemeRole.InkBright;
            }
            else if (st == NodeState.Open)
            {
                f = kit.Frame(c, "Node", ThemeRole.InkBright, 0.85f, ThemeRole.Surface, 0.9f);
                icon = (i + 1).ToString("00"); fg = ThemeRole.InkBright;
            }
            else
            {
                f = kit.Frame(c, "Node", ThemeRole.InkLow, 0.5f, ThemeRole.Surface, 0.5f);
                icon = "x"; fg = ThemeRole.InkLow;
            }
            UiKit.TopLeft(f.rectTransform, pos.x - 20, pos.y - 20, 40, 40);
            L(f.rectTransform, icon, 0, 0, 40, 40, 15, fg, false, TextAlignmentOptions.Center);
            Hit(f.rectTransform, () => { sel = k; Rebuild(); });

            bool dim = st == NodeState.Locked;
            L(c, n.displayName, pos.x - 70, pos.y + 24, 140, 18, 12, dim ? ThemeRole.InkLow : ThemeRole.InkMid, true,
              TextAlignmentOptions.Center);
            L(c, n.id.ToUpper(), pos.x - 70, pos.y + 42, 140, 16, 11, ThemeRole.InkLow, false,
              TextAlignmentOptions.Center);
        }

        // ตำแหน่งเริ่ม: ที่เลื่อนไว้ครั้งก่อน หรือเล็งโหนดที่เลือก
        Vector2 p0;
        if (mapScroll.HasValue) p0 = mapScroll.Value;
        else
        {
            Vector2 np = NodePos(Cur);
            p0 = new Vector2(-(np.x - MapW / 2f), np.y - MapH / 2f);
        }
        p0.x = Mathf.Clamp(p0.x, -(cw - MapW), 0f);
        p0.y = Mathf.Clamp(p0.y, 0f, ch - MapH);
        c.anchoredPosition = p0;

        // ป้ายและ legend อยู่กับที่ (วางหลัง viewport จึงอยู่บนสุด)
        B(p, 18, 20, 3, 12, ThemeRole.InkLow, 1f);
        L(p, "SECTOR MAP", 28, 14, 300, 24, 12, ThemeRole.InkLow, true);
        L(p, "N", MapW - 60, 14, 42, 24, 12, ThemeRole.InkLow, false, TextAlignmentOptions.MidlineRight);

        float ly = MapH - 30;
        B(p, 18, ly + 4, 10, 10, ThemeRole.InkBright, 1f);
        L(p, "CLEARED", 34, ly, 90, 18, 12, ThemeRole.InkMid);
        Outline(p, 138, ly + 4, 10, 10, ThemeRole.InkBright, 1f);
        L(p, "AVAILABLE", 154, ly, 100, 18, 12, ThemeRole.InkMid);
        Outline(p, 268, ly + 4, 10, 10, ThemeRole.InkLow, 0.6f);
        L(p, "LOCKED", 284, ly, 90, 18, 12, ThemeRole.InkLow);
    }

    // เส้นตรงจาก a ไป b (ทึบ = เปิดอยู่, ประ = ล็อก) พิกัดแบบ TopLeft
    void Line(Transform parent, Vector2 a, Vector2 b, bool live)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        if (len < 1f) return;
        float ang = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg;

        var rt = UiKit.Rect("Line", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(a.x, -a.y);
        rt.sizeDelta = new Vector2(len, 0);
        rt.localRotation = Quaternion.Euler(0, 0, ang);

        if (live) B(rt, 0, 1, len, 2, ThemeRole.InkMid, 0.65f);
        else for (float x = 0; x < len; x += 9f) B(rt, x, 1, Mathf.Min(5f, len - x), 1, ThemeRole.InkMid, 0.35f);
    }

    void BuildDetail()
    {
        var n = Cur;
        var st = StateOf(n);
        bool locked = st == NodeState.Locked;

        var card = Card(dyn, DetX, MapY, DetW, MapH, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        var p = card.rectTransform;
        float iw = DetW - 40f;

        B(p, 20, 20, 3, 12, ThemeRole.InkLow, 1f);
        L(p, "MISSION BRIEF", 30, 14, 200, 24, 12, ThemeRole.InkLow, true);

        // chip สถานะ
        string chip = st == NodeState.Cleared ? "CLEARED" : locked ? "LOCKED" : "AVAILABLE";
        var chipRole = locked ? ThemeRole.Warn : ThemeRole.InkMid;
        Outline(p, DetW - 20 - 96, 14, 96, 24, chipRole, 1f);
        L(p, chip, DetW - 20 - 96, 14, 96, 24, 12, chipRole, false, TextAlignmentOptions.Center);

        L(p, n.id.ToUpper(), 20, 48, 300, 18, 12, ThemeRole.InkLow);
        L(p, n.displayName, 20, 64, iw, 40, 30, ThemeRole.InkBright, true);
        var desc = L(p, n.brief, 20, 110, iw, 100, 13, ThemeRole.InkMid);
        desc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        desc.alignment = TMPro.TextAlignmentOptions.TopLeft;

        B(p, 20, 222, iw, 1, ThemeRole.InkLow, 0.6f);

        int gates = n.gatesToClose;
        int sec = Mathf.RoundToInt(n.timeLimit);
        int threat = Mathf.Clamp(n.threat, 1, 5);
        ThemeRole threatRole = threat >= 4 ? ThemeRole.Danger : threat == 3 ? ThemeRole.Warn : ThemeRole.InkBright;

        // OBJECTIVE
        L(p, "OBJECTIVE", 20, 236, 160, 26, 15, ThemeRole.InkMid, true);
        L(p, "SEAL " + gates + " GATES", 160, 236, iw - 140, 26, 16, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);

        // GATES
        L(p, "GATES", 20, 276, 90, 26, 15, ThemeRole.InkMid, true);
        for (int i = 0; i < gates && i < 8; i++)
        {
            B(p, 110 + i * 36, 280, 30, 18, ThemeRole.InkMid, 0.16f);
            Outline(p, 110 + i * 36, 280, 30, 18, ThemeRole.InkBright, 1f);
        }
        L(p, gates.ToString(), DetW - 20 - 40, 276, 40, 26, 16, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);

        // TIME
        L(p, "TIME LIMIT", 20, 316, 160, 26, 15, ThemeRole.InkMid, true);
        L(p, (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00"), 160, 316, iw - 140, 26, 16,
          ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);

        // THREAT
        L(p, "THREAT", 20, 356, 90, 26, 15, ThemeRole.InkMid, true);
        float sw = (DetW - 20 - 90 - 20 - 100) / 5f;
        for (int i = 0; i < 5; i++)
            B(p, 110 + i * sw, 360, sw - 4, 18, i < threat ? threatRole : ThemeRole.InkMid, i < threat ? 1f : 0.15f);
        L(p, ThreatNames[threat], DetW - 20 - 96, 356, 96, 26, 14, threatRole, false, TextAlignmentOptions.MidlineRight);

        // REWARD
        L(p, "REWARD", 20, 396, 160, 26, 15, ThemeRole.InkMid, true);
        int basePts = n.basePoints * Mathf.Max(1, n.threat);
        L(p, basePts + " PT +", 160, 396, iw - 140, 26, 16, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);

        // หมายเหตุล่าง
        string note;
        ThemeRole noteRole = ThemeRole.InkLow;
        if (locked)
        {
            note = "REQUIRES: CLEAR " + (n.requires != null ? n.requires.displayName.ToUpper() : "?");
            noteRole = ThemeRole.Warn;
        }
        else note = st == NodeState.Cleared ? "REPLAY AVAILABLE" : "ROUTE OPEN";
        L(p, note, 20, MapH - 40, iw, 24, 12, noteRole);
    }

    void BuildFooter()
    {
        var n = Cur;
        var st = n != null ? StateOf(n) : NodeState.Locked;
        bool locked = st == NodeState.Locked;

        var back = Card(dyn, 28, 654, 170, 44, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        Tri(back.rectTransform, 46, 16, 9, 12, ThemeRole.InkMid, 1f, true);
        L(back.rectTransform, "BACK", 12, 0, 170, 44, 16, ThemeRole.InkMid, true, TextAlignmentOptions.Center);
        Hit(back.rectTransform, () => LoadScene(backScene));

        L(dyn, "CLICK A SECTOR TO VIEW BRIEF", 214, 654, 500, 44, 12, ThemeRole.InkLow);

        string label = locked ? "LOCKED" : st == NodeState.Cleared ? "REPLAY MISSION" : "DEPLOY";
        var role = locked ? ThemeRole.InkLow : ThemeRole.InkBright;
        var go = Card(dyn, 972, 652, 280, 48, role, 1f, ThemeRole.Surface, locked ? 0.3f : 0.9f);
        L(go.rectTransform, label, -12, 0, 280, 48, 18, role, true, TextAlignmentOptions.Center);
        Tri(go.rectTransform, 206, 18, 10, 12, role, 1f, false);
        if (!locked) Hit(go.rectTransform, Deploy);
    }

    void Update()
    {
        // วงกะพริบรอบโหนดที่เลือก
        if (ring != null)
            ring.canvasRenderer.SetAlpha(0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.5f)));
    }

    // ---------- การกระทำ ----------
    void Deploy()
    {
        var n = Cur;
        if (n == null || StateOf(n) == NodeState.Locked) return;

        // ทดสอบเปิดหน้านี้ตรงๆ โดยยังไม่ได้เลือกหุ่น: ใช้หุ่นตัวแรกที่ปลดล็อก
        if (GameSession.SelectedMech == null && cfg != null && cfg.mechs != null && cfg.mechs.mechs != null)
            foreach (var m in cfg.mechs.mechs)
                if (m != null && SaveService.IsUnlocked(m)) { GameSession.SelectedMech = m; break; }

        GameSession.SelectedMission = n;
        LoadScene(n.sceneName);
    }
}