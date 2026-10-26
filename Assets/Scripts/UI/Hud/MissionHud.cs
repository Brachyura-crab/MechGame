using System.Collections.Generic;
using EnemyV2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD ภารกิจ (สร้างด้วยโค้ด): เวลา (กลางบน) + ความคืบหน้าเกต (ใต้มินิแมป) + ลูกศรชี้ทุกเกต + แต้ม/เสบียง (ขวาบน)
// วางบน GameObject ว่างใต้ Canvas ของ HUD (โค้ดยืดให้เต็ม canvas เอง)
public class MissionHud : MonoBehaviour
{
    [Header("Fonts (ว่างได้)")]
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;

    [Header("ขนาด / ตำแหน่ง")]
    [SerializeField] float uiScale = 1.3f;                                   // ปรับตามความละเอียด canvas ของ HUD
    [SerializeField] float topMargin = 20f;                                  // เวลา: ระยะจากขอบบน
    [SerializeField] Vector2 supplyMargin = new Vector2(24f, 20f);           // กล่องแต้ม: ระยะจากมุมขวาบน
    [SerializeField] float gateBarGap = 8f;                                  // แถบเกต: ระยะใต้มินิแมป
    [SerializeField] Vector2 gateBarFallback = new Vector2(24f, -244f);      // ใช้เมื่อหามินิแมปไม่เจอ
    [SerializeField] float arrowEdgeMargin = 70f;                            // ลูกศรห่างขอบจอ
    [SerializeField, Range(0.5f, 1f)] float onScreenLimit = 0.9f;            // เป้าหมายอยู่ในกรอบนี้ถือว่า "เห็นแล้ว" ซ่อนลูกศร

    [Header("เวลา")]
    [SerializeField] float timeFontSize = 30f;
    [SerializeField] float warnTime = 120f;
    [SerializeField] float dangerTime = 30f;

    const float SupW = 160f, SupH = 62f;

    class ArrowItem
    {
        public RectTransform rt, lrt;
        public TriangleGraphic gfx;
        public TextMeshProUGUI label;
        public int lastKey = -1;
    }

    UiKit kit;
    bool built;

    // เวลา
    RectTransform timeGroup;
    TextMeshProUGUI timeText, warnText;
    int lastSec = -1;

    // แถบเกต (ใต้มินิแมป สร้างตอนแรกที่เจอ MissionManager)
    RectTransform gateBar, gateBoxes, gateBarFill;
    TextMeshProUGUI gateLabel, gateCount;
    float gateBarW, gateAvailW;
    readonly List<Image> gateFills = new List<Image>();
    int builtGates = -1, lastClosed = -1, lastPct = -1;
    bool lastExtract;

    // ลูกศร
    readonly List<ArrowItem> arrows = new List<ArrowItem>();

    // แต้ม/เสบียง
    RectTransform supplyGroup, supplyFill;
    TextMeshProUGUI pointsText, supplyStatus;
    SupplyService supply;
    float deliveryTotal = 1f, deliveredUntil = -1f;
    int lastPoints = int.MinValue;

    RectTransform Root => (RectTransform)transform;
    class Slide { public RectTransform rt; public Vector2 basePos, offset; }
    readonly List<Slide> slides = new List<Slide>();

    // ชิ้นนี้เริ่มนอกจอ (offset) แล้วเลื่อนเข้าตาม BootSequence.HudProgress
    void AddSlide(RectTransform rt, Vector2 offset)
    {
        var s = new Slide { rt = rt, basePos = rt.anchoredPosition, offset = offset };
        slides.Add(s);
        rt.anchoredPosition = s.basePos + s.offset * (1f - BootSequence.HudProgress);
    }

    void ApplySlides()
    {
        float k = 1f - BootSequence.HudProgress;
        for (int i = 0; i < slides.Count; i++)
            if (slides[i].rt != null) slides[i].rt.anchoredPosition = slides[i].basePos + slides[i].offset * k;
    }

    void OnEnable()
    {
        if (!built)
        {
            kit = new UiKit { headFont = headFont, monoFont = monoFont };
            Build();
            built = true;
        }
        UiThemeSource.Changed += OnTheme;
    }

    void OnDisable()
    {
        UiThemeSource.Changed -= OnTheme;
        Unbind();
    }

    void OnTheme() { if (built) kit.Refresh(); }

