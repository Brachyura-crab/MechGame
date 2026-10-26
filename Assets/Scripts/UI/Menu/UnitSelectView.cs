using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// หน้าเลือกหุ่น + อัปเกรดโมดูล สร้างด้วยโค้ด (ขนาดอ้างอิง 1280x720 ตาม mockup)
// วางบน GameObject ว่างใน scene "UnitSelect" ที่มี UiThemeSource อยู่ด้วย
public class UnitSelectView : MonoBehaviour
{
    [Header("Scenes (ต้องอยู่ใน Build Settings)")]
    [SerializeField] string backScene = "MainMenu";
    [SerializeField] string nextScene = "MissionSelect";

    [Header("Fonts (ว่างได้)")]
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;

    const float W = 1280f, H = 720f;
    const float ListX = 28f, ListW = 230f, DataX = 274f, DataW = 532f, UpX = 822f, UpW = 430f;
    const float BodyY = 68f, BodyH = 572f;

    UiKit kit;
    RectTransform stage, dyn;
    GameConfig cfg;
    readonly List<MechEntry> list = new List<MechEntry>();
    int sel;
    string message;
    ScrollRect listSR;
    float listScrollPos = 1f;
    ScrollRect upgSR;
    float upgScrollPos = 1f;

    MechEntry Cur => sel >= 0 && sel < list.Count ? list[sel] : null;

    // ---------- เริ่ม ----------
    void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();

        kit = new UiKit { headFont = headFont, monoFont = monoFont };

        cfg = GameConfig.Instance;
        if (cfg != null && cfg.mechs != null && cfg.mechs.mechs != null)
            foreach (var m in cfg.mechs.mechs) if (m != null) list.Add(m);

