using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// เคอร์เซอร์แบบ UI: crosshair + จำนวนกระสุน, reload = กากบาท + วงรอบ
public class CustomCursorHud : MonoBehaviour
{
    // PauseMenu ตั้งเป็น true เพื่อบังคับใช้เมาส์ระบบ (ซ่อน crosshair)
    public static bool ForceOsCursor;

    [Header("อ้างอิง")]
    [SerializeField] WeaponArbiter arbiter;
    [SerializeField] Weapon defaultWeapon;          // ใช้ก่อนยังไม่เคยยิง (เช่น ปืนไรเฟิล)
    [SerializeField] ModuleSystem modules;          // ModuleSystem บน Player
    [SerializeField] Canvas canvas;                 // canvas ของเคอร์เซอร์
    [SerializeField] RectTransform cursorRoot;      // ตัวแม่ที่ขยับตามเมาส์

    [Header("ภาพ")]
    [SerializeField] RectTransform crosshairRoot;   // ตัวแม่ของแท่ง 4 แท่ง (ตัวที่จะหมุน)
    [SerializeField] Image[] crosshairParts;        // แท่งทั้ง 4 (ไว้เปลี่ยนสี)
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color reloadColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] float xAngle = 45f;
    [SerializeField] float blendTime = 0.15f;       // เวลาเปลี่ยน + เป็น X
    [SerializeField] float kickDistance = 8f;       // ระยะที่แท่งถ่างออกตอนยิง (หน่วย UI)
    [SerializeField] float kickTime = 0.12f;        // เวลาหดกลับ
    [SerializeField] float xLengthMul = 1.15f;      // ยืดแท่งตอนเป็น X (1 = ไม่ยืด)
    [SerializeField] GameObject ringRoot;           // วง track (ใส่ไว้ใต้ ring fill ก็ได้)
    [SerializeField] Image ringFill;                // Image Type=Filled, Radial 360
    [SerializeField] TMP_Text ammoText;

    [Header("ตั้งค่า")]
    [SerializeField] bool confineCursor = true;
    [SerializeField] bool showOsCursorOverUi = true;

    Weapon last;
    int lastAmmo = int.MinValue;
    bool osMode = true;   // เริ่มต้นถือว่ายังเป็นเมาส์ระบบ เพื่อให้ SetOs(false) ครั้งแรกทำงานจริง
    float xBlend;   // 0 = crosshair, 1 = กากบาท
    float kick;
    Weapon kickWeapon;
    Vector2[] restPos;
    Vector2[] restSize;

    void Start()
    {
        if (confineCursor) Cursor.lockState = CursorLockMode.Confined;
        last = defaultWeapon;
        if (ringFill) ringFill.raycastTarget = false;

        restPos = new Vector2[crosshairParts.Length];
        restSize = new Vector2[crosshairParts.Length];
        for (int i = 0; i < crosshairParts.Length; i++)
        {
            restPos[i] = crosshairParts[i].rectTransform.anchoredPosition;
            restSize[i] = crosshairParts[i].rectTransform.sizeDelta;
        }
    }

    void OnDisable() { SetOs(true); }

    void Update()
    {
        // Game Over -> คืนเคอร์เซอร์ระบบ
        if (ForceOsCursor) { SetOs(true); return; }
        var rm = RunManager.Instance;
        if (last.Ammo != lastAmmo)
        {
            // กระสุนลดในอาวุธเดิม = ยิง (สลับอาวุธหรือ reload ไม่นับ)
            if (kickWeapon == last && lastAmmo != int.MinValue && last.Ammo < lastAmmo)
                kick = 1f;
            kickWeapon = last;
            lastAmmo = last.Ammo;
            if (ammoText) ammoText.text = lastAmmo.ToString();
        }

        var mouse = Mouse.current;
        if (mouse == null) return;
        Vector2 pos = mouse.position.ReadValue();

        // เมาส์อยู่บน UI ที่กดได้ -> ใช้ลูกศรระบบ
        bool overUi = showOsCursorOverUi && EventSystem.current != null
                      && EventSystem.current.IsPointerOverGameObject();
        SetOs(overUi);
        if (overUi) return;

        Move(pos);

        if (arbiter != null && arbiter.Active != null) last = arbiter.Active;
        if (last == null) return;

        bool reloading = last.IsReloading;
        bool dry = last.IsOutOfAmmo;
        ModuleDamage tier = (modules != null && last.Module != null)
            ? modules.Tier(last.Module) : ModuleDamage.None;
        bool offline = tier == ModuleDamage.Heavy;
        bool showX = reloading || dry || offline;

        xBlend = Mathf.MoveTowards(xBlend, showX ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, blendTime));
        float e = xBlend * xBlend * (3f - 2f * xBlend);   // ease ให้นุ่ม
        crosshairRoot.localRotation = Quaternion.Euler(0f, 0f, -xAngle * e);
        Color tierColor = tier == ModuleDamage.None ? normalColor : UiThemeSource.TierColor(tier);
        Color c = Color.Lerp(tierColor, offline ? tierColor : reloadColor, e);
        for (int i = 0; i < crosshairParts.Length; i++) crosshairParts[i].color = c;
        kick = Mathf.MoveTowards(kick, 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, kickTime));
        float push = kickDistance * kick * kick;
        for (int i = 0; i < crosshairParts.Length; i++)
        {
            var rt = crosshairParts[i].rectTransform;
            Vector2 size = restSize[i];
            float longSide = Mathf.Max(size.x, size.y);
            float thick = Mathf.Min(size.x, size.y);

            // ระยะจากศูนย์กลางถึงกลางแท่ง: ปกติ -> ตอนเป็น X (ปลายด้านในชนกลาง เหลื่อมกันเท่าครึ่งความหนา)
            float xDist = longSide * xLengthMul * 0.5f - thick * 0.5f;
            float dist = Mathf.Lerp(restPos[i].magnitude, xDist, e) + push;
            rt.anchoredPosition = restPos[i].normalized * dist;

            float stretch = Mathf.Lerp(1f, xLengthMul, e);
            if (size.x >= size.y) size.x *= stretch; else size.y *= stretch;
            rt.sizeDelta = size;
        }
        if (ringRoot) ringRoot.SetActive(reloading);
        if (reloading && ringFill) ringFill.fillAmount = last.ReloadRatio;

        if (last.Ammo != lastAmmo)
        {
            lastAmmo = last.Ammo;
            if (ammoText) ammoText.text = lastAmmo.ToString();
        }
    }

    void Move(Vector2 screenPos)
    {
        var parent = (RectTransform)cursorRoot.parent;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, cam, out var local))
            cursorRoot.localPosition = local;
    }

    void SetOs(bool os)
    {
        if (os == osMode && cursorRoot.gameObject.activeSelf == !os) return;
        osMode = os;
        Cursor.visible = os;
        cursorRoot.gameObject.SetActive(!os);
    }
}