    // ---------- helper ----------
    TextMeshProUGUI Lb(Transform p, string s, float x, float y, float w, float h, float size, ThemeRole r,
                       bool head = false, TextAlignmentOptions a = TextAlignmentOptions.MidlineLeft)
    {
        var t = kit.Label(p, "T", s, size, r, head, a);
        UiKit.TopLeft(t.rectTransform, x, y, w, h);
        return t;
    }

    Image Bx(Transform p, float x, float y, float w, float h, ThemeRole r, float a)
    {
        var i = kit.Box(p, "B", r, a);
        UiKit.TopLeft(i.rectTransform, x, y, w, h);
        return i;
    }

    static void Tint(Graphic g, ThemeRole r, float a = 1f)
    {
        Color c = UiThemeSource.Role(r); c.a = a; g.color = c;
    }

    // ---------- สร้าง ----------
    void Build()
    {
        Root.anchorMin = Vector2.zero; Root.anchorMax = Vector2.one;
        Root.offsetMin = Root.offsetMax = Vector2.zero;

        BuildTime();
        BuildSupply();
    }

    void BuildTime()
    {
        timeGroup = UiKit.Rect("Time", Root);
        timeGroup.anchorMin = timeGroup.anchorMax = timeGroup.pivot = new Vector2(0.5f, 1f);
        timeGroup.sizeDelta = new Vector2(200f, 44f);
        timeGroup.anchoredPosition = new Vector2(0f, -topMargin);
        timeGroup.localScale = Vector3.one * uiScale;

        timeText = Lb(timeGroup, "--:--", 0, 0, 200, 44, timeFontSize, ThemeRole.InkBright, true,
                      TextAlignmentOptions.Center);

        warnText = Lb(timeGroup, "", -100, 44, 400, 22, 13, ThemeRole.Danger, true, TextAlignmentOptions.Center);
        warnText.gameObject.SetActive(false);
        AddSlide(timeGroup, new Vector2(0f, 44f * uiScale + topMargin + 40f));   // เลื่อนลงมาจากขอบบน
    }

    void BuildSupply()
    {
        supplyGroup = UiKit.Rect("Supply", Root);
        supplyGroup.anchorMin = supplyGroup.anchorMax = supplyGroup.pivot = new Vector2(1f, 1f);
        supplyGroup.sizeDelta = new Vector2(SupW, SupH);
        supplyGroup.anchoredPosition = new Vector2(-supplyMargin.x, -supplyMargin.y);
        supplyGroup.localScale = Vector3.one * uiScale;

        var card = kit.Frame(supplyGroup, "Card", ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.6f);
        UiKit.Stretch(card.rectTransform);

        Lb(supplyGroup, "PTS", 10, 3, 40, 28, 12, ThemeRole.InkMid, true);
        pointsText = Lb(supplyGroup, "0", 46, 1, SupW - 46 - 10, 32, 20, ThemeRole.InkBright, true,
                        TextAlignmentOptions.MidlineRight);
        Bx(supplyGroup, 10, 34, SupW - 20, 1, ThemeRole.InkLow, 0.6f);
        Lb(supplyGroup, "SUPPLY", 10, 38, 60, 16, 10, ThemeRole.InkLow);
        supplyStatus = Lb(supplyGroup, "STANDBY", 62, 38, SupW - 62 - 10, 16, 11, ThemeRole.InkMid, true,
                          TextAlignmentOptions.MidlineRight);
        Bx(supplyGroup, 10, 56, SupW - 20, 3, ThemeRole.InkMid, 0.2f);
        supplyFill = Bx(supplyGroup, 10, 56, 0, 3, ThemeRole.Warn, 1f).rectTransform;

        AddSlide(supplyGroup, new Vector2(SupW * uiScale + supplyMargin.x + 40f, 0f));   // เลื่อนเข้าจากขอบขวา
        supplyGroup.gameObject.SetActive(false);   // เปิดเมื่อหาผู้เล่น/SupplyService เจอ
    }

