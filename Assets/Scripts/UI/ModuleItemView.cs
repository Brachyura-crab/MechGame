using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ModuleItemView : MonoBehaviour
{
    [SerializeField] Image frame;              // ชั้นนอก ใช้เป็นเส้นกรอบและรับคลิก
    [SerializeField] TMP_Text nameText;
    [SerializeField] Image icon;
    [SerializeField] Image progressFill;

    [Header("ไอคอนสถานะ (ว่างได้ จะใช้ข้อความแทน)")]
    [SerializeField] Sprite normalIcon;
    [SerializeField] Sprite lightIcon;
    [SerializeField] Sprite mediumIcon;
    [SerializeField] Sprite heavyIcon;
    [SerializeField] Sprite repairIcon;

    [Header("Animation")]
    [SerializeField] float repairSpinSpeed = 180f;                     // องศาต่อวินาที (ค่าลบ = หมุนทวนเข็ม)
    [SerializeField] float[] blinkSpeed = { 0f, 1f, 2f, 3f };          // รอบต่อวินาทีตามระดับ: None, Light, Medium, Heavy
    [SerializeField, Range(0f, 1f)] float blinkMinAlpha = 0.25f;       // ตอนจางสุดยังเหลือความทึบเท่านี้

    float spinAngle;

    ModuleSystem system;
    ModuleData module;

    public void Bind(ModuleSystem sys, ModuleData m)
    {
        system = sys;
        module = m;
        nameText.text = m.displayName;
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (system.Repairing == module) system.CancelRepair();
        else if (system.Tier(module) != ModuleDamage.None) system.StartRepair(module);
    }

    void Update()
    {
        if (system == null) return;

        ModuleDamage tier = system.Tier(module);
        bool repairing = system.Repairing == module;

        Color c = repairing ? UiThemeSource.Role(ThemeRole.InkBright) : UiThemeSource.TierColor(tier);
        frame.color = c;
        nameText.color = c;
        // กะพริบเมื่อเสียหาย (เร็วขึ้นตามระดับ) / หมุนเมื่อกำลังซ่อม
        Color ic = c;
        if (!repairing && tier != ModuleDamage.None)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * blinkSpeed[(int)tier] * Mathf.PI * 2f);
            ic.a = Mathf.Lerp(blinkMinAlpha, 1f, wave);
        }
        icon.color = ic;

        if (repairing) spinAngle -= repairSpinSpeed * Time.unscaledDeltaTime;
        else spinAngle = 0f;                                   // เริ่มหมุนจากมุม 0 ทุกครั้งที่ซ่อมใหม่
        icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spinAngle);

        Sprite s = repairing ? repairIcon : IconFor(tier);
        icon.sprite = s;
        icon.enabled = s != null;
        
        progressFill.fillAmount = repairing ? system.RepairProgress : 0f;
    }

    Sprite IconFor(ModuleDamage t)
    {
        switch (t)
        {
            case ModuleDamage.Light:  return lightIcon;
            case ModuleDamage.Medium: return mediumIcon;
            case ModuleDamage.Heavy:  return heavyIcon;
            default:                  return normalIcon;
        }
    }
}