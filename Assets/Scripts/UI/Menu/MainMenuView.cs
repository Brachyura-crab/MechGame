using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ตัวช่วยรับ hover (ใช้กับปุ่มเมนู)
public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public System.Action<bool> onHover;
    public void OnPointerEnter(PointerEventData e) => onHover?.Invoke(true);
    public void OnPointerExit(PointerEventData e) => onHover?.Invoke(false);
}

// เมนูหลัก: พื้นหลัง + logo + ปุ่ม (ภาพวาดใส่ผ่าน Inspector ว่างได้)
public class MainMenuView : MenuScreen
{
    [Header("Scenes (ต้องอยู่ใน Build Settings)")]
    [SerializeField] string startScene = "UnitSelect";

    [Header("งานวาด (ว่างได้)")]
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] Sprite logoSprite;
    [SerializeField] Sprite buttonSprite;          // ภาพปุ่มปกติ (ว่าง = กรอบเทอร์มินัล)
    [SerializeField] Sprite buttonHoverSprite;     // ภาพปุ่มตอนชี้ (ว่าง = ใช้ภาพปกติ)

    [Header("Logo")]
    [SerializeField] Vector2 logoPos = new Vector2(80f, 90f);     // มุมซ้ายบนของ logo (พิกัด 1280x720)
    [SerializeField] Vector2 logoSize = new Vector2(560f, 200f);
    [SerializeField] string titleText = "GAME TITLE";             // ใช้เมื่อยังไม่มี logo sprite
    [SerializeField] string tagline = "MECH OPERATIONS SYSTEM";

    [Header("ปุ่ม")]
    [SerializeField] Vector2 buttonsPos = new Vector2(80f, 350f);
    [SerializeField] Vector2 buttonSize = new Vector2(340f, 54f);
    [SerializeField] float buttonGap = 14f;

    bool optionsOpen;
    SettingsPanel settings;

    protected override void OnBuild()
    {
        BuildBackground();
        Rebuild();
    }

    // ---------- พื้นหลัง (อยู่ใต้ Stage) ----------
    void BuildBackground()
    {
        if (backgroundSprite == null) return;

        var root = stage.parent;
        var rt = UiKit.Rect("BackgroundArt", root);
        UiKit.Stretch(rt);
        rt.SetSiblingIndex(1);   // ต่อจากพื้นสี ก่อน Stage
        rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = backgroundSprite; img.raycastTarget = false;
        img.preserveAspect = false;

        // เต็มจอแบบไม่เสียสัดส่วน (ครอปส่วนเกิน)
        var fit = rt.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
    }

    void Rebuild()
    {
        ResetDyn();
        BuildLogo();
        BuildButtons();

        string ver = "v" + Application.version;
        L(dyn, ver, W - 28 - 200, H - 36, 200, 20, 12, ThemeRole.InkLow, false, TextAlignmentOptions.MidlineRight);

        if (optionsOpen)
        {
            settings = new SettingsPanel(kit, dyn, CloseOptions, true);
            settings.Build();
        }
    }

    void CloseOptions()
    {
        optionsOpen = false;
        Rebuild();
    }

    void BuildLogo()
    {
        if (logoSprite != null)
        {
            var rt = UiKit.Rect("Logo", dyn);
            UiKit.TopLeft(rt, logoPos.x, logoPos.y, logoSize.x, logoSize.y);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = logoSprite; img.preserveAspect = true; img.raycastTarget = false;
            return;
        }

        // ตัวหนังสือชั่วคราวระหว่างรอ logo
        B(dyn, logoPos.x, logoPos.y + 12, 10, 10, ThemeRole.InkBright, 1f);
        L(dyn, "MECH-OS", logoPos.x + 20, logoPos.y, 300, 34, 22, ThemeRole.InkMid, true);
        L(dyn, titleText, logoPos.x, logoPos.y + 40, logoSize.x, 90, 64, ThemeRole.InkBright, true);
        L(dyn, "// " + tagline, logoPos.x + 4, logoPos.y + 134, logoSize.x, 24, 13, ThemeRole.InkLow);
    }

    void BuildButtons()
    {
        string[] labels = { "START", "OPTIONS", "QUIT" };
        System.Action[] acts = { OnStart, () => { optionsOpen = true; Rebuild(); }, OnQuit };

        for (int i = 0; i < labels.Length; i++)
        {
            float y = buttonsPos.y + i * (buttonSize.y + buttonGap);
            MenuButton(dyn, buttonsPos.x, y, buttonSize.x, buttonSize.y, labels[i], acts[i], true);
        }
    }

    // ปุ่มเมนู: ภาพวาด (ถ้ามี) หรือกรอบเทอร์มินัล + ลูกศรตอนชี้
    void MenuButton(Transform parent, float x, float y, float w, float h, string label, System.Action act, bool big)
    {
        RectTransform host;
        CutCornerFrame frame = null;
        Image art = null;

        if (buttonSprite != null)
        {
            var rt = UiKit.Rect("Btn", parent);
            UiKit.TopLeft(rt, x, y, w, h);
            rt.gameObject.AddComponent<CanvasRenderer>();
            art = rt.gameObject.AddComponent<Image>();
            art.sprite = buttonSprite; art.raycastTarget = false;
            host = rt;
        }
        else
        {
            frame = Card(parent, x, y, w, h, ThemeRole.InkMid, 0.8f, ThemeRole.Surface, 0.5f);
            host = frame.rectTransform;
        }

        var tri = Tri(host, 18, h / 2 - 7, 9, 14, ThemeRole.InkBright, 1f, false);
        tri.gameObject.SetActive(false);
        L(host, label, 40, 0, w - 50, h, big ? 20 : 16, ThemeRole.InkMid, true);

        Hit(host, () => act());
        var hit = host.GetChild(host.childCount - 1);
        var relay = hit.gameObject.AddComponent<HoverRelay>();
        relay.onHover = on =>
        {
            if (on) GameAudio.UiHover();
            tri.gameObject.SetActive(on);
            if (frame != null)
                frame.SetStyle(on ? ThemeRole.InkBright : ThemeRole.InkMid, on ? 1f : 0.8f,
                               on ? ThemeRole.InkMid : ThemeRole.Surface, on ? 0.35f : 0.5f);
            if (art != null && buttonHoverSprite != null) art.sprite = on ? buttonHoverSprite : buttonSprite;
        };
    }

    // ---------- การกระทำ ----------
    void OnStart()
    {
        GameSession.Clear();
        LoadScene(startScene);
    }

    static void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        // Esc ปิดหน้า Settings (บันทึกค่าด้วย)
        if (optionsOpen && settings != null && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            settings.Close();
    }
}