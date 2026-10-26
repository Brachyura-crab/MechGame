using TMPro;
using UnityEngine;
using UnityEngine.UI;

// เปลือกของหน้าจอสถานะ: แถบหัว (MECH-OS) + แถบแท็บด้านบน + พื้นที่เนื้อหา  สร้างด้วยโค้ดทั้งหมด
// วางบน object ว่างชื่อ Shell (ลูกของ Panel, ยืดเต็มพื้นที่หน้าจอด้านใน, อยู่ใต้ชั้น CRT fx)
// หน้าทั้ง 5 ลากมาใส่ช่อง Pages โค้ดจะย้ายเข้าพื้นที่เนื้อหาให้เอง
public enum TabAlert { None, Caution, Critical }
public class StatusShellView : MonoBehaviour
{
    [Header("หน้า (เรียงตามแท็บ)")]
    [SerializeField] GameObject[] pages;
    [SerializeField] string[] names = { "SYS", "MCH", "STG", "SUP", "MAP" };
    [SerializeField] int startIndex = 0;

    [Header("แจ้งเตือนบนแท็บ (เหลือง = เฝ้าระวัง, แดง = วิกฤต)")]
    [SerializeField] int sysTabIndex = 0;              // แท็บที่เตือนเรื่องโมดูลเสียหาย / เลือดต่ำ
    [SerializeField] int supplyTabIndex = 3;           // แท็บที่เตือนเรื่องเสบียงเหลือน้อย
    [SerializeField, Range(0f, 1f)] float hpCritical = 0.2f;   // เลือดต่ำกว่านี้ = แดง
    [SerializeField] float lowPacks = 1f;              // เสบียงเหลือไม่เกินกี่ชุด = เหลือง (หมด = แดง)
    [SerializeField] float cautionBlinkHz = 1.2f;
    [SerializeField] float criticalBlinkHz = 3f;

    [Header("Layout (หน่วย canvas)")]
    [SerializeField] float headerHeight = 40f;
    [SerializeField] float tabBarHeight = 42f;
    [SerializeField] float tabWidth = 170f;
    [SerializeField] float tabGap = 6f;
    [SerializeField] Vector4 contentPadding = new Vector4(26, 12, 26, 26);   // ซ้าย, บน(ใต้แถบแท็บ), ขวา, ล่าง

    [Header("Fonts (ว่างได้)")]
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;

    class Tab { public CutCornerFrame frame; public TextMeshProUGUI label; public Image led, glow, bar; }

    UiKit kit;
    bool built;
    int current = -1;
    Tab[] tabs;
    RectTransform content;
    MapLines rule; float ruleWidth = -1f;
    Image pulse;
    TextMeshProUGUI sysText, timeText, linkText;
    TextMeshProUGUI linkDot;
    TabAlert[] alerts;
    bool[] wasAlerted;
    float alertTimer;
    ModuleSystem mods;
    SupplyService sup;

    public int Current => Mathf.Max(0, current);

    void OnEnable()
    {
        if (!built) { kit = new UiKit { headFont = headFont, monoFont = monoFont }; built = true; Build(); }
        UiThemeSource.Changed += OnTheme;
        Select(current < 0 ? startIndex : current);
    }
    void OnDisable() => UiThemeSource.Changed -= OnTheme;
    void OnTheme() { if (built) { kit.Refresh(); StyleTabs(); ruleWidth = -1f; } }

    // ---------- สร้าง ----------
    void Build()
    {
        var root = (RectTransform)transform;
        BuildHeader(root);
        BuildTabBar(root);

        content = UiKit.Rect("Content", root);
        UiKit.Stretch(content, contentPadding.x, contentPadding.w, contentPadding.z,
                      headerHeight + tabBarHeight + contentPadding.y);

        if (pages != null)
            foreach (var p in pages)
            {
                if (p == null) continue;
                var rt = (RectTransform)p.transform;
                rt.SetParent(content, false);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localScale = Vector3.one;
            }
    }