        // เริ่มที่ตัวที่เลือกไว้ล่าสุด ไม่งั้นตัวแรกที่ปลดล็อก
        sel = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (GameSession.SelectedMech != null && list[i].id == GameSession.SelectedMech.id) { sel = i; break; }
            if (sel == 0 && !SaveService.IsUnlocked(list[0]) && SaveService.IsUnlocked(list[i])) sel = i;
        }

        BuildCanvas();
        BuildStatic();
        Rebuild();
    }

    void OnEnable() => UiThemeSource.Changed += OnTheme;
    void OnDisable() => UiThemeSource.Changed -= OnTheme;
    void OnTheme() { if (kit != null) kit.Refresh(); }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    void BuildCanvas()
    {
        var go = new GameObject("UnitSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(W, H);
        sc.matchWidthOrHeight = 0.5f;

        var root = (RectTransform)go.transform;

        // พื้นหลังเต็มจอ (ภายหลังเปลี่ยนเป็นภาพวาดได้)
        var bg = kit.Box(root, "Background", ThemeRole.Background, 1f);
        UiKit.Stretch(bg.rectTransform);

        // เวทีขนาดคงที่ 1280x720 กึ่งกลางจอ
        stage = UiKit.Rect("Stage", root);
        stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(0.5f, 0.5f);
        stage.sizeDelta = new Vector2(W, H);
        stage.anchoredPosition = Vector2.zero;
        
        // เอฟเฟกต์จอ CRT ทับทั้งหน้า (ต้องเป็นลูกลำดับท้ายสุดของ canvas จึงอยู่บนสุดเสมอ)
        var stageGroup = stage.gameObject.AddComponent<CanvasGroup>();
        var fx = UiKit.Rect("CrtFx", root);
        UiKit.Stretch(fx);
        var crt = fx.gameObject.AddComponent<CrtScreenFx>();
        crt.SetFlickerTarget(stageGroup);
    }

    // ---------- helper ----------
    TextMeshProUGUI L(Transform p, string text, float x, float y, float w, float h, float size, ThemeRole role,
                      bool head = false, TextAlignmentOptions a = TextAlignmentOptions.MidlineLeft)
    {
        var t = kit.Label(p, "T", text, size, role, head, a);
        UiKit.TopLeft(t.rectTransform, x, y, w, h);
        return t;
    }

    Image B(Transform p, float x, float y, float w, float h, ThemeRole r, float a)
    {
        var i = kit.Box(p, "B", r, a);
        UiKit.TopLeft(i.rectTransform, x, y, w, h);
        return i;
    }

    // สามเหลี่ยมแทน ▶ ◀
    TriangleGraphic Tri(Transform p, float x, float y, float w, float h, ThemeRole r, float a, bool left)
    {
        var rt = UiKit.Rect("Tri", p);
        UiKit.TopLeft(rt, x, y, w, h);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var t = rt.gameObject.AddComponent<TriangleGraphic>();
        t.pointLeft = left; t.raycastTarget = false;
        kit.Bind(t, r, a);
        return t;
    }

    CutCornerFrame Card(Transform p, float x, float y, float w, float h,
                        ThemeRole outline, float oa, ThemeRole fill, float fa)
    {
        var f = kit.Frame(p, "Card", outline, oa, fill, fa);
        UiKit.TopLeft(f.rectTransform, x, y, w, h);
        return f;
    }

    // ปุ่มโปร่งใสทับพื้นที่ parent
    static void Hit(RectTransform parent, UnityAction act)
    {
        var rt = UiKit.Rect("Hit", parent);
        UiKit.Stretch(rt);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.color = Color.clear; img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None; b.targetGraphic = img;
        b.onClick.AddListener(GameAudio.UiClick);
        b.onClick.AddListener(act);
    }

    static string Fmt(float v) => v.ToString("0.##");

    // ---------- ส่วนที่ไม่เปลี่ยน (header) ----------
    void BuildStatic()
    {
        var hd = UiKit.Rect("Header", stage);
        UiKit.TopLeft(hd, 28, 20, 1224, 36);
        B(hd, 0, 13, 10, 10, ThemeRole.InkBright, 1f);
        L(hd, "MECH-OS", 26, 0, 160, 36, 22, ThemeRole.InkBright, true);
        L(hd, "// UNIT SELECT", 150, 0, 220, 36, 13, ThemeRole.InkLow);
        B(hd, 340, 17, 520, 1, ThemeRole.InkLow, 0.6f);
        L(hd, "STEP 1 / 2    NEXT: SELECT MISSION", 824, 0, 400, 36, 14, ThemeRole.InkMid, false,
          TextAlignmentOptions.MidlineRight);
    }

    // ---------- ส่วนที่เปลี่ยนตามการเลือก ----------
    void Rebuild()
    {
        if (listSR != null) listScrollPos = listSR.verticalNormalizedPosition;
        if (upgSR != null) upgScrollPos = upgSR.verticalNormalizedPosition;
        if (dyn != null) { dyn.gameObject.SetActive(false); Destroy(dyn.gameObject); }
        dyn = UiKit.Rect("Dyn", stage);
        UiKit.Stretch(dyn);

        if (list.Count == 0)
        {
            L(dyn, "NO UNITS: ตั้ง GameConfig + MechCatalog ก่อน", 28, 68, 800, 40, 18, ThemeRole.Warn);
            BuildFooter();
            return;
        }
        BuildList();
        BuildData();
        BuildUpgrades();
        BuildFooter();
    }

        void BuildList()
    {
        int n = Mathf.Max(1, list.Count);
        float gap = 12f;
        float h = Mathf.Clamp((BodyH - gap * (n - 1)) / n, 96f, 130f);
        float total = n * h + (n - 1) * gap;
        var col = Scroll(dyn, ListX, BodyY, ListW, BodyH, total, out listSR);
        if (total > BodyH) listSR.verticalNormalizedPosition = listScrollPos;

        for (int i = 0; i < list.Count; i++)
        {
            int k = i;
            var e = list[i];
            bool on = i == sel;
            bool open = SaveService.IsUnlocked(e);

            var card = Card(col, 0, i * (h + gap), ListW, h,
                            on ? ThemeRole.InkBright : ThemeRole.InkLow, on ? 1f : 0.7f,
                            ThemeRole.Surface, on ? 0.9f : 0.35f);
            var c = card.rectTransform;

            string code = e.data != null ? e.data.code : e.id;
            string shortName = open && e.data != null ? e.data.shortName : "- - -";

            L(c, code, 14, h / 2 - 36, 190, 16, 11, ThemeRole.InkLow);
            L(c, shortName, 14, h / 2 - 18, 190, 30, 22, on ? ThemeRole.InkBright : ThemeRole.InkMid, true);
            L(c, !open ? "LOCKED" : on ? "SELECTED" : "READY", 14, h / 2 + 14, 190, 16, 11,
              open ? ThemeRole.InkMid : ThemeRole.Warn);
            B(c, ListW - 24, h / 2 - 4, 8, 8, on ? ThemeRole.InkBright : ThemeRole.InkLow, on ? 1f : 0.5f);

            Hit(c, () => { sel = k; message = null; Rebuild(); });
        }
    }

    void BuildData()
    {
        var e = Cur;
        bool open = SaveService.IsUnlocked(e);

        var pan = Card(dyn, DataX, BodyY, DataW, BodyH, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        var p = pan.rectTransform;

        // portrait
        var pf = Card(p, 20, 16, 96, 96, ThemeRole.InkLow, 0.9f, ThemeRole.Background, 0.6f);
        if (open && e.data != null && e.data.portrait != null)
        {
            var rt = UiKit.Rect("Portrait", pf.rectTransform);
            UiKit.Stretch(rt, 6, 6, 6, 6);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = e.data.portrait; img.preserveAspect = true; img.raycastTarget = false;
        }
        else L(pf.rectTransform, "[ PORTRAIT ]", 0, 0, 96, 96, 11, ThemeRole.InkLow, false, TextAlignmentOptions.Center);

        string code = e.data != null ? e.data.code : e.id;
        string name = open && e.data != null ? e.data.displayName : "- - - - - -";
        L(p, "UNIT DESIGNATION   " + code, 132, 16, 380, 18, 12, ThemeRole.InkLow);
        L(p, name, 132, 34, 380, 40, 30, ThemeRole.InkBright, true);
        string desc = open ? e.description : (string.IsNullOrEmpty(e.unlockedByMissionId) ? "LOCKED" : "COMPLETE A MISSION TO UNLOCK");
        var dt = L(p, desc, 132, 76, 380, 40, 12, ThemeRole.InkMid);
        dt.textWrappingMode = TextWrappingModes.Normal;
        dt.alignment = TextAlignmentOptions.TopLeft;

        // performance
        B(p, 20, 128, DataW - 40, 1, ThemeRole.InkLow, 0.6f);
        B(p, 20, 138, 3, 12, ThemeRole.InkLow, 1f);
        L(p, "PERFORMANCE", 30, 134, 300, 20, 12, ThemeRole.InkLow, true);

        string[] bl = { "ARMOR", "MOBILITY", "POWER" };
        float[] bv = open ? new[] { e.barArmor, e.barSpeed, e.barPower } : new[] { 0f, 0f, 0f };
        for (int i = 0; i < 3; i++)
        {
            float y = 164 + i * 34;
            L(p, bl[i], 20, y, 100, 24, 14, ThemeRole.InkMid, true);
            int on = Mathf.RoundToInt(Mathf.Clamp01(bv[i]) * 10f);
            float x0 = 130f, sw = (DataW - 20 - 130 - 50) / 10f;
            for (int s = 0; s < 10; s++)
                B(p, x0 + s * sw, y + 5, sw - 3, 14, s < on ? ThemeRole.InkBright : ThemeRole.InkMid, s < on ? 1f : 0.15f);
            L(p, open ? Mathf.RoundToInt(bv[i] * 100f).ToString() : "--", DataW - 70, y, 50, 24, 14,
              ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
        }

        // loadout
        B(p, 20, 274, DataW - 40, 1, ThemeRole.InkLow, 0.6f);
        B(p, 20, 284, 3, 12, ThemeRole.InkLow, 1f);
        L(p, "LOADOUT", 30, 280, 300, 20, 12, ThemeRole.InkLow, true);
        L(p, "FIXED", DataW - 120, 280, 100, 20, 12, ThemeRole.InkLow, false, TextAlignmentOptions.MidlineRight);

        int rows = open && e.loadoutNames != null ? Mathf.Min(7, e.loadoutNames.Length) : 0;
        for (int i = 0; i < rows; i++)
        {
            float y = 308 + i * 38;
            B(p, 20, y, DataW - 40, 32, ThemeRole.InkMid, 0.07f);
            B(p, 20, y, 3, 32, ThemeRole.InkBright, 1f);
            L(p, (i + 1).ToString("00"), 34, y, 40, 32, 12, ThemeRole.InkLow);
            L(p, e.loadoutNames[i], 76, y, DataW - 120, 32, 15, ThemeRole.InkBright, true);
        }
    }

    void BuildUpgrades()
    {
        var e = Cur;
        bool open = SaveService.IsUnlocked(e);
        var table = cfg != null ? cfg.upgrades : null;

        var pan = Card(dyn, UpX, BodyY, UpW, BodyH, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        var p = pan.rectTransform;

        B(p, 18, 20, 3, 12, ThemeRole.InkLow, 1f);
        L(p, "MODULE UPGRADE", 28, 14, 220, 24, 12, ThemeRole.InkLow, true);
        L(p, "POINTS", UpW - 150, 12, 70, 28, 14, ThemeRole.InkMid, false, TextAlignmentOptions.MidlineRight);
        L(p, SaveService.Data.points.ToString(), UpW - 76, 8, 60, 34, 22, ThemeRole.InkBright, true,
          TextAlignmentOptions.MidlineRight);

        if (table == null || table.defs == null || table.defs.Length == 0)
        {
            L(p, "NO UPGRADE TABLE", 18, 60, 300, 24, 14, ThemeRole.Warn);
            return;
        }

        int n = table.defs.Length;
        float top = 52f, gap = 10f;
        float rh = Mathf.Clamp((BodyH - top - 16f - gap * (n - 1)) / n, 84f, 100f);
        float rw = UpW - 36f;
        float vh = BodyH - top - 16f;
        float total = n * rh + (n - 1) * gap;
        var content = Scroll(p, 18, top, rw, vh, total, out upgSR);
        if (total > vh) upgSR.verticalNormalizedPosition = upgScrollPos;

        for (int i = 0; i < n; i++)
        {
            var d = table.defs[i];
            float y = i * (rh + gap);
            int lv = open ? SaveService.Level(e.id, d.kind) : 0;
            bool maxed = lv >= table.maxLevel;
            int cost = table.CostOf(d, lv);
            bool can = open && !maxed && SaveService.Data.points >= cost;

            var row = UiKit.Rect("Row" + i, content);
            UiKit.TopLeft(row, 0, y, rw, rh);
            B(row, 0, 0, rw, rh, ThemeRole.InkMid, 0.07f);
            B(row, 0, 0, 3, rh, maxed ? ThemeRole.Warn : ThemeRole.InkBright, 1f);

            L(row, d.label, 14, rh / 2 - 26, 190, 22, 16, ThemeRole.InkBright, true);

            // pips
            float px = rw - 12 - 96 - 14 - table.maxLevel * 13f;
            for (int k = 0; k < table.maxLevel; k++)
                B(row, px + k * 13f, rh / 2 - 20, 10, 10, ThemeRole.InkBright, k < lv ? 1f : 0.15f);

            // stat: cur -> next
            float baseV = BaseStat(e, UpgradeApplier.ToStat(d.kind));
            float curV = baseV * (1f + table.BonusPercent(d, lv) / 100f);
            float nextV = baseV * (1f + table.BonusPercent(d, lv + 1) / 100f);
            L(row, d.statLabel, 14, rh / 2 + 2, 120, 22, 12, ThemeRole.InkLow);
            L(row, open ? Fmt(curV) : "--", 136, rh / 2 + 2, 60, 22, 13, ThemeRole.InkMid);
            Tri(row, 190, rh / 2 + 8, 8, 10, ThemeRole.InkLow, 1f, false);
            L(row, !open ? "--" : maxed ? "MAX" : Fmt(nextV), 208, rh / 2 + 2, 70, 22, 13,
              maxed ? ThemeRole.Warn : ThemeRole.InkBright);

            // ปุ่มอัปเกรด
            var btn = Card(row, rw - 12 - 96, rh / 2 - 19, 96, 38,
                           can ? ThemeRole.InkBright : ThemeRole.InkLow, 1f,
                           ThemeRole.Surface, can ? 0.9f : 0.3f);
            var bt = btn.rectTransform;
            L(bt, maxed ? "MAX" : "UPGRADE", 0, 3, 96, 20, 13, can ? ThemeRole.InkBright : ThemeRole.InkLow, true,
              TextAlignmentOptions.Center);
            L(bt, maxed || !open ? "--" : cost + " PT", 0, 20, 96, 16, 11, can ? ThemeRole.InkMid : ThemeRole.InkLow, false,
              TextAlignmentOptions.Center);

            if (can)
            {
                var def = d;
                Hit(bt, () => { if (SaveService.TryBuy(e.id, def, table)) Rebuild(); });
            }
        }
    }

    static float BaseStat(MechEntry e, StatId id)
    {
        if (e == null || e.data == null) return 0f;
        var sb = new StatBlock();
        e.data.ApplyTo(sb);
        return sb.Get(id);
    }

    void BuildFooter()
    {
        var e = Cur;
        bool open = e != null && SaveService.IsUnlocked(e);

        var back = Card(dyn, 28, 654, 170, 44, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        Tri(back.rectTransform, 46, 16, 9, 12, ThemeRole.InkMid, 1f, true);
        L(back.rectTransform, "BACK", 12, 0, 170, 44, 16, ThemeRole.InkMid, true, TextAlignmentOptions.Center);
        Hit(back.rectTransform, () => LoadScene(backScene));

        L(dyn, "CLICK A UNIT TO PREVIEW  /  UPGRADES ARE KEPT PER UNIT", 214, 654, 560, 44, 12, ThemeRole.InkLow);

        bool warn = e != null && !open;
        L(dyn, message ?? (warn ? "UNIT LOCKED" : "READY TO DEPLOY"), 760, 654, 218, 44, 12,
          warn ? ThemeRole.Warn : ThemeRole.InkLow, false, TextAlignmentOptions.MidlineRight);

        var go = Card(dyn, 992, 652, 260, 48,
                      open ? ThemeRole.InkBright : ThemeRole.InkLow, 1f,
                      ThemeRole.Surface, open ? 0.9f : 0.3f);
        L(go.rectTransform, "CONFIRM UNIT", -12, 0, 260, 48, 18,
          open ? ThemeRole.InkBright : ThemeRole.InkLow, true, TextAlignmentOptions.Center);
        Tri(go.rectTransform, 188, 18, 10, 12, open ? ThemeRole.InkBright : ThemeRole.InkLow, 1f, false);
        Hit(go.rectTransform, Confirm);
    }

    // ---------- scroll ----------
    // viewport + content (content ยึดมุมซ้ายบน ลูกใช้ UiKit.TopLeft ได้ตามปกติ)
    static RectTransform Scroll(Transform p, float x, float y, float w, float h, float contentH, out ScrollRect sr)
    {
        var vp = UiKit.Rect("Viewport", p);
        UiKit.TopLeft(vp, x, y, w, h);
        vp.gameObject.AddComponent<RectMask2D>();
        vp.gameObject.AddComponent<CanvasRenderer>();
        var img = vp.gameObject.AddComponent<Image>();
        img.color = Color.clear; img.raycastTarget = true;   // รับล้อเมาส์/การลากบนพื้นว่าง

        var content = UiKit.Rect("Content", vp);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = new Vector2(0, Mathf.Max(contentH, h));
        content.anchoredPosition = Vector2.zero;

        sr = vp.gameObject.AddComponent<ScrollRect>();
        sr.viewport = vp; sr.content = content;
        sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;
        return content;
    }

    // ---------- การกระทำ ----------
    void Confirm()
    {
        var e = Cur;
        if (e == null) return;
        if (!SaveService.IsUnlocked(e)) { message = "UNIT LOCKED"; Rebuild(); return; }
        GameSession.SelectedMech = e;
        LoadScene(nextScene);
    }

    static void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("UnitSelectView: ไม่พบ scene '" + sceneName + "' (ยังไม่ได้สร้าง หรือยังไม่ได้ใส่ Build Settings)");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}