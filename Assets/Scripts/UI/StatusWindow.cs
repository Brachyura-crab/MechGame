using UnityEngine;
using UnityEngine.InputSystem;

public class StatusWindow : MonoBehaviour
{
    [SerializeField] InputActionReference toggleAction;       // ปุ่มเปิด/ปิด (Tab)
    [SerializeField] GameObject panel;                        // ตัวหน้าต่าง (ปิดไว้ตอนเริ่ม)
    [SerializeField] RectTransform blockArea;                 // พื้นที่ที่เมาส์อยู่แล้วห้ามยิง (ปกติ = panel)
    [SerializeField] Camera uiCamera;                         // Canvas แบบ Screen Space - Camera ใส่กล้อง, Overlay ปล่อยว่าง
    [SerializeField] InputActionReference[] attackActions;    // Attack และ Attack2

    [Header("Animation")]
    [SerializeField] RectTransform animTarget;   // ตัวที่หมุน (ปกติ = Panel)
    [SerializeField] CanvasGroup group;
    [SerializeField] float openDuration = 0.35f;
    [SerializeField] float closeDuration = 0.25f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] float startAngleY = 90f;    // ถ้าเห็นหมุนผิดทาง สลับเป็น -90
    [SerializeField] Vector2 slideOffset = new Vector2(120f, 0f);   // เริ่มเลื่อนเข้ามาจากขวา (หน่วย Canvas)
    [SerializeField] float startScale = 0.9f;

    bool wantOpen;
    float progress;
    Vector2 basePos;

    bool hoverBlocking;

    void OnEnable() => toggleAction.action.Enable();
    void OnDisable() => SetHover(false);
    public bool IsOpen => wantOpen;

    void Awake() => basePos = animTarget.anchoredPosition;
    void Start() => Apply(0f);

    void Update()
    {
        RunManager run = RunManager.Instance;
        bool playing = run == null || run.State == RunManager.RunState.Playing;

        if (!playing) wantOpen = false;                              // ตาย/จบด่าน = ปิด
        else if (toggleAction.action.WasPressedThisFrame()) wantOpen = !wantOpen;

        float dur = Mathf.Max(0.01f, wantOpen ? openDuration : closeDuration);
        progress = Mathf.MoveTowards(progress, wantOpen ? 1f : 0f, Time.unscaledDeltaTime / dur);
        Apply(progress);

        bool hover = panel.activeSelf && Mouse.current != null
            && RectTransformUtility.RectangleContainsScreenPoint(blockArea, Mouse.current.position.ReadValue(), uiCamera);
        SetHover(hover);
    }

    void Apply(float p)
    {
        bool visible = p > 0f;
        if (panel.activeSelf != visible) panel.SetActive(visible);
        if (!visible) return;

        float e = ease.Evaluate(p);
        animTarget.localRotation = Quaternion.Euler(0f, Mathf.Lerp(startAngleY, 0f, e), 0f);
        animTarget.anchoredPosition = basePos + slideOffset * (1f - e);
        animTarget.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, e);
        group.alpha = e;
        group.interactable = p >= 1f;       // กดปุ่มได้เมื่อเปิดเต็มแล้ว
        group.blocksRaycasts = p >= 1f;
    }

    void SetHover(bool hover)
    {
        if (hover == hoverBlocking) return;
        hoverBlocking = hover;
        foreach (InputActionReference a in attackActions)
        {
            if (a == null) continue;
            if (hover) a.action.Disable();
            else a.action.Enable();
        }
    }
}