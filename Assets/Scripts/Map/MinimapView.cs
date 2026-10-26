using System.Collections.Generic;
using EnemyV2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// minimap วงกลมมุมซ้ายบน เหนือคงที่ ไม่หมุน  สร้าง UI ทั้งหมดด้วยโค้ด (ไม่ต้องทำ prefab/sprite)
//   วางบน GameObject ว่างที่เป็นลูกของ Canvas ของ HUD แล้วใส่ MapStyle  สคริปต์จัดตำแหน่ง/ขนาดเอง
//   icon: swarm/leader (ในวงเห็นตำแหน่งจริง เกินเล็กน้อยเกาะขอบ ไกลกว่านั้นหาย)  gate/จุดส่ง (เกาะขอบเสมอเมื่ออยู่นอกวง)
//   เส้น: grid(เขียว) วงเตือน(แดง) สนามอันตราย gate(ม่วง) วง active gate(น้ำเงิน)
public class MinimapView : MonoBehaviour
{
    [SerializeField] MapStyle style;

    RectTransform iconLayer;
    Image playerArrow;
    MapLines grid, ringWarn, ringField, ringActive, border;
    Sprite circleBig, circleIcon, arrowSprite;

    readonly List<Image> pool = new List<Image>();
    int used;

    Rigidbody2D playerRb;
    Vector2 lastDir = Vector2.up;
    float R, ppu;

    // ---------- สร้าง UI ----------

