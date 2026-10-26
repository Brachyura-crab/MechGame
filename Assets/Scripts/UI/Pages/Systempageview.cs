using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้า SYS สร้างด้วยโค้ด: ซ้าย = ซิลลูเอตหุ่น (กดเลือกชิ้นส่วน) + repair kit  ขวา = VITALS + โมดูลของชิ้นที่เลือก (กดเพื่อซ่อม)
// วางบน Page_System (ยืดเต็มพื้นที่เนื้อหา ห้ามมี Layout Group)
public class SystemPageView : MonoBehaviour
{
    [Header("ชิ้นส่วน (ลาก BodyPartData asset มาใส่ ว่างได้)")]
    [SerializeField] BodyPartData head;
    [SerializeField] BodyPartData torso;
    [SerializeField] BodyPartData armL;
    [SerializeField] BodyPartData armR;
    [SerializeField] BodyPartData legL;
    [SerializeField] BodyPartData legR;
    [SerializeField] BodyPartData defaultPart;   // ว่าง = torso

    [Header("Fonts (ว่างได้)")]
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;
    [SerializeField] float refreshInterval = 0.1f;

    const float LeftW = 420f, Gap = 14f, VitalsH = 176f, RowH = 54f;
    const int Segs = 20;

    class Part
    {
        public BodyPartData data; public RectTransform rt; public Image fill;
        public Image[] edges = new Image[4]; public Image[] cross = new Image[2];
        public TextMeshProUGUI pct;
    }
    class Bar { public Image[] segs = new Image[Segs]; public TextMeshProUGUI value; }
    class ModRow
    {
        public RectTransform root; public CutCornerFrame frame; public Button button;
        public TextMeshProUGUI name, state, info; public Image prog; public RectTransform progRt;
        public ModuleData module; public int lastTier = -2; public bool lastRepair;
    }

    UiKit kit;
    bool built, bound;
    float timer;

    ModuleSystem system; Health health; Energy energy; ResourceInventory inventory;
    BodyPartData selected;
    readonly List<Part> parts = new List<Part>();
    readonly List<ModuleData> buf = new List<ModuleData>();
    readonly List<ModRow> rows = new List<ModRow>();
    Bar hpBar, shBar, enBar;
    RectTransform silArea, rowsRoot, contentRt;
    TextMeshProUGUI caption, kitCount, modHeading, emptyText;

    void OnEnable()
    {
        if (!built) { kit = new UiKit { headFont = headFont, monoFont = monoFont }; built = true; BuildFrame(); }
        UiThemeSource.Changed += OnTheme;
        timer = 0f;
    }
    void OnDisable() => UiThemeSource.Changed -= OnTheme;
    void OnTheme() { if (built) { kit.Refresh(); timer = 0f; } }

