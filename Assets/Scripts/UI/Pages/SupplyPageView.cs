using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้า SUP สร้างด้วยโค้ด ซ้าย = ของที่ถืออยู่ ขวา = เลือกของที่จะขอ
// วางบน Page_Supply (ยืดเต็มพื้นที่เนื้อหา ห้ามมี Layout Group)
public class SupplyPageView : MonoBehaviour
{
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;

    const int Segs = 20;
    const float RowH = 62f, TopY = 44f, Gap = 14f;

    UiKit kit;
    bool built;
    SupplyService svc;
    int[] picks;

    class StockRow { public TextMeshProUGUI count; public Image[] segs = new Image[Segs]; }
    class ReqRow { public TextMeshProUGUI count, info; public CutCornerFrame minus, plus; }
    readonly List<StockRow> stock = new List<StockRow>();
    readonly List<ReqRow> req = new List<ReqRow>();

    RectTransform left, right;
    TextMeshProUGUI pointsText, totalText, statusText, reqLabel;
    CutCornerFrame reqButton;
    int lastReqState = -2;

    void OnEnable()
    {
        if (!built) { kit = new UiKit { headFont = headFont, monoFont = monoFont }; built = true; BuildFrame(); }
        UiThemeSource.Changed += OnTheme;
    }
    void OnDisable() => UiThemeSource.Changed -= OnTheme;
    void OnTheme() { if (built) kit.Refresh(); }

    // ---------- โครงที่ไม่ต้องรอข้อมูล ----------
    void BuildFrame()
    {
        var root = (RectTransform)transform;
        left = Card(root, "StockCard", 0, 0.5f, 0, Gap * 0.5f, "STOCK ON HAND");
        right = Card(root, "RequestCard", 0.5f, 1f, Gap * 0.5f, 0, "REQUEST RESUPPLY");

        pointsText = kit.Label(left, "Points", "POINTS  0", 30, ThemeRole.InkBright, true, TextAlignmentOptions.BottomLeft);
        pointsText.rectTransform.anchorMin = new Vector2(0, 0); pointsText.rectTransform.anchorMax = new Vector2(1, 0);
        pointsText.rectTransform.pivot = new Vector2(0, 0);
        pointsText.rectTransform.offsetMin = new Vector2(16, 14); pointsText.rectTransform.offsetMax = new Vector2(-16, 50);
    }

    RectTransform Card(RectTransform parent, string name, float ax0, float ax1, float padL, float padR, string title)
    {
        var f = kit.Frame(parent, name, ThemeRole.InkLow, 1f, ThemeRole.Surface, 0.35f);
        var rt = f.rectTransform;
        rt.anchorMin = new Vector2(ax0, 0); rt.anchorMax = new Vector2(ax1, 1);
        rt.offsetMin = new Vector2(padL, 0); rt.offsetMax = new Vector2(-padR, 0);
        var h = kit.Label(rt, "Heading", title, 16, ThemeRole.InkMid, true);
        UiKit.TopLeft(h.rectTransform, 16, 10, 400, 22);
        return rt;
    }

    // ---------- สร้างแถวเมื่อได้ข้อมูลแล้ว ----------
    bool Bind()
    {
        if (svc == null)
        {
            var tag = PlayerTag.Instance;
            svc = tag != null ? tag.GetComponent<SupplyService>() : null;
            if (svc == null) return false;
            picks = new int[svc.Items.Count];
            for (int i = 0; i < svc.Items.Count; i++) BuildRows(i);
            BuildRequestFooter();
        }
        return true;
    }

    void BuildRows(int i)
    {
        var it = svc.Items[i];
        float y = TopY + i * RowH;
        string name = it.type != null ? it.type.displayName : "-";

        // ซ้าย: ชื่อ จำนวน แถบ
        var sr = new StockRow();
        var n = kit.Label(left, "Name" + i, name, 20, ThemeRole.InkBright, true);
        UiKit.TopLeft(n.rectTransform, 16, y + 4, 260, 26);
        sr.count = kit.Label(left, "Count" + i, "0/0", 22, ThemeRole.InkMid, false, TextAlignmentOptions.TopRight);
        sr.count.rectTransform.anchorMin = sr.count.rectTransform.anchorMax = sr.count.rectTransform.pivot = new Vector2(1, 1);
        sr.count.rectTransform.sizeDelta = new Vector2(160, 26); sr.count.rectTransform.anchoredPosition = new Vector2(-16, -(y + 4));
        var segRoot = UiKit.Rect("Bar" + i, left);
        segRoot.anchorMin = new Vector2(0, 1); segRoot.anchorMax = new Vector2(1, 1); segRoot.pivot = new Vector2(0.5f, 1);
        segRoot.offsetMin = new Vector2(16, -(y + 46)); segRoot.offsetMax = new Vector2(-16, -(y + 34));
        for (int s = 0; s < Segs; s++)
        {
            var img = kit.Box(segRoot, "S" + s, ThemeRole.InkLow, 0.3f);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(s / (float)Segs, 0); rt.anchorMax = new Vector2((s + 1) / (float)Segs, 1);
            rt.offsetMin = new Vector2(1.5f, 0); rt.offsetMax = new Vector2(-1.5f, 0);
            sr.segs[s] = img;
        }
        stock.Add(sr);

        // ขวา: ชื่อ ราคา [-] จำนวน [+]
        var rr = new ReqRow();
        int idx = i;
        var rn = kit.Label(right, "RName" + i, name, 20, ThemeRole.InkBright, true);
        UiKit.TopLeft(rn.rectTransform, 16, y + 4, 260, 26);
        rr.info = kit.Label(right, "Info" + i, "", 14, ThemeRole.InkDim);
        UiKit.TopLeft(rr.info.rectTransform, 16, y + 32, 300, 20);

        rr.minus = Button(right, "Minus" + i, "-", 44, 40, () => Change(idx, -1));
        rr.plus = Button(right, "Plus" + i, "+", 44, 40, () => Change(idx, +1));
        PlaceRight(rr.plus.rectTransform, 16, y + 6, 44, 40);
        rr.count = kit.Label(right, "Pick" + i, "0", 30, ThemeRole.InkBright, false, TextAlignmentOptions.Center);
        PlaceRight(rr.count.rectTransform, 16 + 44 + 4, y + 6, 70, 40);
        PlaceRight(rr.minus.rectTransform, 16 + 44 + 4 + 70 + 4, y + 6, 44, 40);
        req.Add(rr);
    }

