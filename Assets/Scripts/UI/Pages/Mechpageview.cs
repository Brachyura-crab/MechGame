using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้า MCH สร้างด้วยโค้ด: ซ้าย = เลือกส่วน (MECH / อาวุธ / สกิล / ULT)  ขวา = รูป ชื่อ และ stat ของส่วนที่เลือก
// วางบน Page_Mech (ยืดเต็มพื้นที่เนื้อหา ห้ามมี Layout Group)  หาข้อมูลจากผู้เล่นตอนรัน
public class MechPageView : MonoBehaviour
{
    [SerializeField] StatDisplayTable table;               // ตารางแสดง stat ของหุ่น
    [SerializeField] int[] skillSlots = { 0, 1 };          // ช่องสกิลที่โชว์ (เลข slot ใน MechSkills)
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;
    [SerializeField] float refreshInterval = 0.2f;

    const float LeftW = 230f, Gap = 14f, SelH = 62f, RowH = 30f, HeaderH = 132f, Cols = 3;

    enum Kind { Mech, Weapon, Skill, Ult }
    class Src { public Kind kind; public int index; public string tag; public string name; }
    struct Entry { public bool heading; public string label, value, delta; public ThemeRole valueRole, deltaRole; }
    class Sel { public CutCornerFrame frame; public TextMeshProUGUI tag, name; public Image led; }
    class Item { public RectTransform root; public TextMeshProUGUI label, value, delta; public Image line, accent; }

    UiKit kit;
    bool built, bound;
    int selected;
    float timer;

    // แหล่งข้อมูล
    MechStats mech; MechSkills skills; UltimateGauge ult;
    readonly List<Weapon> weapons = new List<Weapon>();
    readonly List<Src> srcs = new List<Src>();
    readonly List<Sel> sels = new List<Sel>();
    readonly List<InfoLine> lines = new List<InfoLine>();
    readonly List<Entry> entries = new List<Entry>();
    readonly List<Item> items = new List<Item>();

    RectTransform left, rowsRoot;
    Image portrait; TextMeshProUGUI noImage, nameText, subText;

    void OnEnable()
    {
        if (!built) { kit = new UiKit { headFont = headFont, monoFont = monoFont }; built = true; BuildFrame(); }
        UiThemeSource.Changed += OnTheme;
        timer = 0f;
    }
    void OnDisable() => UiThemeSource.Changed -= OnTheme;
    void OnTheme() { if (built) { kit.Refresh(); StyleSelectors(); } }

    // ---------- โครง ----------
    void BuildFrame()
    {
        var root = (RectTransform)transform;

        left = UiKit.Rect("Selector", root);
        left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0, 1);
        left.pivot = new Vector2(0, 1); left.offsetMin = Vector2.zero; left.offsetMax = new Vector2(LeftW, 0);

        var card = kit.Frame(root, "DetailCard", ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        var crt = card.rectTransform;
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(LeftW + Gap, 0); crt.offsetMax = Vector2.zero;

        // รูป
        var pf = kit.Frame(crt, "PortraitFrame", ThemeRole.InkMid, 0.6f, ThemeRole.Background, 0.5f);
        UiKit.TopLeft(pf.rectTransform, 16, 16, 100, 100);
        var prt = UiKit.Rect("Portrait", pf.rectTransform);
        UiKit.Stretch(prt, 6, 6, 6, 6);
        prt.gameObject.AddComponent<CanvasRenderer>();
        portrait = prt.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true; portrait.raycastTarget = false;
        noImage = kit.Label(pf.rectTransform, "NoImage", "NO IMAGE", 12, ThemeRole.InkDim, false, TextAlignmentOptions.Center);
        UiKit.Stretch(noImage.rectTransform);

        nameText = kit.Label(crt, "Name", "", 30, ThemeRole.InkBright, true);
        UiKit.TopLeft(nameText.rectTransform, 132, 22, 600, 38);
        subText = kit.Label(crt, "Sub", "", 16, ThemeRole.InkDim);
        UiKit.TopLeft(subText.rectTransform, 132, 66, 600, 24);

        var sep = kit.Box(crt, "Sep", ThemeRole.InkMid, 0.4f);
        sep.rectTransform.anchorMin = new Vector2(0, 1); sep.rectTransform.anchorMax = new Vector2(1, 1);
        sep.rectTransform.pivot = new Vector2(0.5f, 1);
        sep.rectTransform.offsetMin = new Vector2(16, -(HeaderH - 6)); sep.rectTransform.offsetMax = new Vector2(-16, -(HeaderH - 7));

        rowsRoot = UiKit.Rect("Rows", crt);
        UiKit.Stretch(rowsRoot, 16, 12, 16, HeaderH + 4);
    }