    // ---------- โครง ----------
    CutCornerFrame Card(RectTransform parent, string name)
    {
        return kit.Frame(parent, name, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
    }

    void BuildFrame()
    {
        var root = (RectTransform)transform;

        // ----- ซ้าย -----
        var lc = Card(root, "IntegrityCard");
        var lrt = lc.rectTransform;
        lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(0, 1); lrt.pivot = new Vector2(0, 0.5f);
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = new Vector2(LeftW, 0);
        Heading(lrt, "UNIT INTEGRITY");

        silArea = UiKit.Rect("Silhouette", lrt);
        UiKit.Stretch(silArea, 16, 96, 16, 40);

        caption = kit.Label(lrt, "Caption", "", 15, ThemeRole.InkMid, true, TextAlignmentOptions.Center);
        caption.rectTransform.anchorMin = new Vector2(0, 0); caption.rectTransform.anchorMax = new Vector2(1, 0);
        caption.rectTransform.pivot = new Vector2(0.5f, 0);
        caption.rectTransform.offsetMin = new Vector2(16, 66); caption.rectTransform.offsetMax = new Vector2(-16, 90);

        var kitLine = kit.Box(lrt, "KitLine", ThemeRole.InkMid, 0.4f);
        kitLine.rectTransform.anchorMin = new Vector2(0, 0); kitLine.rectTransform.anchorMax = new Vector2(1, 0);
        kitLine.rectTransform.offsetMin = new Vector2(16, 62); kitLine.rectTransform.offsetMax = new Vector2(-16, 63);

        var plus = kit.Frame(lrt, "KitIcon", ThemeRole.InkBright, 1f, ThemeRole.Surface, 0f);
        plus.rectTransform.anchorMin = plus.rectTransform.anchorMax = plus.rectTransform.pivot = new Vector2(0, 0);
        plus.rectTransform.sizeDelta = new Vector2(36, 36); plus.rectTransform.anchoredPosition = new Vector2(16, 14);
        var pt = kit.Label(plus.rectTransform, "Plus", "+", 26, ThemeRole.InkBright, true, TextAlignmentOptions.Center);
        UiKit.Stretch(pt.rectTransform);
        var kl = kit.Label(lrt, "KitLabel", "REPAIR KIT", 16, ThemeRole.InkMid, true, TextAlignmentOptions.MidlineLeft);
        kl.rectTransform.anchorMin = kl.rectTransform.anchorMax = kl.rectTransform.pivot = new Vector2(0, 0);
        kl.rectTransform.sizeDelta = new Vector2(200, 36); kl.rectTransform.anchoredPosition = new Vector2(62, 14);
        kitCount = kit.Label(lrt, "KitCount", "x00", 32, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
        kitCount.rectTransform.anchorMin = kitCount.rectTransform.anchorMax = kitCount.rectTransform.pivot = new Vector2(1, 0);
        kitCount.rectTransform.sizeDelta = new Vector2(160, 40); kitCount.rectTransform.anchoredPosition = new Vector2(-16, 12);

        BuildSilhouette();

        // ----- ขวา -----
        var vc = Card(root, "VitalsCard");
        var vrt = vc.rectTransform;
        vrt.anchorMin = new Vector2(0, 1); vrt.anchorMax = new Vector2(1, 1); vrt.pivot = new Vector2(0.5f, 1);
        vrt.offsetMin = new Vector2(LeftW + Gap, -VitalsH); vrt.offsetMax = Vector2.zero;
        Heading(vrt, "VITALS");
        hpBar = MakeBar(vrt, "HP", 0);
        shBar = MakeBar(vrt, "SHIELD", 1);
        enBar = MakeBar(vrt, "ENERGY", 2);

        var mc = Card(root, "ModulesCard");
        var mrt = mc.rectTransform;
        mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
        mrt.offsetMin = new Vector2(LeftW + Gap, 0); mrt.offsetMax = new Vector2(0, -(VitalsH + Gap));
        modHeading = Heading(mrt, "MODULES");
        rowsRoot = UiKit.Rect("Rows", mrt);
        UiKit.Stretch(rowsRoot, 16, 12, 16, 44);
                rowsRoot.gameObject.AddComponent<RectMask2D>();
        rowsRoot.gameObject.AddComponent<CanvasRenderer>();
        var hit = rowsRoot.gameObject.AddComponent<Image>();   // โปร่งใส ไว้รับล้อเมาส์
        hit.color = Color.clear; hit.raycastTarget = true;

        contentRt = UiKit.Rect("Content", rowsRoot);
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.offsetMin = Vector2.zero; contentRt.offsetMax = Vector2.zero;

        var sr = rowsRoot.gameObject.AddComponent<ScrollRect>();
        sr.content = contentRt; sr.viewport = rowsRoot;
        sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;
        emptyText = kit.Label(mrt, "Empty", "NO MODULES IN THIS SECTION", 16, ThemeRole.InkDim, true, TextAlignmentOptions.Center);
        UiKit.Stretch(emptyText.rectTransform);
    }

    TextMeshProUGUI Heading(RectTransform p, string text)
    {
        var t = kit.Label(p, "Heading", text, 16, ThemeRole.InkMid, true);
        UiKit.TopLeft(t.rectTransform, 16, 10, 520, 22);
        return t;
    }

    Bar MakeBar(RectTransform parent, string label, int i)
    {
        float y = 40 + i * 42;
        var b = new Bar();
        var l = kit.Label(parent, "L" + label, label, 16, ThemeRole.InkBright, true, TextAlignmentOptions.MidlineLeft);
        UiKit.TopLeft(l.rectTransform, 16, y, 90, 30);

        var segRoot = UiKit.Rect("Bar" + label, parent);
        segRoot.anchorMin = new Vector2(0, 1); segRoot.anchorMax = new Vector2(1, 1); segRoot.pivot = new Vector2(0.5f, 1);
        segRoot.offsetMin = new Vector2(112, -(y + 25)); segRoot.offsetMax = new Vector2(-130, -(y + 5));
        for (int s = 0; s < Segs; s++)
        {
            var img = kit.Box(segRoot, "S" + s, ThemeRole.InkLow, 0.3f);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(s / (float)Segs, 0); rt.anchorMax = new Vector2((s + 1) / (float)Segs, 1);
            rt.offsetMin = new Vector2(1.5f, 0); rt.offsetMax = new Vector2(-1.5f, 0);
            b.segs[s] = img;
        }
        b.value = kit.Label(parent, "V" + label, "", 18, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
        b.value.rectTransform.anchorMin = b.value.rectTransform.anchorMax = b.value.rectTransform.pivot = new Vector2(1, 1);
        b.value.rectTransform.sizeDelta = new Vector2(116, 30); b.value.rectTransform.anchoredPosition = new Vector2(-14, -y);
        return b;
    }

    // ---------- ซิลลูเอต (พิกัดตาม mockup: viewBox 200x250) ----------
    void BuildSilhouette()
    {
        var holder = UiKit.Rect("Holder", silArea);
        holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0.5f, 0.5f);
        holder.sizeDelta = new Vector2(200, 250);
        silHolder = holder;

        MakePart(head,  78,  6, 44,  34);
        MakePart(torso, 62, 48, 76,  86);
        MakePart(armL,  14, 52, 38,  92);
        MakePart(armR, 148, 52, 38,  92);
        MakePart(legL,  66, 142, 30, 96);
        MakePart(legR, 104, 142, 30, 96);
    }
    RectTransform silHolder;

    void MakePart(BodyPartData data, float x, float y, float w, float h)
    {
        if (data == null) return;
        var p = new Part { data = data };
        var f = kit.Box(silHolder, "Part_" + data.displayName, ThemeRole.InkBright, 0.14f);
        p.fill = f; p.rt = f.rectTransform;
        // พิกัดแบบ SVG (y ลง) -> anchor จากมุมบนซ้ายของ holder
        p.rt.anchorMin = p.rt.anchorMax = p.rt.pivot = new Vector2(0, 1);
        p.rt.anchoredPosition = new Vector2(x, -y); p.rt.sizeDelta = new Vector2(w, h);
        f.raycastTarget = true;
        var b = f.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None; b.targetGraphic = f;
        var cap = data;
        b.onClick.AddListener(() => { selected = cap; timer = 0f; });

        for (int i = 0; i < 4; i++)
        {
            var e = kit.Box(p.rt, "Edge" + i, ThemeRole.InkBright, 1f);
            var r = e.rectTransform;
            switch (i)
            {
                case 0: r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.offsetMin = new Vector2(0, -1.5f); r.offsetMax = Vector2.zero; break;
                case 1: r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.offsetMin = Vector2.zero; r.offsetMax = new Vector2(0, 1.5f); break;
                case 2: r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0, 1); r.offsetMin = Vector2.zero; r.offsetMax = new Vector2(1.5f, 0); break;
                default: r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, 1); r.offsetMin = new Vector2(-1.5f, 0); r.offsetMax = Vector2.zero; break;
            }
            p.edges[i] = e;
        }
        for (int i = 0; i < 2; i++)
        {
            var c = kit.Box(p.rt, "X" + i, ThemeRole.Warn, 1f);
            var r = c.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(Mathf.Min(w, h) * 0.8f * 1.41f, 1.5f);
            r.localRotation = Quaternion.Euler(0, 0, i == 0 ? 45 : -45);
            p.cross[i] = c;
        }
        parts.Add(p);
    }