    static void PlaceRight(RectTransform rt, float fromRight, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(-fromRight, -y);
    }

    void BuildRequestFooter()
    {
        float y = TopY + svc.Items.Count * RowH + 8;
        totalText = kit.Label(right, "Total", "TOTAL  0", 22, ThemeRole.InkMid, true);
        UiKit.TopLeft(totalText.rectTransform, 16, y, 300, 28);
        statusText = kit.Label(right, "Status", "", 20, ThemeRole.Warn, true, TextAlignmentOptions.TopRight);
        PlaceRight(statusText.rectTransform, 16, y, 240, 28);

        reqButton = Button(right, "RequestBtn", "", 0, 52, () => { if (svc.Request(picks)) System.Array.Clear(picks, 0, picks.Length); });
        var rt = reqButton.rectTransform;
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(16, 14); rt.offsetMax = new Vector2(-16, 66);
        reqLabel = reqButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    CutCornerFrame Button(Transform parent, string name, string label, float w, float h, UnityEngine.Events.UnityAction onClick)
    {
        var f = kit.Frame(parent, name, ThemeRole.InkMid, 1f, ThemeRole.Surface, 0.4f);
        f.raycastTarget = true;
        var b = f.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.targetGraphic = f;
        b.onClick.AddListener(onClick);
        var t = kit.Label(f.transform, "Label", label, 26, ThemeRole.InkBright, true, TextAlignmentOptions.Center);
        UiKit.Stretch(t.rectTransform);
        return f;
    }

    void Change(int i, int d)
    {
        if (svc == null) return;
        picks[i] = Mathf.Clamp(picks[i] + d, 0, svc.MaxPacks(i));
    }

    // ---------- อัปเดต ----------
    void Update()
    {
        if (!built || !Bind()) return;

        // ซ้าย
        for (int i = 0; i < stock.Count; i++)
        {
            var it = svc.Items[i];
            if (it.type == null) continue;
            int cur = svc.Inventory.Get(it.type), max = it.type.maxAmount;
            float ratio = max > 0 ? cur / (float)max : 0f;
            var sr = stock[i];
            sr.count.text = cur + "/" + max;
            sr.count.color = cur > 0 ? UiThemeSource.Role(ThemeRole.InkMid) : UiThemeSource.Role(ThemeRole.Danger);
            int lit = Mathf.CeilToInt(ratio * Segs);
            Color on = UiThemeSource.LevelColor(ratio);
            Color off = UiThemeSource.Role(ThemeRole.InkLow); off.a = 0.3f;
            for (int s = 0; s < Segs; s++) sr.segs[s].color = s < lit ? on : off;
        }
        pointsText.text = "POINTS  " + svc.Points.Points;

        // ขวา
        int total = svc.TotalCost(picks);
        bool afford = svc.Points.Points >= total;
        for (int i = 0; i < req.Count; i++)
        {
            picks[i] = Mathf.Min(picks[i], svc.MaxPacks(i));   // ของเต็มคลังแล้วขอเพิ่มไม่ได้
            var it = svc.Items[i]; var rr = req[i];
            rr.count.text = picks[i].ToString();
            rr.info.text = "+" + it.amount + " / PACK   COST " + it.cost;
        }
        totalText.text = "TOTAL  " + total;
        totalText.color = UiThemeSource.Role(afford ? ThemeRole.InkMid : ThemeRole.Danger);

        bool can = svc.CanRequest(picks);
        statusText.text = svc.Pending ? "INBOUND  " + Mathf.CeilToInt(svc.Remaining).ToString("00") + "s" : "";
        int state = svc.Pending ? 1 : can ? 2 : 0;
        reqLabel.text = svc.Pending ? "WAITING FOR DELIVERY" : can ? "REQUEST" : (total > 0 ? "NOT ENOUGH POINTS" : "SELECT ITEMS");
        if (state != lastReqState)
        {
            lastReqState = state;
            reqButton.SetStyle(can ? ThemeRole.InkBright : ThemeRole.InkLow, 1f, ThemeRole.Surface, can ? 0.4f : 0.2f);
            reqButton.GetComponent<Button>().interactable = can;
        }
        reqLabel.color = UiThemeSource.Role(can ? ThemeRole.InkBright : ThemeRole.InkLow);
    }
}