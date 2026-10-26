using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ลากเมาส์บนแถบเพื่อกำหนดค่า 0-1 (ใช้กับสไลเดอร์เสียง)
public class BarInput : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Action<float> onValue;

    public void OnPointerDown(PointerEventData e) => Set(e);
    public void OnDrag(PointerEventData e) => Set(e);

    void Set(PointerEventData e)
    {
        var rt = (RectTransform)transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 p))
            onValue?.Invoke(Mathf.InverseLerp(rt.rect.xMin, rt.rect.xMax, p.x));
    }
}

// แผง Settings (พิกัดอ้างอิงเวทีขนาด 1280x720) ใช้ได้ทั้งเมนูหลักและเมนู Pause
public class SettingsPanel
{
    const float W = 1280f, H = 720f, PW = 640f;

    readonly UiKit kit;
    readonly RectTransform parent;
    readonly Action onClose;
    readonly bool allowErase;

    RectTransform root;
    bool eraseArmed;
    bool? fullShown;   // โหมดจอที่ผู้เล่นเพิ่งเลือก (Screen.fullScreen อัปเดตช้ากว่าการคลิก)

    public SettingsPanel(UiKit kit, RectTransform parent, Action onClose, bool allowErase)
    {
        this.kit = kit; this.parent = parent; this.onClose = onClose; this.allowErase = allowErase;
    }