    // ---------- หาข้อมูล / สร้างตัวเลือกซ้าย ----------
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
        mech = Find<MechStats>(tag);
        if (mech == null) return false;

        skills = Find<MechSkills>(tag);
        ult = Find<UltimateGauge>(tag);
        weapons.Clear();
        weapons.AddRange(tag.GetComponentsInChildren<Weapon>(true));
        weapons.Sort((a, b) => a.IsRightArm.CompareTo(b.IsRightArm));   // ซ้ายก่อนขวา

        srcs.Clear();
        MechData d = mech.Data;
        srcs.Add(new Src { kind = Kind.Mech, tag = "MECH", name = d != null ? d.shortName : "" });
        for (int i = 0; i < weapons.Count && i < 2; i++)
            if (weapons[i].Data != null)
                srcs.Add(new Src { kind = Kind.Weapon, index = i, tag = "WEAPON " + (i + 1), name = weapons[i].Data.displayName });
        if (skills != null)
            for (int i = 0; i < skillSlots.Length; i++)
            {
                var sk = skills.GetSkill(skillSlots[i]);
                if (sk != null) srcs.Add(new Src { kind = Kind.Skill, index = skillSlots[i], tag = "SKILL " + (i + 1), name = sk.displayName });
            }
        if (ult != null) srcs.Add(new Src { kind = Kind.Ult, tag = "ULTIMATE", name = "" });