    // ---------- ผูกข้อมูล ----------
    static T Find<T>(PlayerTag tag) where T : Component
    {
        var c = tag.GetComponentInChildren<T>(true);
        if (c == null) c = tag.GetComponentInParent<T>();
        return c;
    }

    bool Bind()
    {
        if (bound) return true;
        var tag = PlayerTag.Instance;
        if (tag == null) return false;
        system = Find<ModuleSystem>(tag);
        health = Find<Health>(tag);
        energy = Find<Energy>(tag);
        inventory = Find<ResourceInventory>(tag);
        if (system == null || health == null || energy == null) return false;
        selected = defaultPart != null ? defaultPart : torso;
        bound = true;
        return true;
    }

    // ---------- อัปเดต ----------
    void Update()
    {
        if (!built || !Bind()) return;
        ScaleSilhouette();
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = refreshInterval;
        RefreshParts(); RefreshVitals(); RefreshModules(); RefreshKit();
    }

    void ScaleSilhouette()
    {
        Rect a = silArea.rect;
        if (a.width < 10 || a.height < 10) return;
        float s = Mathf.Min(1.7f, Mathf.Min(a.width / 200f, a.height / 250f));
        silHolder.localScale = new Vector3(s, s, 1f);
    }

    float PartEfficiency(BodyPartData d)
    {
        system.GetModules(d, buf);
        if (buf.Count == 0) return 1f;
        float sum = 0f;
        foreach (var m in buf) sum += system.Efficiency(m);
        return sum / buf.Count;
    }

