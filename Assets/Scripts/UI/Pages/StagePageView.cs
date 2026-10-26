using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EnemyV2;

// หน้า STG สร้างด้วยโค้ดทั้งหมด วางไว้บน Page_Stage (RectTransform ยืดเต็มพื้นที่)
// ข้อมูลหาเองตอนรัน ไม่มี reference ฉากให้หลุด
public class StagePageView : MonoBehaviour
{
    [Header("Fonts (ว่างได้ ใช้ฟอนต์เริ่มต้น)")]
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;

    const int Segs = 20;
    const int MaxRows = 6;
    const float LeftW = 440f, Gap = 14f;

    UiKit kit;
    bool built;

    // นาฬิกา
    TextMeshProUGUI clock, phase, limitText, elapsedText;
    RectTransform progressFill;
    Image progressImg;
    int lastSec = -1;

    // ประตู
    TextMeshProUGUI gatesCount;
    Image[] gateBoxes = new Image[8];
    RectTransform gateBoxRow;

    // สถิติ
    TextMeshProUGUI killsText, distText;

    // รายการประตู
    class Row
    {
        public RectTransform root; public TextMeshProUGUI name, chip, dist;
        public CutCornerFrame chipFrame; public int lastRole = -1; public Image[] segs = new Image[Segs];
    }
    readonly List<Row> rows = new List<Row>();
    RectTransform rowsRoot;

    // เตือนนอกพื้นที่
    int lastWarn = -1;
    CutCornerFrame warnFrame; TextMeshProUGUI warnTitle, warnTime;

    void OnEnable()
    {
        if (!built) Build();
        UiThemeSource.Changed += OnTheme;
    }
    void OnDisable() => UiThemeSource.Changed -= OnTheme;
    void OnTheme() { if (built) kit.Refresh(); }

    // ================= สร้าง =================
    void Build()
    {
        built = true;
        kit = new UiKit { headFont = headFont, monoFont = monoFont };
        var root = (RectTransform)transform;

        // ----- คอลัมน์ซ้าย -----
        var left = UiKit.Rect("Left", root);
        left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0, 1);
        left.pivot = new Vector2(0, 1); left.offsetMin = Vector2.zero; left.offsetMax = new Vector2(LeftW, 0);

        BuildClockCard(left, 0, 214);
        BuildGatesCard(left, 214 + Gap, 120);
        BuildStatCards(left, 214 + Gap + 120 + Gap, 100);

        // ----- คอลัมน์ขวา -----
        var right = UiKit.Rect("Right", root);
        right.anchorMin = Vector2.zero; right.anchorMax = Vector2.one;
        right.offsetMin = new Vector2(LeftW + Gap, 0); right.offsetMax = Vector2.zero;

