using UnityEngine;
using UnityEngine.UI;

// สถานะโมดูลหนึ่งตัวบน HUD: dim เปิด/ปิด + ไอคอนความเสียหาย/กำลังซ่อม
public class ModuleStatusIndicator : MonoBehaviour
{
    [SerializeField] ModuleSystem modules;
    [SerializeField] ModuleData module;           // ว่าง = ไม่แสดงอะไร (เช่นแถว HP)
    [SerializeField] ModuleStatusStyle style;
    [SerializeField] Weapon weapon;

    [Header("Dim (เปิด/ปิดเท่านั้น ความสว่างตั้งที่ตัวรูป)")]
    [SerializeField] GameObject dim;              // ว่างได้
    [SerializeField] ModuleDamage dimFromTier = ModuleDamage.Medium;
    [SerializeField] bool dimWhileRepairing = true;

    [Header("Icon")]
    [SerializeField] Image icon;                  // Pivot ต้องเป็น (0.5, 0.5) ไม่งั้นหมุนไม่กลางตัว

    float angle;

    public bool DimSuppressed { get; set; }   // ผู้อื่นสั่งปิด dim ชั่วคราว (เช่นช่องอาวุธที่อยู่ด้านหลัง)
    public bool WantsDim { get; private set; } // โมดูลอยู่ในสภาพที่ควรมืดหรือไม่

    void Update()
    {
        ModuleData m = module != null ? module : (weapon != null ? weapon.Module : null);
        bool valid = m != null && modules != null;
        ModuleDamage tier = valid ? modules.Tier(m) : ModuleDamage.None;
        bool repairing = valid && modules.Repairing == m;

        WantsDim = (repairing && dimWhileRepairing) || (tier != ModuleDamage.None && tier >= dimFromTier);
        bool dimOn = WantsDim && !DimSuppressed;
        if (dim != null && dim.activeSelf != dimOn) dim.SetActive(dimOn);

        RefreshIcon(tier, repairing);
    }

    void RefreshIcon(ModuleDamage tier, bool repairing)
    {
        if (icon == null || style == null) return;

        Sprite sp = repairing ? style.repairIcon : style.IconFor(tier);
        bool show = sp != null && (repairing || tier != ModuleDamage.None);
        if (icon.enabled != show) icon.enabled = show;
        if (!show)
        {
            angle = 0f;
            return;
        }

        Color c = repairing ? UiThemeSource.Role(ThemeRole.InkBright) : UiThemeSource.TierColor(tier);
        if (!repairing)
        {
            float speed = style.blinkSpeed[Mathf.Min((int)tier, style.blinkSpeed.Length - 1)];
            if (speed > 0f)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f);
                c.a = Mathf.Lerp(style.blinkMinAlpha, 1f, wave);
            }
        }
        icon.color = c;
        icon.sprite = sp;

        angle = repairing ? angle - style.spinDegPerSec * Time.unscaledDeltaTime : 0f;
        icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}