    // บันทึกค่าแล้วแจ้งผู้เรียกให้ปิดแผง
    public void Close()
    {
        GameSettings.Save();
        AudioPrefs.Save();
        onClose?.Invoke();
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

    CutCornerFrame Card(Transform p, float x, float y, float w, float h,
                        ThemeRole outline, float oa, ThemeRole fill, float fa)
    {
        var f = kit.Frame(p, "Card", outline, oa, fill, fa);
        UiKit.TopLeft(f.rectTransform, x, y, w, h);
        return f;
    }

    TriangleGraphic Tri(Transform p, float x, float y, float w, float h, ThemeRole r, bool left)
    {
        var rt = UiKit.Rect("Tri", p);
        UiKit.TopLeft(rt, x, y, w, h);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var t = rt.gameObject.AddComponent<TriangleGraphic>();
        t.pointLeft = left; t.raycastTarget = false;
        kit.Bind(t, r, 1f);
        return t;
    }

    static void Hit(RectTransform parent, Action act)
    {
        var rt = UiKit.Rect("Hit", parent);
        UiKit.Stretch(rt);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.color = Color.clear; img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None; b.targetGraphic = img;
        b.onClick.AddListener(GameAudio.UiClick);
        b.onClick.AddListener(() => act());
    }

    // ---------- สร้างแผง ----------
    public void Build()
    {
        if (root != null) { root.gameObject.SetActive(false); UnityEngine.Object.Destroy(root.gameObject); }
        root = UiKit.Rect("SettingsPanel", parent);
        UiKit.Stretch(root);

        // ชั้นมืด + กันคลิกทะลุ
        var dim = B(root, 0, 0, W, H, ThemeRole.Background, 0.82f);
        dim.raycastTarget = true;

        float ph = allowErase ? 650f : 590f;
        float px = (W - PW) / 2f, py = (H - ph) / 2f;
        var card = Card(root, px, py, PW, ph, ThemeRole.InkMid, 0.9f, ThemeRole.Surface, 0.96f);
        var p = card.rectTransform;

        B(p, 40, 30, 3, 14, ThemeRole.InkLow, 1f);
        L(p, "SETTINGS", 50, 22, 300, 30, 18, ThemeRole.InkBright, true);
        B(p, 40, 60, PW - 80, 1, ThemeRole.InkLow, 0.6f);

        float y = 74f;

        Section(p, "DISPLAY", ref y);
        Toggle(p, ref y, "FULLSCREEN", fullShown ?? GameSettings.Fullscreen, v => { fullShown = v; GameSettings.SetFullscreen(v); });
        Toggle(p, ref y, "VSYNC", GameSettings.VSync, GameSettings.SetVSync);
        FpsRow(p, ref y);

        Section(p, "EFFECTS", ref y);
        Toggle(p, ref y, "CRT EFFECT", GameSettings.CrtEffect, GameSettings.SetCrtEffect);

        Section(p, "AUDIO", ref y);
        Slider(p, ref y, "MASTER", AudioBus.Master);
        Slider(p, ref y, "MUSIC", AudioBus.Music);
        Slider(p, ref y, "SFX", AudioBus.Sfx);

        if (allowErase)
        {
            Section(p, "SAVE DATA", ref y);
            EraseRow(p, ref y);
        }

        var close = Card(p, (PW - 220) / 2f, ph - 70, 220, 48, ThemeRole.InkBright, 1f, ThemeRole.Surface, 0.9f);
        L(close.rectTransform, "CLOSE", 0, 0, 220, 48, 16, ThemeRole.InkBright, true, TextAlignmentOptions.Center);
        Hit(close.rectTransform, Close);
    }

    void Section(RectTransform p, string name, ref float y)
    {
        B(p, 40, y + 5, 3, 12, ThemeRole.InkLow, 1f);
        L(p, name, 50, y, 300, 22, 12, ThemeRole.InkLow, true);
        y += 28f;
    }

    void RowBg(RectTransform p, float y, string label)
    {
        B(p, 40, y, PW - 80, 44, ThemeRole.InkMid, 0.07f);
        B(p, 40, y, 3, 44, ThemeRole.InkBright, 1f);
        L(p, label, 56, y, 260, 44, 15, ThemeRole.InkMid, true);
    }

    void Toggle(RectTransform p, ref float y, string label, bool value, Action<bool> set)
    {
        RowBg(p, y, label);
        var role = value ? ThemeRole.InkBright : ThemeRole.InkLow;
        var chip = Card(p, PW - 52 - 150, y + 6, 150, 32, role, 1f, ThemeRole.Surface, value ? 0.6f : 0.3f);
        L(chip.rectTransform, value ? "ON" : "OFF", 0, 0, 150, 32, 13, role, true, TextAlignmentOptions.Center);
        Hit(chip.rectTransform, () => { set(!value); Build(); });
        y += 54f;
    }

    void FpsRow(RectTransform p, ref float y)
    {
        RowBg(p, y, "FPS LIMIT");
        float yy = y;

        if (GameSettings.VSync)
        {
            L(p, "LOCKED TO DISPLAY (VSYNC ON)", PW - 52 - 280, yy, 280, 44, 12, ThemeRole.InkLow, false,
              TextAlignmentOptions.MidlineRight);
        }
        else
        {
            var opts = GameSettings.FpsOptions;
            int idx = Mathf.Max(0, Array.IndexOf(opts, GameSettings.FpsLimit));
            float x0 = PW - 52 - 180;

            var l = Card(p, x0, yy + 6, 32, 32, ThemeRole.InkMid, 1f, ThemeRole.Surface, 0.5f);
            Tri(l.rectTransform, 11, 9, 9, 14, ThemeRole.InkMid, true);
            Hit(l.rectTransform, () => { GameSettings.SetFpsLimit(opts[(idx - 1 + opts.Length) % opts.Length]); Build(); });

            L(p, GameSettings.FpsLabel(opts[idx]), x0 + 36, yy, 108, 44, 14, ThemeRole.InkBright, true,
              TextAlignmentOptions.Center);

            var r = Card(p, x0 + 148, yy + 6, 32, 32, ThemeRole.InkMid, 1f, ThemeRole.Surface, 0.5f);
            Tri(r.rectTransform, 12, 9, 9, 14, ThemeRole.InkMid, false);
            Hit(r.rectTransform, () => { GameSettings.SetFpsLimit(opts[(idx + 1) % opts.Length]); Build(); });
        }
        y += 54f;
    }

    void Slider(RectTransform p, ref float y, string label, AudioBus bus)
    {
        RowBg(p, y, label);
        float yy = y;
        const int N = 20;
        float bx = 210f, bw = 318f, by = yy + 14f, sw = bw / N;

        var on = new Image[N];
        for (int i = 0; i < N; i++)
        {
            B(p, bx + i * sw, by, sw - 3, 16, ThemeRole.InkMid, 0.15f);
            on[i] = B(p, bx + i * sw, by, sw - 3, 16, ThemeRole.InkBright, 1f);
        }
        var valueText = L(p, "", PW - 52 - 56, yy, 56, 44, 14, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);

        void Show(float v)
        {
            int k = Mathf.RoundToInt(v * N);
            for (int i = 0; i < N; i++) on[i].gameObject.SetActive(i < k);
            valueText.text = Mathf.RoundToInt(v * 100f) + "%";
        }
        Show(AudioPrefs.Get(bus));

        // พื้นที่รับเมาส์ทับแถบ (ไม่สร้างแผงใหม่ระหว่างลาก ไม่งั้นการลากจะขาด)
        var hit = UiKit.Rect("Bar", p);
        UiKit.TopLeft(hit, bx, by - 6f, bw, 28f);
        hit.gameObject.AddComponent<CanvasRenderer>();
        var img = hit.gameObject.AddComponent<Image>();
        img.color = Color.clear; img.raycastTarget = true;
        var input = hit.gameObject.AddComponent<BarInput>();
        input.onValue = t =>
        {
            float v = Mathf.Round(Mathf.Clamp01(t) * N) / N;
            AudioPrefs.Set(bus, v);
            Show(v);
        };

        y += 54f;
    }

    void EraseRow(RectTransform p, ref float y)
    {
        RowBg(p, y, "ERASE SAVE");
        var role = eraseArmed ? ThemeRole.Warn : ThemeRole.InkMid;
        var chip = Card(p, PW - 52 - 190, y + 6, 190, 32, role, 1f, ThemeRole.Surface, 0.5f);
        L(chip.rectTransform, eraseArmed ? "CONFIRM ERASE?" : "ERASE", 0, 0, 190, 32, 13, role, true,
          TextAlignmentOptions.Center);
        Hit(chip.rectTransform, () =>
        {
            if (!eraseArmed) { eraseArmed = true; Build(); return; }
            SaveService.Reset();
            GameSession.Clear();
            eraseArmed = false;
            Build();
        });
        y += 54f;
    }
}