        for (int i = 0; i < srcs.Count; i++) sels.Add(MakeSelector(i));
        selected = Mathf.Clamp(selected, 0, srcs.Count - 1);
        StyleSelectors();
        bound = true;
        return true;
    }

    Sel MakeSelector(int i)
    {
        var s = new Sel();
        int idx = i;
        s.frame = kit.Frame(left, "Sel" + i, ThemeRole.InkMid, 0.45f, ThemeRole.Background, 1f);
        UiKit.TopLeft(s.frame.rectTransform, 0, i * (SelH + 10), LeftW, SelH);
        s.frame.raycastTarget = true;
        var b = s.frame.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None; b.targetGraphic = s.frame;
        b.onClick.AddListener(() => { selected = idx; StyleSelectors(); timer = 0f; });

        s.tag = kit.Label(s.frame.rectTransform, "Tag", srcs[i].tag, 20, ThemeRole.InkBright, true);
        UiKit.TopLeft(s.tag.rectTransform, 16, 8, LeftW - 50, 26);
        s.name = kit.Label(s.frame.rectTransform, "Name", srcs[i].name, 13, ThemeRole.InkDim);
        UiKit.TopLeft(s.name.rectTransform, 16, 36, LeftW - 50, 20);
        s.led = kit.Box(s.frame.rectTransform, "Led", ThemeRole.InkLow, 0.5f);
        s.led.rectTransform.anchorMin = s.led.rectTransform.anchorMax = s.led.rectTransform.pivot = new Vector2(1, 0.5f);
        s.led.rectTransform.sizeDelta = new Vector2(8, 8); s.led.rectTransform.anchoredPosition = new Vector2(-14, 0);
        return s;
    }

    void StyleSelectors()
    {
        for (int i = 0; i < sels.Count; i++)
        {
            bool on = i == selected; var s = sels[i];
            s.frame.SetStyle(ThemeRole.InkMid, on ? 0.9f : 0.45f, on ? ThemeRole.InkDim : ThemeRole.Background, 1f);
            var c = UiThemeSource.Role(on ? ThemeRole.InkBright : ThemeRole.InkLow); c.a = on ? 1f : 0.5f;
            s.led.color = c;
            var t = UiThemeSource.Role(on ? ThemeRole.InkBright : ThemeRole.InkMid); t.a = on ? 1f : 0.7f;
            s.tag.color = t;
        }
    }

    // ---------- อัปเดต ----------
    void Update()
    {
        if (!built || !Bind()) return;
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = refreshInterval;
        Refresh();
    }

    void Refresh()
    {
        entries.Clear();
        Src src = srcs[selected];
        Sprite img = null; string title = "", sub = "";
        bool deltaCol = false;

        switch (src.kind)
        {
            case Kind.Mech:
                MechData d = mech.Data;
                if (d != null) { img = d.portrait; title = d.displayName; sub = "CODE: " + d.code; }
                BuildMech(); deltaCol = true;
                break;
            case Kind.Weapon:
                Weapon w = weapons[src.index];
                img = w.Data.icon; title = w.Data.displayName;
                sub = w.IsRightArm ? "RIGHT ARM WEAPON" : "LEFT ARM WEAPON";
                BuildWeapon(w);
                break;
            case Kind.Skill:
                SkillData sk = skills.GetSkill(src.index);
                img = sk.icon; title = sk.displayName; sub = "SKILL";
                BuildSkill(sk, src.index);
                break;
            default:
                title = "ULTIMATE"; sub = "SPECIAL ABILITY";
                BuildUlt();
                break;
        }

        portrait.sprite = img;
        portrait.enabled = img != null;
        noImage.gameObject.SetActive(img == null);
        nameText.text = title; subText.text = sub;
        Layout(deltaCol);
    }

    static Entry Head(string t) => new Entry { heading = true, label = t };
    static Entry Row(string l, string v, ThemeRole vr = ThemeRole.InkBright) =>
        new Entry { label = l, value = v, valueRole = vr };

    void BuildMech()
    {
        if (table == null || table.entries == null) { entries.Add(Head("NO STAT TABLE")); return; }
        StatBlock s = mech.Stats;
        string last = null;
        foreach (var e in table.entries)
        {
            if (!e.visible) continue;
            if (e.group != last) { entries.Add(Head(e.group)); last = e.group; }
            float v = s.Get(e.stat), b = s.GetBase(e.stat);
            bool same = Mathf.Approximately(v, b);
            string delta = ""; ThemeRole dr = ThemeRole.InkBright;
            if (!same)
            {
                delta = Mathf.Abs(b) > 0.0001f
                    ? ((v - b) / Mathf.Abs(b) * 100f).ToString("+0;-0") + "%"
                    : (v - b > 0 ? "+" : "") + StatDisplayTable.Format(v - b, e.format);
                bool better = e.lowerIsBetter ? v < b : v > b;
                dr = better ? ThemeRole.InkBright : ThemeRole.Warn;
            }
            entries.Add(new Entry { label = e.label, value = StatDisplayTable.Format(v, e.format),
                valueRole = same ? ThemeRole.InkMid : ThemeRole.InkBright, delta = delta, deltaRole = dr });
        }
    }

    void BuildWeapon(Weapon w)
    {
        lines.Clear();
        w.Data.GetInfo(mech, lines);
        entries.Add(Head("SPECS"));
        foreach (var l in lines) entries.Add(Row(l.label, l.value));

        entries.Add(Head("STATUS"));
        entries.Add(Row("AMMO", w.Ammo + "/" + w.Data.magazineSize));
        int reserve = w.Reserve;
        entries.Add(Row("RESERVE", reserve < 0 ? "UNLIMITED" : reserve.ToString(), reserve == 0 ? ThemeRole.Danger : ThemeRole.InkBright));
        float eff = w.ModuleEfficiency;
        entries.Add(Row("SYSTEM", Mathf.RoundToInt(eff * 100f) + "%", eff < 0.2f ? ThemeRole.Danger : eff < 0.4f ? ThemeRole.Warn : ThemeRole.InkBright));
    }

    void BuildSkill(SkillData sk, int slot)
    {
        lines.Clear();
        sk.GetInfo(mech, lines);
        entries.Add(Head("SPECS"));
        foreach (var l in lines) entries.Add(Row(l.label, l.value));

        entries.Add(Head("STATUS"));
        int loaded = skills.Loaded(slot);
        if (loaded >= 0)
        {
            entries.Add(Row("LOADED", loaded + "/" + sk.MagazineSize));
            int reserve = skills.Reserve(slot);
            entries.Add(Row("RESERVE", reserve < 0 ? "UNLIMITED" : reserve.ToString(), reserve == 0 ? ThemeRole.Danger : ThemeRole.InkBright));
        }
        float rem = skills.CooldownRemaining(slot);
        entries.Add(Row("STATE", rem > 0f ? rem.ToString("0.0") + "s" : "READY", rem > 0f ? ThemeRole.Warn : ThemeRole.InkBright));
    }

    void BuildUlt()
    {
        entries.Add(Head("SPECS"));
        entries.Add(Row("GAUGE MAX", ult.Max.ToString("0.#")));
        entries.Add(Row("DURATION", ult.Duration.ToString("0.#") + "s"));
        entries.Add(Head("STATUS"));
        entries.Add(Row("GAUGE", Mathf.FloorToInt(ult.Ratio * 100f) + "%"));
        string st = ult.IsActive ? "ACTIVE " + ult.ActiveRemaining.ToString("0.0") + "s" : ult.IsFull ? "READY" : "CHARGING";
        entries.Add(Row("STATE", st, ult.IsActive ? ThemeRole.Warn : ThemeRole.InkBright));
    }

    // ---------- จัดวางแถวเป็นคอลัมน์ (กลุ่มอยู่ติดกัน) ----------
    void Layout(bool deltaCol)
    {
        Rect r = rowsRoot.rect;
        int rowsMax = Mathf.Max(1, Mathf.FloorToInt(r.height / RowH));
        float colGap = 24f;
        float colW = (r.width - colGap * (Cols - 1)) / Cols;

        int col = 0, row = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            // เริ่มกลุ่มใหม่ถ้าไม่พอที่ทั้งกลุ่มให้ขึ้นคอลัมน์ใหม่
            if (entries[i].heading && row > 0)
            {
                int size = 1;
                for (int j = i + 1; j < entries.Count && !entries[j].heading; j++) size++;
                if (row + size > rowsMax) { col++; row = 0; }
            }
            if (row >= rowsMax) { col++; row = 0; }

            while (items.Count <= i) items.Add(MakeItem(items.Count));
            Item it = items[i];
            it.root.gameObject.SetActive(col < Cols);
            Entry e = entries[i];

            UiKit.TopLeft(it.root, col * (colW + colGap), row * RowH, colW, RowH);
            it.accent.gameObject.SetActive(e.heading);
            it.value.gameObject.SetActive(!e.heading);
            it.delta.gameObject.SetActive(!e.heading && deltaCol);

            it.label.text = e.label;
            it.label.color = UiThemeSource.Role(e.heading ? ThemeRole.InkMid : ThemeRole.InkBright);
            it.label.fontSize = e.heading ? 14 : 15;
            it.line.color = Alpha(UiThemeSource.Role(ThemeRole.InkMid), e.heading ? 0.5f : 0.18f);

            if (!e.heading)
            {
                it.value.text = e.value;
                it.value.color = UiThemeSource.Role(e.valueRole);
                it.value.rectTransform.offsetMax = new Vector2(deltaCol ? -50 : 0, 0);
                it.delta.text = e.delta ?? "";
                it.delta.color = UiThemeSource.Role(e.deltaRole);
            }
            row++;
        }
        for (int i = entries.Count; i < items.Count; i++) items[i].root.gameObject.SetActive(false);
    }

    static Color Alpha(Color c, float a) { c.a = a; return c; }

    Item MakeItem(int i)
    {
        var it = new Item();
        it.root = UiKit.Rect("Item" + i, rowsRoot);
        it.accent = kit.Box(it.root, "Accent", ThemeRole.InkMid, 1f);
        it.accent.rectTransform.anchorMin = new Vector2(0, 0); it.accent.rectTransform.anchorMax = new Vector2(0, 1);
        it.accent.rectTransform.offsetMin = new Vector2(0, 6); it.accent.rectTransform.offsetMax = new Vector2(3, -6);

        it.label = kit.Label(it.root, "Label", "", 15, ThemeRole.InkBright, true, TextAlignmentOptions.MidlineLeft);
        UiKit.Stretch(it.label.rectTransform); it.label.rectTransform.offsetMin = new Vector2(8, 0);

        it.value = kit.Label(it.root, "Value", "", 18, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
        UiKit.Stretch(it.value.rectTransform);

        it.delta = kit.Label(it.root, "Delta", "", 12, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
        it.delta.rectTransform.anchorMin = new Vector2(1, 0); it.delta.rectTransform.anchorMax = new Vector2(1, 1);
        it.delta.rectTransform.pivot = new Vector2(1, 0.5f);
        it.delta.rectTransform.sizeDelta = new Vector2(46, 0); it.delta.rectTransform.anchoredPosition = Vector2.zero;

        it.line = kit.Box(it.root, "Line", ThemeRole.InkMid, 0.18f);
        it.line.rectTransform.anchorMin = Vector2.zero; it.line.rectTransform.anchorMax = new Vector2(1, 0);
        it.line.rectTransform.offsetMin = Vector2.zero; it.line.rectTransform.offsetMax = new Vector2(0, 1);
        return it;
    }
}