    void RefreshParts()
    {
        foreach (var p in parts)
        {
            ModuleDamage tier = system.PartTier(p.data);
            Color c = UiThemeSource.TierColor(tier);
            bool sel = p.data == selected;
            float blink = 1f;
            if (tier != ModuleDamage.None)
                blink = Mathf.Lerp(0.45f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (int)tier * Mathf.PI * 2f * 0.5f));

            var fc = c; fc.a = sel ? 0.34f : 0.14f; p.fill.color = fc;
            var ec = c; ec.a = (sel ? 1f : 0.7f) * blink;
            foreach (var e in p.edges) e.color = ec;
            bool dmg = tier != ModuleDamage.None;
            foreach (var x in p.cross) { x.gameObject.SetActive(dmg); x.color = ec; }
        }
        if (selected != null)
        {
            int pc = Mathf.RoundToInt(PartEfficiency(selected) * 100f);
            caption.text = selected.displayName.ToUpper() + "   " + pc + "%";
        }
    }

    void RefreshVitals()
    {
        SetBar(hpBar, health.Current, health.Max, true);
        SetBar(shBar, health.Shield, health.MaxShield, false);
        SetBar(enBar, energy.Current, energy.Max, false);
    }

    void SetBar(Bar b, float cur, float max, bool level)
    {
        float ratio = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
        int lit = Mathf.CeilToInt(ratio * Segs);
        Color on = level ? UiThemeSource.LevelColor(ratio) : UiThemeSource.Role(ThemeRole.InkBright);
        Color off = UiThemeSource.Role(ThemeRole.InkLow); off.a = 0.3f;
        for (int i = 0; i < Segs; i++) b.segs[i].color = i < lit ? on : off;
        b.value.text = Mathf.CeilToInt(cur) + "/" + Mathf.CeilToInt(max);
        b.value.color = on;
    }

    void RefreshKit()
    {
        var kt = system.RepairKit;
        kitCount.text = kt == null || inventory == null ? "FREE" : "x" + inventory.Get(kt).ToString("00");
        bool none = kt != null && inventory != null && inventory.Get(kt) == 0;
        kitCount.color = UiThemeSource.Role(none ? ThemeRole.Danger : ThemeRole.InkBright);
    }

    // ---------- โมดูล ----------
    void RefreshModules()
    {
        modHeading.text = "MODULES  //  " + (selected != null ? selected.displayName.ToUpper() : "-");
        system.GetModules(selected, buf);
        emptyText.gameObject.SetActive(buf.Count == 0);
        contentRt.sizeDelta = new Vector2(0, Mathf.Max(0f, buf.Count * (RowH + 8f) - 8f));

        while (rows.Count < buf.Count) rows.Add(MakeRow(rows.Count));
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            r.root.gameObject.SetActive(i < buf.Count);
            if (i >= buf.Count) continue;
            r.module = buf[i];
            UpdateRow(r);
        }
    }

    static string TierName(ModuleDamage t)
    {
        switch (t)
        {
            case ModuleDamage.Light: return "MINR";
            case ModuleDamage.Medium: return "MAJR";
            case ModuleDamage.Heavy: return "FAIL";
            default: return "OK";
        }
    }

    void UpdateRow(ModRow r)
    {
        ModuleData m = r.module;
        ModuleDamage tier = system.Tier(m);
        bool repairing = system.Repairing == m;
        Color c = repairing ? UiThemeSource.Role(ThemeRole.InkBright) : UiThemeSource.TierColor(tier);

        r.name.text = m.displayName; r.name.color = c;
        r.state.text = repairing ? "REPAIRING" : TierName(tier); r.state.color = c;

        if (tier == ModuleDamage.None) r.info.text = "EFFICIENCY 100%";
        else
        {
            int cost = system.RepairCost(m);
            string costStr = cost == 0 ? "FREE" : cost + " KIT";
            r.info.text = "EFFICIENCY " + Mathf.RoundToInt(system.Efficiency(m) * 100f) + "%   REPAIR " + costStr + " / " + system.RepairTime(m).ToString("0.#") + "s";
        }
        r.info.color = UiThemeSource.Role(ThemeRole.InkDim);

        int key = (int)tier * 2 + (repairing ? 1 : 0);
        if (key != r.lastTier)
        {
            r.lastTier = key;
            r.frame.SetStyle(repairing ? ThemeRole.InkBright : ThemeRole.InkMid, tier == ModuleDamage.None && !repairing ? 0.35f : 0.8f,
                             ThemeRole.Surface, repairing ? 0.5f : 0.3f);
        }
        r.prog.color = c;
        r.progRt.anchorMax = new Vector2(repairing ? system.RepairProgress : 0f, 0f);
    }

    ModRow MakeRow(int i)
    {
        var r = new ModRow();
        r.frame = kit.Frame(contentRt, "Row" + i, ThemeRole.InkMid, 0.35f, ThemeRole.Surface, 0.3f);
        r.root = r.frame.rectTransform;
        r.root.anchorMin = new Vector2(0, 1); r.root.anchorMax = new Vector2(1, 1); r.root.pivot = new Vector2(0.5f, 1);
        r.root.offsetMin = new Vector2(0, -(i * (RowH + 8) + RowH)); r.root.offsetMax = new Vector2(0, -(i * (RowH + 8)));
        r.frame.raycastTarget = true;
        r.button = r.frame.gameObject.AddComponent<Button>();
        r.button.transition = Selectable.Transition.None; r.button.targetGraphic = r.frame;
        r.button.onClick.AddListener(() => OnRowClick(r));

        // แถบความคืบหน้าการซ่อม (ล่างสุดของแถว)
        r.prog = kit.Box(r.root, "Prog", ThemeRole.InkBright, 1f);
        r.progRt = r.prog.rectTransform;
        r.progRt.anchorMin = new Vector2(0, 0); r.progRt.anchorMax = new Vector2(0, 0);
        r.progRt.pivot = new Vector2(0, 0);
        r.progRt.offsetMin = Vector2.zero; r.progRt.offsetMax = new Vector2(0, 4);
        r.prog.raycastTarget = false;

        r.name = kit.Label(r.root, "Name", "", 19, ThemeRole.InkBright, true, TextAlignmentOptions.MidlineLeft);
        UiKit.TopLeft(r.name.rectTransform, 16, 4, 420, 26);
        r.info = kit.Label(r.root, "Info", "", 13, ThemeRole.InkDim, false, TextAlignmentOptions.MidlineLeft);
        UiKit.TopLeft(r.info.rectTransform, 16, 30, 640, 20);
        r.state = kit.Label(r.root, "State", "", 18, ThemeRole.InkBright, true, TextAlignmentOptions.MidlineRight);
        r.state.rectTransform.anchorMin = r.state.rectTransform.anchorMax = r.state.rectTransform.pivot = new Vector2(1, 0.5f);
        r.state.rectTransform.sizeDelta = new Vector2(180, 30); r.state.rectTransform.anchoredPosition = new Vector2(-16, 0);
        return r;
    }

    void OnRowClick(ModRow r)
    {
        if (r.module == null) return;
        if (system.Repairing == r.module) system.CancelRepair();
        else if (system.Tier(r.module) != ModuleDamage.None) system.StartRepair(r.module);
        timer = 0f;
    }
}