    // แถบเกตใต้มินิแมป ใช้ตำแหน่ง/ความกว้างของมินิแมป (ต้องสร้างหลัง MinimapView.Awake จึงทำตอน Update แรก)
    void BuildGateBar()
    {
        Vector2 pos = gateBarFallback;
        float w = 200f;
        var mini = FindAnyObjectByType<MinimapView>();
        if (mini != null)
        {
            var mr = (RectTransform)mini.transform;
            w = mr.sizeDelta.x > 1f ? mr.sizeDelta.x : w;
            pos = new Vector2(mr.anchoredPosition.x, mr.anchoredPosition.y - mr.sizeDelta.y - gateBarGap);
        }
        gateBarW = w;

        gateBar = UiKit.Rect("GateBar", Root);
        gateBar.anchorMin = gateBar.anchorMax = gateBar.pivot = new Vector2(0f, 1f);
        gateBar.sizeDelta = new Vector2(w, 24f);
        gateBar.anchoredPosition = pos;

        Bx(gateBar, 0, 0, w, 24, ThemeRole.Surface, 0.45f);
        Bx(gateBar, 0, 0, 3, 24, ThemeRole.InkBright, 1f);
        gateLabel = Lb(gateBar, "GATES", 10, 0, 64, 24, 12, ThemeRole.InkMid, true);

        gateAvailW = w - 74f - 50f;
        gateBoxes = UiKit.Rect("Boxes", gateBar);
        UiKit.TopLeft(gateBoxes, 74, 0, gateAvailW, 24);
        gateBarFill = null;

        gateCount = Lb(gateBar, "0/0", w - 8 - 44, 0, 44, 24, 13, ThemeRole.InkBright, true,
                       TextAlignmentOptions.MidlineRight);

        // แถบยึดจุดรับ (ใช้ตอนเฟส Extract)
        Bx(gateBar, 74, 9, gateAvailW, 6, ThemeRole.InkMid, 0.2f).gameObject.name = "ExtractBg";
        gateBarFill = Bx(gateBar, 74, 9, 0, 6, ThemeRole.InkBright, 1f).rectTransform;
        gateBar.Find("ExtractBg").gameObject.SetActive(false);
        AddSlide(gateBar, new Vector2(-(gateBarW + Mathf.Abs(pos.x) + 40f), 0f));   // เลื่อนเข้าจากขอบซ้าย
        gateBarFill.gameObject.SetActive(false);
    }

    // ---------- วงจร ----------
    void Update()
    {
        if (!built) return;
        BindSupply();
        UpdateTime();
        UpdateGates();
        ApplySlides();
        UpdateSupply();
        UpdateArrows();
    }