        BuildGateList(right);
        BuildWarning(right);
    }

    CutCornerFrame Card(RectTransform parent, string name, float y, float h, float x = 0, float w = -1)
    {
        var f = kit.Frame(parent, name, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        if (w < 0) // เต็มความกว้างของ parent
        {
            var rt = f.rectTransform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(0, -(y + h)); rt.offsetMax = new Vector2(0, -y);
        }
        else UiKit.TopLeft(f.rectTransform, x, y, w, h);
        return f;
    }

    void Heading(Transform p, string text)
    {
        var t = kit.Label(p, "Heading", text, 16, ThemeRole.InkMid, true);
        UiKit.TopLeft(t.rectTransform, 16, 10, 300, 22);
    }

    void BuildClockCard(RectTransform parent, float y, float h)
    {
        var c = Card(parent, "ClockCard", y, h);
        Heading(c.transform, "MISSION CLOCK");

        phase = kit.Label(c.transform, "Phase", "", 14, ThemeRole.InkDim, true, TextAlignmentOptions.TopRight);
        UiKit.TopLeft(phase.rectTransform, LeftW - 16 - 240, 12, 240, 20);

        clock = kit.Label(c.transform, "Clock", "10:00", 92, ThemeRole.InkBright, false, TextAlignmentOptions.Center);
        UiKit.TopLeft(clock.rectTransform, 0, 36, LeftW, 100);

        // แถบเวลา
        var bar = kit.Box(c.transform, "BarBg", ThemeRole.InkLow, 0.35f);
        UiKit.TopLeft(bar.rectTransform, 16, 146, LeftW - 32, 10);
        var fill = kit.Box(bar.transform, "Fill", ThemeRole.InkBright, 1f);
        progressImg = fill; progressFill = fill.rectTransform;
        progressFill.anchorMin = Vector2.zero; progressFill.anchorMax = new Vector2(1, 1);
        progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;

        elapsedText = kit.Label(c.transform, "Elapsed", "00:00", 14, ThemeRole.InkDim);
        UiKit.TopLeft(elapsedText.rectTransform, 16, 166, 140, 20);
        limitText = kit.Label(c.transform, "Limit", "LIMIT 10:00", 14, ThemeRole.InkDim, false, TextAlignmentOptions.TopRight);
        UiKit.TopLeft(limitText.rectTransform, LeftW - 16 - 160, 166, 160, 20);
    }

    void BuildGatesCard(RectTransform parent, float y, float h)
    {
        var c = Card(parent, "GatesCard", y, h);
        Heading(c.transform, "GATES SEALED");
        gatesCount = kit.Label(c.transform, "Count", "0/3", 56, ThemeRole.InkBright, false, TextAlignmentOptions.TopLeft);
        UiKit.TopLeft(gatesCount.rectTransform, 16, 40, 200, 68);

        gateBoxRow = UiKit.Rect("Boxes", c.transform);
        UiKit.TopLeft(gateBoxRow, 230, 52, LeftW - 230 - 16, 44);
    }

    void EnsureGateBoxes(int total)
    {
        total = Mathf.Min(total, gateBoxes.Length);
        const float size = 44f, step = 54f;
        for (int i = 0; i < total; i++)
        {
            if (gateBoxes[i] != null) continue;
            var b = kit.Box(gateBoxRow, "Box" + i, ThemeRole.InkLow, 0.5f);
            UiKit.TopLeft(b.rectTransform, i * step, 0, size, size);
            gateBoxes[i] = b;
        }
    }

    void BuildStatCards(RectTransform parent, float y, float h)
    {
        float w = (LeftW - Gap) * 0.5f;
        var k = Card(parent, "KillsCard", y, h, 0, w);
        Heading(k.transform, "KILLS");
        killsText = kit.Label(k.transform, "Value", "0", 48, ThemeRole.InkBright);
        UiKit.TopLeft(killsText.rectTransform, 16, 38, w - 32, 56);

        var d = Card(parent, "DistCard", y, h, w + Gap, w);
        Heading(d.transform, "FROM DROP");
        distText = kit.Label(d.transform, "Value", "0 m", 48, ThemeRole.InkBright);
        UiKit.TopLeft(distText.rectTransform, 16, 38, w - 32, 56);
    }

    void BuildGateList(RectTransform right)
    {
        var c = Card((RectTransform)right, "ListCard", 0, 0);
        var crt = c.rectTransform;   // เต็มความสูง เว้นล่างให้กล่องเตือน
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.pivot = new Vector2(0.5f, 0.5f);
        crt.offsetMin = new Vector2(0, 104); crt.offsetMax = Vector2.zero;
        Heading(c.transform, "GATE STATUS");

        rowsRoot = UiKit.Rect("Rows", c.transform);
        UiKit.Stretch(rowsRoot, 16, 12, 16, 44);
    }

    Row MakeRow(int i)
    {
        var r = new Row();
        r.root = UiKit.Rect("Row" + i, rowsRoot);
        r.root.anchorMin = new Vector2(0, 1); r.root.anchorMax = new Vector2(1, 1); r.root.pivot = new Vector2(0.5f, 1);
        r.root.offsetMin = new Vector2(0, -(i * 62 + 56)); r.root.offsetMax = new Vector2(0, -(i * 62));

        var line = kit.Box(r.root, "Line", ThemeRole.InkLow, 0.4f);
        line.rectTransform.anchorMin = Vector2.zero; line.rectTransform.anchorMax = new Vector2(1, 0);
        line.rectTransform.offsetMin = Vector2.zero; line.rectTransform.offsetMax = new Vector2(0, 1);

        r.name = kit.Label(r.root, "Name", "GATE", 20, ThemeRole.InkBright, true);
        UiKit.TopLeft(r.name.rectTransform, 0, 4, 200, 26);

        r.chipFrame = kit.Frame(r.root, "Chip", ThemeRole.InkMid, 1f, ThemeRole.Surface, 0f);
        UiKit.TopLeft(r.chipFrame.rectTransform, 0, 32, 110, 20);
        r.chip = kit.Label(r.chipFrame.transform, "Text", "ACTIVE", 12, ThemeRole.InkMid, true, TextAlignmentOptions.Center);
        UiKit.Stretch(r.chip.rectTransform);

        r.dist = kit.Label(r.root, "Dist", "0 m", 22, ThemeRole.InkMid, false, TextAlignmentOptions.TopRight);
        r.dist.rectTransform.anchorMin = r.dist.rectTransform.anchorMax = r.dist.rectTransform.pivot = new Vector2(1, 1);
        r.dist.rectTransform.sizeDelta = new Vector2(120, 26); r.dist.rectTransform.anchoredPosition = new Vector2(0, -4);

        // แถบพลังงานแบบเป็นช่อง
        var segRoot = UiKit.Rect("Energy", r.root);
        segRoot.anchorMin = new Vector2(0, 1); segRoot.anchorMax = new Vector2(1, 1); segRoot.pivot = new Vector2(0.5f, 1);
        segRoot.offsetMin = new Vector2(130, -50); segRoot.offsetMax = new Vector2(0, -34);
        for (int s = 0; s < Segs; s++)
        {
            var img = kit.Box(segRoot, "S" + s, ThemeRole.InkLow, 0.3f);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(s / (float)Segs, 0); rt.anchorMax = new Vector2((s + 1) / (float)Segs, 1);
            rt.offsetMin = new Vector2(1.5f, 0); rt.offsetMax = new Vector2(-1.5f, 0);
            r.segs[s] = img;
        }
        return r;
    }

    void BuildWarning(RectTransform right)
    {
        warnFrame = kit.Frame(right, "Warning", ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.2f);
        var rt = warnFrame.rectTransform;
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0, 90);

        warnTitle = kit.Label(rt, "Title", "ZONE: NOMINAL", 22, ThemeRole.InkDim, true, TextAlignmentOptions.MidlineLeft);
        UiKit.TopLeft(warnTitle.rectTransform, 20, 0, 640, 90);
        warnTime = kit.Label(rt, "Time", "", 48, ThemeRole.Danger, false, TextAlignmentOptions.MidlineRight);
        warnTime.rectTransform.anchorMin = new Vector2(1, 0); warnTime.rectTransform.anchorMax = new Vector2(1, 1);
        warnTime.rectTransform.pivot = new Vector2(1, 0.5f);
        warnTime.rectTransform.sizeDelta = new Vector2(220, 0); warnTime.rectTransform.anchoredPosition = new Vector2(-20, 0);
    }

    // ================= อัปเดต =================
    void Update()
    {
        if (!built) return;
        var run = RunManager.Instance;
        var mm = MissionManager.Instance;
        EnemyTarget.Ensure();
        Transform pl = EnemyTarget.Transform;

        UpdateClock(run, mm);
        UpdateStats(run, mm, pl);
        UpdateGates(mm, pl);
        UpdateWarning(mm);
    }

    static string Mmss(float s)
    {
        int t = Mathf.Max(0, Mathf.CeilToInt(s));
        return (t / 60).ToString("00") + ":" + (t % 60).ToString("00");
    }

    void UpdateClock(RunManager run, MissionManager mm)
    {
        if (run == null) { clock.text = "--:--"; return; }

        float limit = run.Elapsed + run.Remaining;
        bool countdown = run.MissionMode;
        float shown = countdown ? run.Remaining : run.Elapsed;
        int sec = Mathf.CeilToInt(shown);
        if (sec != lastSec)
        {
            lastSec = sec;
            clock.text = Mmss(shown);
            elapsedText.text = Mmss(run.Elapsed);
            limitText.text = countdown ? "LIMIT " + Mmss(limit) : "NO LIMIT";
        }

        float remainRatio = countdown && limit > 0f ? run.Remaining / limit : 1f;
        Color c = UiThemeSource.LevelColor(remainRatio);
        clock.color = c;
        progressImg.color = c;
        progressFill.anchorMax = new Vector2(countdown ? Mathf.Clamp01(1f - remainRatio) : 0f, 1);

        phase.text = mm == null ? "" : mm.Phase switch
        {
            MissionPhase.Mission => "PHASE: SEAL GATES",
            MissionPhase.Extract => "PHASE: EXTRACT",
            MissionPhase.Complete => "MISSION COMPLETE",
            _ => "MISSION FAILED"
        };
    }

    void UpdateStats(RunManager run, MissionManager mm, Transform pl)
    {
        if (run != null) killsText.text = run.Kills.ToString();
        if (mm != null)
        {
            int total = mm.Total, closed = mm.ClosedCount;
            gatesCount.text = closed + "/" + total;
            EnsureGateBoxes(total);
            for (int i = 0; i < gateBoxes.Length; i++)
            {
                if (gateBoxes[i] == null) continue;
                bool done = i < closed;
                var col = UiThemeSource.Role(done ? ThemeRole.InkBright : ThemeRole.InkLow);
                col.a = done ? 1f : 0.5f;
                gateBoxes[i].color = col;
            }
            if (pl != null) distText.text = Mathf.RoundToInt(Vector2.Distance(pl.position, mm.Origin)) + " m";
        }
    }

    Gate FindLive(Vector2 pos)
    {
        Gate best = null; float bd = 2f;
        for (int i = 0; i < Gate.All.Count; i++)
        {
            var g = Gate.All[i];
            if (g == null) continue;
            float d = Vector2.Distance(g.transform.position, pos);
            if (d < bd) { bd = d; best = g; }
        }
        return best;
    }

    void UpdateGates(MissionManager mm, Transform pl)
    {
        int n = mm != null ? Mathf.Min(mm.Total, MaxRows) : 0;
        while (rows.Count < n) rows.Add(MakeRow(rows.Count));

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            r.root.gameObject.SetActive(i < n);
            if (i >= n) continue;

            var info = mm.GateInfos[i];
            Gate g = info.closed ? null : FindLive(info.position);

            r.name.text = "GATE " + (char)('A' + i);
            if (pl != null) r.dist.text = Mathf.RoundToInt(Vector2.Distance(pl.position, info.position)) + " m";

            ThemeRole role; string label; float ratio; Color litColor;
            if (info.closed)
            { role = ThemeRole.InkLow; label = "SEALED"; ratio = 0f; litColor = UiThemeSource.Role(ThemeRole.InkLow); }
            else if (g != null && g.State == GateState.Charging)
            { role = ThemeRole.Warn; label = "CHARGING"; ratio = g.ChargeRatio; litColor = UiThemeSource.Role(ThemeRole.Warn); }
            else
            { role = ThemeRole.InkBright; label = "ACTIVE"; ratio = g != null ? g.Ratio : 1f; litColor = UiThemeSource.Role(ThemeRole.InkBright); }

            var rc = UiThemeSource.Role(role);
            r.chip.text = label; r.chip.color = rc;
            if (r.lastRole != (int)role) { r.lastRole = (int)role; r.chipFrame.SetStyle(role, 1f, ThemeRole.Surface, 0f); }
            r.name.color = UiThemeSource.Role(info.closed ? ThemeRole.InkDim : ThemeRole.InkBright);
            r.dist.color = UiThemeSource.Role(info.closed ? ThemeRole.InkLow : ThemeRole.InkMid);

            int lit = Mathf.CeilToInt(Mathf.Clamp01(ratio) * Segs);
            var dim = UiThemeSource.Role(ThemeRole.InkLow); dim.a = 0.3f;
            for (int s = 0; s < Segs; s++) r.segs[s].color = s < lit ? litColor : dim;
        }
    }

    void UpdateWarning(MissionManager mm)
    {
        bool out_ = mm != null && mm.OutOfArea;
        if (lastWarn != (out_ ? 1 : 0))
        {
            lastWarn = out_ ? 1 : 0;
            if (out_) warnFrame.SetStyle(ThemeRole.Danger, 1f, ThemeRole.Danger, 0.15f);
            else warnFrame.SetStyle(ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.2f);
        }
        if (out_)
        {
            warnTitle.text = "WARNING // OUT OF ZONE  -  RETURN TO AREA";
            warnTitle.color = UiThemeSource.Role(ThemeRole.Danger);
            warnTime.text = Mmss(mm.AbandonRemaining);
        }
        else
        {
            warnTitle.text = "ZONE: NOMINAL";
            warnTitle.color = UiThemeSource.Role(ThemeRole.InkDim);
            warnTime.text = "";
        }
    }
}