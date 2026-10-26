using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ฐานของหน้าเมนูที่สร้างด้วยโค้ด (1280x720 + CRT + helper วาง UI)
public abstract class MenuScreen : MonoBehaviour
{
    [Header("Fonts (ว่างได้)")]
    [SerializeField] protected TMP_FontAsset headFont;
    [SerializeField] protected TMP_FontAsset monoFont;

    protected const float W = 1280f, H = 720f;

    protected UiKit kit;
    protected RectTransform stage, dyn;

    protected virtual void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();
        kit = new UiKit { headFont = headFont, monoFont = monoFont };
        BuildCanvas();
        OnBuild();
    }

    protected abstract void OnBuild();

    protected virtual void OnEnable() => UiThemeSource.Changed += OnTheme;
    protected virtual void OnDisable() => UiThemeSource.Changed -= OnTheme;
    void OnTheme() { if (kit != null) kit.Refresh(); }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    void BuildCanvas()
    {
        var go = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(W, H);
        sc.matchWidthOrHeight = 0.5f;

        var root = (RectTransform)go.transform;

        var bg = kit.Box(root, "Background", ThemeRole.Background, 1f);
        UiKit.Stretch(bg.rectTransform);

        stage = UiKit.Rect("Stage", root);
        stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(0.5f, 0.5f);
        stage.sizeDelta = new Vector2(W, H);
        stage.anchoredPosition = Vector2.zero;

        var stageGroup = stage.gameObject.AddComponent<CanvasGroup>();
        var fx = UiKit.Rect("CrtFx", root);
        UiKit.Stretch(fx);
        var crt = fx.gameObject.AddComponent<CrtScreenFx>();
        crt.SetFlickerTarget(stageGroup);
    }

    // ล้างชั้นเนื้อหาแล้วสร้างใหม่ (header ไม่ถูกล้าง เพราะอยู่นอก dyn)
    protected void ResetDyn()
    {
        if (dyn != null) { dyn.gameObject.SetActive(false); Destroy(dyn.gameObject); }
        dyn = UiKit.Rect("Dyn", stage);
        UiKit.Stretch(dyn);
    }

    // ---------- helper ----------
    protected TextMeshProUGUI L(Transform p, string text, float x, float y, float w, float h, float size, ThemeRole role,
                                bool head = false, TextAlignmentOptions a = TextAlignmentOptions.MidlineLeft)
    {
        var t = kit.Label(p, "T", text, size, role, head, a);
        UiKit.TopLeft(t.rectTransform, x, y, w, h);
        return t;
    }

    protected Image B(Transform p, float x, float y, float w, float h, ThemeRole r, float a)
    {
        var i = kit.Box(p, "B", r, a);
        UiKit.TopLeft(i.rectTransform, x, y, w, h);
        return i;
    }

    // กรอบเส้นบางสี่เหลี่ยม (4 เส้น)
    protected void Outline(Transform p, float x, float y, float w, float h, ThemeRole r, float a)
    {
        B(p, x, y, w, 1, r, a); B(p, x, y + h - 1, w, 1, r, a);
        B(p, x, y, 1, h, r, a); B(p, x + w - 1, y, 1, h, r, a);
    }

    protected CutCornerFrame Card(Transform p, float x, float y, float w, float h,
                                  ThemeRole outline, float oa, ThemeRole fill, float fa)
    {
        var f = kit.Frame(p, "Card", outline, oa, fill, fa);
        UiKit.TopLeft(f.rectTransform, x, y, w, h);
        return f;
    }

    protected TriangleGraphic Tri(Transform p, float x, float y, float w, float h, ThemeRole r, float a, bool left)
    {
        var rt = UiKit.Rect("Tri", p);
        UiKit.TopLeft(rt, x, y, w, h);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var t = rt.gameObject.AddComponent<TriangleGraphic>();
        t.pointLeft = left; t.raycastTarget = false;
        kit.Bind(t, r, a);
        return t;
    }

    protected static void Hit(RectTransform parent, UnityAction act)
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

    // header มาตรฐาน: MECH-OS // ชื่อหน้า ---- ข้อความขวา
    protected void BuildHeader(string title, string right)
    {
        var hd = UiKit.Rect("Header", stage);
        UiKit.TopLeft(hd, 28, 20, 1224, 36);
        B(hd, 0, 13, 10, 10, ThemeRole.InkBright, 1f);
        L(hd, "MECH-OS", 26, 0, 160, 36, 22, ThemeRole.InkBright, true);
        L(hd, "// " + title, 150, 0, 260, 36, 13, ThemeRole.InkLow);
        B(hd, 400, 17, 440, 1, ThemeRole.InkLow, 0.6f);
        L(hd, right, 824, 0, 400, 36, 14, ThemeRole.InkMid, false, TextAlignmentOptions.MidlineRight);
    }

    protected static void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("MenuScreen: ไม่พบ scene '" + sceneName + "' (ยังไม่ได้สร้าง หรือยังไม่ได้ใส่ Build Settings)");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    protected static string Fmt(float v) => v.ToString("0.##");
}