    void UpdateTime()
    {
        var rm = RunManager.Instance;
        if (rm == null) { timeText.text = "--:--"; return; }

        bool limited = rm.MissionMode;
        float t = limited ? rm.Remaining : rm.Elapsed;

        int sec = limited ? Mathf.CeilToInt(t) : Mathf.FloorToInt(t);
        if (sec != lastSec)
        {
            lastSec = sec;
            timeText.text = (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00");
        }

        ThemeRole role = ThemeRole.InkBright;
        float a = 1f;
        if (limited)
        {
            if (t <= dangerTime)
            {
                role = ThemeRole.Danger;
                if (rm.State == RunManager.RunState.Playing)
                    a = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f * Mathf.PI));
            }
            else if (t <= warnTime) role = ThemeRole.Warn;
        }
        Tint(timeText, role, a);
    }

    void UpdateGates()
    {
        var mm = MissionManager.Instance;
        var rm = RunManager.Instance;

        // เตือนออกนอกพื้นที่ (ใต้ตัวเลขเวลา)
        bool live = rm == null || rm.State == RunManager.RunState.Playing;
        bool warn = mm != null && live && mm.OutOfArea &&
                    (mm.Phase == MissionPhase.Mission || mm.Phase == MissionPhase.Extract);
        warnText.gameObject.SetActive(warn);
        if (warn)
        {
            warnText.text = "LEAVING MISSION AREA   " + Mathf.CeilToInt(mm.AbandonRemaining) + "s";
            Tint(warnText, ThemeRole.Danger, 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f)));
        }

        if (mm == null) { if (gateBar != null) gateBar.gameObject.SetActive(false); return; }
        if (gateBar == null) BuildGateBar();
        gateBar.gameObject.SetActive(true);

        EnsureGateBoxes(mm.Total);

        bool extract = mm.Phase == MissionPhase.Extract || mm.Phase == MissionPhase.Complete;
        if (extract != lastExtract)
        {
            lastExtract = extract;
            gateBoxes.gameObject.SetActive(!extract);
            gateBar.Find("ExtractBg").gameObject.SetActive(extract);
            gateBarFill.gameObject.SetActive(extract);
            gateLabel.text = extract ? "EXTRACT" : "GATES";
            lastClosed = -1; lastPct = -1;
        }

        if (!extract)
        {
            int closed = mm.ClosedCount;
            if (closed != lastClosed)
            {
                lastClosed = closed;
                for (int i = 0; i < gateFills.Count; i++) gateFills[i].enabled = i < closed;
                gateCount.text = closed + "/" + mm.Total;
            }
        }
        else
        {
            float prog = mm.Phase == MissionPhase.Complete ? 1f : mm.ExtractProgress;
            var sd = gateBarFill.sizeDelta;
            sd.x = gateAvailW * prog;
            gateBarFill.sizeDelta = sd;

            int pct = Mathf.RoundToInt(prog * 100f);
            if (pct != lastPct) { lastPct = pct; gateCount.text = pct + "%"; }
        }
    }

    void EnsureGateBoxes(int n)
    {
        if (n == builtGates) return;
        builtGates = n;
        lastClosed = -1;

        for (int i = gateBoxes.childCount - 1; i >= 0; i--) Destroy(gateBoxes.GetChild(i).gameObject);
        gateFills.Clear();
        if (n <= 0) { gateCount.text = "0/0"; return; }

        const float gap = 5f;
        float w = Mathf.Min(20f, (gateAvailW - gap * (n - 1)) / n);
        for (int i = 0; i < n; i++)
        {
            float x = i * (w + gap);
            Bx(gateBoxes, x, 6, w, 12, ThemeRole.InkMid, 0.18f);
            var f = Bx(gateBoxes, x, 6, w, 12, ThemeRole.InkBright, 1f);
            f.enabled = false;
            gateFills.Add(f);
        }
    }

    // ---------- แต้ม / เสบียง ----------
    void BindSupply()
    {
        if (supply != null) return;
        var tag = PlayerTag.Instance;
        if (tag == null) return;

        supply = tag.GetComponentInParent<SupplyService>();
        if (supply == null) supply = tag.GetComponentInChildren<SupplyService>(true);
        if (supply == null) return;

        supply.Changed += OnSupplyChanged;
        supply.Delivered += OnDelivered;
        supplyGroup.gameObject.SetActive(true);
        lastPoints = int.MinValue;
    }

    void Unbind()
    {
        if (supply == null) return;
        supply.Changed -= OnSupplyChanged;
        supply.Delivered -= OnDelivered;
        supply = null;
    }

    void OnSupplyChanged()
    {
        if (supply != null && supply.Pending) deliveryTotal = Mathf.Max(0.01f, supply.Remaining);
    }

    void OnDelivered() => deliveredUntil = Time.unscaledTime + 2.5f;

    void UpdateSupply()
    {
        if (supply == null) return;

        int pts = supply.Points != null ? supply.Points.Points : 0;
        if (pts != lastPoints) { lastPoints = pts; pointsText.text = pts.ToString("N0"); }

        bool flash = Time.unscaledTime < deliveredUntil;
        var sd = supplyFill.sizeDelta;
        if (supply.Pending)
        {
            supplyStatus.text = "INBOUND " + Mathf.CeilToInt(supply.Remaining) + "s";
            Tint(supplyStatus, ThemeRole.Warn);
            sd.x = (SupW - 20f) * Mathf.Clamp01(1f - supply.Remaining / deliveryTotal);
        }
        else
        {
            supplyStatus.text = flash ? "DELIVERED" : "STANDBY";
            Tint(supplyStatus, flash ? ThemeRole.InkBright : ThemeRole.InkMid);
            sd.x = 0f;
        }
        supplyFill.sizeDelta = sd;
    }

    // ---------- ลูกศรชี้เป้า (ทุกเกตที่ยังไม่ปิด) ----------
    ArrowItem GetArrow(int i)
    {
        while (arrows.Count <= i)
        {
            var it = new ArrowItem();

            it.rt = UiKit.Rect("Arrow" + arrows.Count, Root);
            it.rt.anchorMin = it.rt.anchorMax = it.rt.pivot = new Vector2(0.5f, 0.5f);
            it.rt.sizeDelta = new Vector2(22f, 26f);
            it.rt.localScale = Vector3.one * uiScale;
            it.rt.gameObject.AddComponent<CanvasRenderer>();
            it.gfx = it.rt.gameObject.AddComponent<TriangleGraphic>();
            it.gfx.raycastTarget = false;

            it.label = kit.Label(Root, "ArrowLabel" + arrows.Count, "", 12, ThemeRole.InkBright, true,
                                 TextAlignmentOptions.Center);
            it.lrt = it.label.rectTransform;
            it.lrt.anchorMin = it.lrt.anchorMax = it.lrt.pivot = new Vector2(0.5f, 0.5f);
            it.lrt.sizeDelta = new Vector2(110f, 36f);
            it.lrt.localScale = Vector3.one * uiScale;

            arrows.Add(it);
        }
        return arrows[i];
    }

    void HideArrowsFrom(int start)
    {
        for (int i = start; i < arrows.Count; i++)
        {
            if (arrows[i].rt.gameObject.activeSelf) arrows[i].rt.gameObject.SetActive(false);
            if (arrows[i].lrt.gameObject.activeSelf) arrows[i].lrt.gameObject.SetActive(false);
        }
    }

    // วางลูกศรหนึ่งอัน true = แสดง (เป้าหมายอยู่นอกจอ)
    bool PlaceArrow(ArrowItem it, Camera cam, Vector2 me, Vector2 target, string name, int id, bool highlight, ThemeRole role)
    {
        Vector3 vp = cam.WorldToViewportPoint(target);
        float lim = onScreenLimit * 0.5f;
        if (vp.z > 0f && Mathf.Abs(vp.x - 0.5f) < lim && Mathf.Abs(vp.y - 0.5f) < lim) return false;

        Rect r = Root.rect;
        Vector2 d = new Vector2((vp.x - 0.5f) * r.width, (vp.y - 0.5f) * r.height);
        if (vp.z < 0f) d = -d;
        if (d.sqrMagnitude < 1f) d = Vector2.up;

        float sx = Mathf.Abs(d.x) > 0.001f ? (r.width * 0.5f - arrowEdgeMargin) / Mathf.Abs(d.x) : float.MaxValue;
        float sy = Mathf.Abs(d.y) > 0.001f ? (r.height * 0.5f - arrowEdgeMargin) / Mathf.Abs(d.y) : float.MaxValue;
        Vector2 pos = d * Mathf.Min(sx, sy);
        Vector2 dir = d.normalized;

        it.rt.gameObject.SetActive(true);
        it.lrt.gameObject.SetActive(true);
        it.rt.anchoredPosition = pos;
        it.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        it.lrt.anchoredPosition = pos - dir * (42f * uiScale);

        float a = highlight ? 1f : 0.6f;
        Tint(it.gfx, role, a);
        Tint(it.label, highlight ? ThemeRole.InkBright : ThemeRole.InkMid, a);

        int dist = Mathf.RoundToInt(Vector2.Distance(me, target));
        int key = dist * 64 + id;
        if (key != it.lastKey)
        {
            it.lastKey = key;
            it.label.text = name + "\n" + dist + "m";
        }
        return true;
    }

    void UpdateArrows()
    {
        if (BootSequence.HudProgress < 1f) { HideArrowsFrom(0); return; }   // รอ HUD เลื่อนเข้าเสร็จก่อนค่อยโชว์ลูกศร
        var mm = MissionManager.Instance;
        var rm = RunManager.Instance;
        var cam = Camera.main;
        var tag = PlayerTag.Instance;
        if (mm == null || cam == null || tag == null || (rm != null && rm.State != RunManager.RunState.Playing))
        {
            HideArrowsFrom(0);
            return;
        }

        Vector2 me = tag.transform.position;
        int used = 0;

        if (mm.Phase == MissionPhase.Mission)
        {
            var infos = mm.GateInfos;

            // หาเกตที่ใกล้ที่สุด (ไว้เน้นสี)
            int nearest = -1; float best = float.MaxValue;
            for (int i = 0; i < infos.Count; i++)
            {
                if (infos[i].closed) continue;
                float d = (infos[i].position - me).sqrMagnitude;
                if (d < best) { best = d; nearest = i; }
            }

            for (int i = 0; i < infos.Count; i++)
            {
                if (infos[i].closed) continue;
                var it = GetArrow(used);
                if (PlaceArrow(it, cam, me, infos[i].position, "GATE " + (i + 1), i + 1, i == nearest, ThemeRole.Warn)) used++;
                else
                {
                    it.rt.gameObject.SetActive(false);
                    it.lrt.gameObject.SetActive(false);
                }
            }
        }
        else if (mm.Phase == MissionPhase.Extract)
        {
            var it = GetArrow(used);
            if (PlaceArrow(it, cam, me, mm.Origin, "EXTRACT", 63, true, ThemeRole.InkBright)) used++;
            else { it.rt.gameObject.SetActive(false); it.lrt.gameObject.SetActive(false); }
        }

        HideArrowsFrom(used);
    }
}