    void BuildHeader(RectTransform root)
    {
        var h = UiKit.Rect("Header", root);
        h.anchorMin = new Vector2(0, 1); h.anchorMax = new Vector2(1, 1); h.pivot = new Vector2(0.5f, 1);
        h.offsetMin = new Vector2(26, -headerHeight); h.offsetMax = new Vector2(-26, 0);

        pulse = kit.Box(h, "Pulse", ThemeRole.InkBright, 1f);
        pulse.rectTransform.anchorMin = pulse.rectTransform.anchorMax = pulse.rectTransform.pivot = new Vector2(0, 0.5f);
        pulse.rectTransform.sizeDelta = new Vector2(10, 10); pulse.rectTransform.anchoredPosition = new Vector2(0, 0);

        var title = kit.Label(h, "Title", "MECH-OS", 22, ThemeRole.InkBright, true, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, 22, 140);
        var ver = kit.Label(h, "Version", "v4.2 // STATUS TERMINAL", 13, ThemeRole.InkDim, false, TextAlignmentOptions.MidlineLeft);
        Place(ver.rectTransform, 170, 240);

        // ขวา: LINK / T+ / SYS (เรียงจากขวา)
        linkText = kit.Label(h, "Link", "LINK", 14, ThemeRole.Warn, false, TextAlignmentOptions.MidlineRight);
        PlaceRight(linkText.rectTransform, 0, 74);
        linkDot = kit.Label(h, "LinkDot", "●", 14, ThemeRole.Warn, false, TextAlignmentOptions.MidlineLeft);
        linkDot.rectTransform.anchorMin = linkDot.rectTransform.anchorMax = linkDot.rectTransform.pivot = new Vector2(1, 0.5f);
        linkDot.rectTransform.sizeDelta = new Vector2(20, 24); linkDot.rectTransform.anchoredPosition = new Vector2(0, 0);
        linkText.rectTransform.anchoredPosition = new Vector2(-20, 0);

        timeText = kit.Label(h, "Time", "T+ 00:00", 14, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
        PlaceRight(timeText.rectTransform, 118, 110);
        sysText = kit.Label(h, "Sys", "SYS ONLINE", 14, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
        PlaceRight(sysText.rectTransform, 246, 140);

        // เส้นขีดตรงกลาง
        var rt = UiKit.Rect("Rule", h);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(430, 4); rt.offsetMax = new Vector2(-410, -4);
        rule = rt.gameObject.AddComponent<MapLines>();
        rule.raycastTarget = false;
        rule.color = Alpha(UiThemeSource.Role(ThemeRole.InkMid), 0.5f);
    }

    static void Place(RectTransform rt, float x, float w)
    {
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(w, 0); rt.anchoredPosition = new Vector2(x, 0);
    }
    static void PlaceRight(RectTransform rt, float fromRight, float w)
    {
        rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 0.5f);
        rt.sizeDelta = new Vector2(w, 0); rt.anchoredPosition = new Vector2(-fromRight, 0);
    }
    static Color Alpha(Color c, float a) { c.a = a; return c; }

    void BuildTabBar(RectTransform root)
    {
        var bar = UiKit.Rect("TabBar", root);
        bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1); bar.pivot = new Vector2(0.5f, 1);
        bar.offsetMin = new Vector2(26, -(headerHeight + tabBarHeight)); bar.offsetMax = new Vector2(-26, -headerHeight);

        var line = kit.Box(bar, "Line", ThemeRole.InkBright, 0.9f);
        line.rectTransform.anchorMin = new Vector2(0, 0); line.rectTransform.anchorMax = new Vector2(1, 0);
        line.rectTransform.offsetMin = Vector2.zero; line.rectTransform.offsetMax = new Vector2(0, 2);

        int n = pages != null ? pages.Length : 0;
        tabs = new Tab[n];
        for (int i = 0; i < n; i++)
        {
            int idx = i;
            var t = new Tab();
            t.frame = kit.Frame(bar, "Tab" + i, ThemeRole.InkMid, 0.45f, ThemeRole.Background, 1f);
            var rt = t.frame.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(tabWidth, tabBarHeight - 4);
            rt.anchoredPosition = new Vector2(i * (tabWidth + tabGap), 2);
            t.frame.raycastTarget = true;
            var b = t.frame.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None; b.targetGraphic = t.frame;
            b.onClick.AddListener(() => Select(idx));

            // ชั้นแจ้งเตือน (อยู่ใต้ตัวหนังสือ): แสงจางทั้งแท็บ + แถบบาง ๆ ด้านบน ปิดไว้จนกว่าจะมีปัญหา
            t.glow = kit.Box(rt, "AlertGlow", ThemeRole.InkBright, 0f);
            UiKit.Stretch(t.glow.rectTransform, 2, 2, 2, 2);
            t.glow.enabled = false;
            t.bar = kit.Box(rt, "AlertBar", ThemeRole.InkBright, 0f);
            t.bar.rectTransform.anchorMin = new Vector2(0, 1); t.bar.rectTransform.anchorMax = new Vector2(1, 1);
            t.bar.rectTransform.pivot = new Vector2(0.5f, 1);
            t.bar.rectTransform.offsetMin = new Vector2(0, -3); t.bar.rectTransform.offsetMax = Vector2.zero;
            t.bar.enabled = false;

            string nm = names != null && i < names.Length ? names[i] : ("TAB " + (i + 1));
            t.label = kit.Label(rt, "Label", nm, 17, ThemeRole.InkBright, true, TextAlignmentOptions.MidlineLeft);
            t.label.characterSpacing = 6f;
            UiKit.Stretch(t.label.rectTransform, 16, 0, 30, 0);

            t.led = kit.Box(rt, "Led", ThemeRole.InkLow, 0.5f);
            t.led.rectTransform.anchorMin = t.led.rectTransform.anchorMax = t.led.rectTransform.pivot = new Vector2(1, 0.5f);
            t.led.rectTransform.sizeDelta = new Vector2(7, 7); t.led.rectTransform.anchoredPosition = new Vector2(-14, 0);
            tabs[i] = t;
        }
        
        alerts = new TabAlert[n];
        wasAlerted = new bool[n];
    }