    void Awake()
    {
        if (style == null)
        {
            Debug.LogError("MinimapView: ต้องใส่ MapStyle", this);
            enabled = false;
            return;
        }

        R = style.diameter * 0.5f;

        var rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(style.margin, -style.margin);
        rt.sizeDelta = new Vector2(style.diameter, style.diameter);

        // Canvas ซ้อน: icon ขยับทุกเฟรม จะไม่ทำให้ canvas ของ HUD ทั้งหมดต้อง rebuild
        if (GetComponent<Canvas>() == null) gameObject.AddComponent<Canvas>();

        circleBig = MakeCircleSprite(256);
        circleIcon = MakeCircleSprite(64);
        arrowSprite = MakeArrowSprite(64);

        Image bg = NewImage("Bg", rt, circleBig, style.bgColor);
        Stretch(bg.rectTransform);

        RectTransform content = NewRect("Content", rt);
        Stretch(content);
        Image maskImg = content.gameObject.AddComponent<Image>();
        maskImg.sprite = circleBig;
        maskImg.raycastTarget = false;
        Mask mask = content.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        grid = NewLines("Grid", content, style.gridColor);
        ringWarn = NewLines("RingWarn", content, style.warnColor);
        ringField = NewLines("RingField", content, style.fieldColor);
        ringActive = NewLines("RingActive", content, style.activeColor);

        iconLayer = NewRect("Icons", content);
        Stretch(iconLayer);

        playerArrow = NewImage("Player", content, arrowSprite, style.playerColor);
        playerArrow.rectTransform.anchorMin = playerArrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrow.rectTransform.sizeDelta = new Vector2(style.playerSize, style.playerSize);

        border = NewLines("Border", rt, style.borderColor);
        border.Begin(style.borderThickness);
        border.Circle(Vector2.zero, R - style.borderThickness * 0.5f, R * 2f);
        border.Commit();

        RectTransform nrt = NewRect("N", rt);
        nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 1f);
        nrt.pivot = new Vector2(0.5f, 1f);
        nrt.anchoredPosition = new Vector2(0f, -4f);
        nrt.sizeDelta = new Vector2(30f, 20f);
        TextMeshProUGUI n = nrt.gameObject.AddComponent<TextMeshProUGUI>();
        n.text = "N";
        n.fontSize = 14f;
        n.alignment = TextAlignmentOptions.Center;
        n.color = style.borderColor;
        n.raycastTarget = false;
    }

    void OnDestroy()
    {
        DestroySprite(circleBig);
        DestroySprite(circleIcon);
        DestroySprite(arrowSprite);
    }

    // ---------- อัปเดต ----------

    void LateUpdate()
    {
        used = 0;

        if (!EnemyTarget.Ensure())
        {
            ClearLines();
            HideUnused();
            return;
        }

        Vector2 pp = EnemyTarget.Transform.position;
        bool dead = EnemyTarget.PlayerHealth != null && EnemyTarget.PlayerHealth.IsDead;
        ppu = R / style.visibleRadius;

        DrawGrid(pp);
        DrawRings(pp);

        MissionManager mm = MissionManager.Instance;

        // ลำดับวาด: swarm → leader → gate → จุดส่ง (ตัวหลังอยู่บน)  ผู้เล่นตาย = ซ่อนศัตรู
        if (!dead)
        {
            DrawEnemies(pp, MapIconType.Swarm, style.swarmSize, style.swarmColor);
            DrawEnemies(pp, MapIconType.Leader, style.leaderSize, style.leaderColor);
        }

        IReadOnlyList<Gate> gates = Gate.All;
        for (int i = 0; i < gates.Count; i++)
        {
            Gate g = gates[i];
            if (g == null || g.State == GateState.Closed) continue;

            float size = style.gateSize;
            if (Place(g.transform.position, true, size, pp, out Vector2 local))
            {
                if (g.State == GateState.Charging) size *= 1f + 0.25f * Mathf.Sin(Time.time * style.pulseSpeed);
                Show(local, circleIcon, style.gateColor, size);
            }
        }

        if (mm != null)
        {
            float size = style.extractionSize;
            if (Place(mm.Origin, true, size, pp, out Vector2 local))
            {
                if (mm.Phase == MissionPhase.Extract) size *= 1f + 0.25f * Mathf.Sin(Time.time * style.pulseSpeed);
                Show(local, null, style.extractionColor, size);
            }
        }

        UpdatePlayerArrow(pp);
        HideUnused();
    }

    void DrawEnemies(Vector2 pp, MapIconType type, float size, Color col)
    {
        var list = MapIconSource.All;
        for (int i = 0; i < list.Count; i++)
        {
            MapIconSource s = list[i];
            if (s.Type != type || !s.IsAlive) continue;
            if (Place(s.Position, false, size, pp, out Vector2 local))
                Show(local, circleIcon, col, size);
        }
    }

    // world → ตำแหน่งในวง  ในวง = ตำแหน่งจริง  นอกวง: alwaysShow = เกาะขอบ  ไม่ใช่ = เกาะขอบถ้าเกินไม่เกิน edgeMargin
    bool Place(Vector2 world, bool alwaysShow, float sizePx, Vector2 pp, out Vector2 local)
    {
        Vector2 d = world - pp;
        float distWorld = d.magnitude;
        float edge = R - sizePx * 0.5f - style.edgePad;

        if (distWorld * ppu <= edge)
        {
            local = d * ppu;
            return true;
        }

        if (!alwaysShow && distWorld > style.visibleRadius + style.edgeMargin)
        {
            local = Vector2.zero;
            return false;
        }

        local = d / distWorld * edge;
        return true;
    }

    void UpdatePlayerArrow(Vector2 pp)
    {
        if (playerRb == null) playerRb = EnemyTarget.Transform.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            Vector2 v = playerRb.linearVelocity;
            if (v.sqrMagnitude > style.arrowMinSpeed * style.arrowMinSpeed) lastDir = v.normalized;
        }
        float ang = Mathf.Atan2(lastDir.y, lastDir.x) * Mathf.Rad2Deg - 90f;   // sprite ลูกศรชี้ขึ้น
        playerArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ang);
    }

    // ---------- เส้น ----------

    void DrawGrid(Vector2 pp)
    {
        grid.Begin(style.lineThickness);
        float s = style.gridSpacing;
        if (s > 0.01f)
        {
            float vr = style.visibleRadius;
            int x0 = Mathf.FloorToInt((pp.x - vr) / s), x1 = Mathf.CeilToInt((pp.x + vr) / s);
            for (int k = x0; k <= x1; k++)
            {
                float x = (k * s - pp.x) * ppu;
                grid.Line(new Vector2(x, -R), new Vector2(x, R));
            }
            int y0 = Mathf.FloorToInt((pp.y - vr) / s), y1 = Mathf.CeilToInt((pp.y + vr) / s);
            for (int k = y0; k <= y1; k++)
            {
                float y = (k * s - pp.y) * ppu;
                grid.Line(new Vector2(-R, y), new Vector2(R, y));
            }
        }
        grid.Commit();
    }

    void DrawRings(Vector2 pp)
    {
        float th = style.lineThickness;
        ringWarn.Begin(th);
        ringField.Begin(th);
        ringActive.Begin(th);

        MissionManager mm = MissionManager.Instance;
        if (mm != null)
            ringWarn.Circle((mm.Origin - pp) * ppu, mm.WarnRadius * ppu, R);

        IReadOnlyList<Gate> gates = Gate.All;
        for (int i = 0; i < gates.Count; i++)
        {
            Gate g = gates[i];
            if (g == null || g.State == GateState.Closed) continue;

            Vector2 c = ((Vector2)g.transform.position - pp) * ppu;
            if (g.TryGetComponent(out GateField gf)) ringField.Circle(c, gf.CurrentRadius * ppu, R);
            if (g.Data != null) ringActive.Circle(c, g.Data.activeRadius * ppu, R);
        }

        ringWarn.Commit();
        ringField.Commit();
        ringActive.Commit();
    }

    void ClearLines()
    {
        grid.Begin(1f); grid.Commit();
        ringWarn.Begin(1f); ringWarn.Commit();
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
            im = NewImage("i", iconLayer, null, Color.white);
            im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pool.Add(im);
        }
        used++;

        if (!im.gameObject.activeSelf) im.gameObject.SetActive(true);
        im.sprite = sprite;     // null = สี่เหลี่ยมทึบ
        im.color = col;
        RectTransform r = im.rectTransform;
        r.sizeDelta = new Vector2(size, size);
        r.anchoredPosition = local;
    }

    void HideUnused()
    {
        for (int i = used; i < pool.Count; i++)
            if (pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);
    }

    // ---------- ตัวช่วยสร้าง UI / sprite ----------

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Image im = rt.gameObject.AddComponent<Image>();
        im.sprite = sprite;
        im.color = color;
        im.raycastTarget = false;
        return im;
    }

    static MapLines NewLines(string name, Transform parent, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Stretch(rt);
        MapLines l = rt.gameObject.AddComponent<MapLines>();
        l.color = color;
        l.raycastTarget = false;
        return l;
    }

    public static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f, r = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(r - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ลูกศรชี้ขึ้น (หัวลูกศรมีรอยบากด้านล่าง)
    public static Sprite MakeArrowSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Vector2[] poly =
        {
            new Vector2(0.50f, 0.95f),
            new Vector2(0.88f, 0.08f),
            new Vector2(0.50f, 0.28f),
            new Vector2(0.12f, 0.08f)
        };
        var px = new Color32[size * size];
        const int ss = 4;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float fx = (x + (sx + 0.5f) / ss) / size;
                        float fy = (y + (sy + 0.5f) / ss) / size;
                        if (Inside(poly, fx, fy)) hit++;
                    }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255f * hit / (ss * ss)));
            }
        tex.SetPixels32(px);
        tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool Inside(Vector2[] p, float x, float y)
    {
        bool c = false;
        for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
        {
            if (((p[i].y > y) != (p[j].y > y)) &&
                (x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x))
                c = !c;
        }
        return c;
    }

    static void DestroySprite(Sprite s)
    {
        if (s == null) return;
        if (s.texture != null) Destroy(s.texture);
        Destroy(s);
    }
}