    // ---------- เลือกแท็บ ----------
    public void Select(int index)
    {
        if (pages == null || pages.Length == 0) return;
        current = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null) pages[i].SetActive(i == current);
        if (built) StyleTabs();
    }

    void StyleTabs()
    {
        if (tabs == null) return;
        for (int i = 0; i < tabs.Length; i++) StyleTab(i);
    }

    void StyleTab(int i)
    {
        bool on = i == current; var t = tabs[i];
        t.frame.SetStyle(ThemeRole.InkMid, on ? 0.85f : 0.45f, on ? ThemeRole.InkDim : ThemeRole.Background, 1f);
        var lc = UiThemeSource.Role(on ? ThemeRole.InkBright : ThemeRole.InkMid); lc.a = on ? 1f : 0.6f;
        t.label.color = lc;
        var c = UiThemeSource.Role(on ? ThemeRole.InkBright : ThemeRole.InkLow); c.a = on ? 1f : 0.5f;
        t.led.color = c;
        t.glow.enabled = false;
        t.bar.enabled = false;
    }

    // ---------- แจ้งเตือนบนแท็บ ----------
    void SetAlert(int tab, TabAlert a)
    {
        if (alerts != null && tab >= 0 && tab < alerts.Length) alerts[tab] = a;
    }

    // ตรวจเงื่อนไขทุกช่วงเวลา ไม่ต้องทุกเฟรม
    void RefreshAlerts()
    {
        var tag = PlayerTag.Instance;
        if (tag != null)
        {
            if (mods == null) mods = tag.GetComponentInChildren<ModuleSystem>(true);
            if (mods == null) mods = tag.GetComponentInParent<ModuleSystem>();
            if (sup == null) sup = tag.GetComponentInChildren<SupplyService>(true);
            if (sup == null) sup = tag.GetComponentInParent<SupplyService>();
        }

        // SYS: โมดูลเสียหาย / เลือดต่ำ
        TabAlert sys = TabAlert.None;
        if (mods != null)
            foreach (var m in mods.Modules)
            {
                if (m == null) continue;
                var tier = mods.Tier(m);
                if (tier == ModuleDamage.Heavy) { sys = TabAlert.Critical; break; }
                if (tier != ModuleDamage.None) sys = TabAlert.Caution;
            }
        EnemyTarget.Ensure();
        var hp = EnemyTarget.PlayerHealth;
        if (hp != null && hp.Max > 0f && hp.Current / hp.Max < hpCritical) sys = TabAlert.Critical;
        SetAlert(sysTabIndex, sys);

        // SUP: เสบียงเหลือน้อย / หมด
        TabAlert sp = TabAlert.None;
        if (sup != null && sup.Inventory != null)
            foreach (var it in sup.Items)
            {
                if (it.type == null) continue;
                int amt = sup.Inventory.Get(it.type);
                if (amt <= 0) { sp = TabAlert.Critical; break; }
                if (amt <= it.amount * lowPacks) sp = TabAlert.Caution;
            }
        SetAlert(supplyTabIndex, sp);
    }

    // ทุกเฟรม: แท็บที่มีปัญหาเปลี่ยนสีและกะพริบ ปัญหาหายแล้วคืนสีธีม
    void ApplyAlerts()
    {
        if (tabs == null || alerts == null) return;
        for (int i = 0; i < tabs.Length; i++)
        {
            var a = alerts[i];
            var t = tabs[i];

            if (a == TabAlert.None)
            {
                if (wasAlerted[i]) { wasAlerted[i] = false; StyleTab(i); }
                continue;
            }
            wasAlerted[i] = true;

            bool crit = a == TabAlert.Critical;
            Color col = crit ? UiThemeSource.Critical : UiThemeSource.Caution;
            float hz = crit ? criticalBlinkHz : cautionBlinkHz;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);
            float pulse = Mathf.Lerp(crit ? 0.3f : 0.45f, 1f, wave);

            Color c = col; c.a = pulse;
            t.label.color = c;
            t.led.color = c;

            t.bar.enabled = true;
            t.bar.color = c;

            t.glow.enabled = true;
            Color g = col; g.a = Mathf.Lerp(0.04f, crit ? 0.26f : 0.18f, wave);
            t.glow.color = g;
        }
    }

    // ---------- อัปเดตหัว ----------
    void Update()
    {
        if (!built) return;

        float t = Time.unscaledTime;
        var pc = UiThemeSource.Role(ThemeRole.InkBright);
        pc.a = 0.55f + 0.45f * Mathf.Sin(t * 3f);
        pulse.color = pc;

        var lc = UiThemeSource.Role(ThemeRole.Warn);
        lc.a = Mathf.Sin(t * 4f) > 0f ? 1f : 0.2f;
        linkDot.color = lc;

        var run = RunManager.Instance;
        float el = run != null ? run.Elapsed : 0f;
        int s = Mathf.FloorToInt(el);
        timeText.text = "T+ " + (s / 60).ToString("00") + ":" + (s % 60).ToString("00");

        bool crit = false;
        EnemyTarget.Ensure();
        var hp = EnemyTarget.PlayerHealth;
        if (hp != null && hp.Max > 0f) crit = hp.Current / hp.Max < 0.2f;
        sysText.text = crit ? "SYS CRITICAL" : "SYS ONLINE";
        sysText.color = UiThemeSource.Role(crit ? ThemeRole.Danger : ThemeRole.InkBright);

        float w = ((RectTransform)rule.transform).rect.width;
        if (w > 1f && !Mathf.Approximately(w, ruleWidth))
        {
            ruleWidth = w;
            rule.color = Alpha(UiThemeSource.Role(ThemeRole.InkMid), 0.5f);
            rule.Begin(1f);
            rule.Line(new Vector2(-w * 0.5f, 0), new Vector2(w * 0.5f, 0));
            for (float x = -w * 0.5f; x <= w * 0.5f; x += 10f)
                rule.Line(new Vector2(x, -4), new Vector2(x, 4));
            rule.Commit();
        }

        alertTimer -= Time.unscaledDeltaTime;
        if (alertTimer <= 0f) { alertTimer = 0.25f; RefreshAlerts(); }
        ApplyAlerts();
    }
}