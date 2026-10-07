using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceCounterView : MonoBehaviour
{
    [SerializeField] ResourceInventory inventory;
    [SerializeField] ResourceType resource;      // ชนิดที่นับ (เช่น Res_RepairKit)
    [SerializeField] Image icon;
    [SerializeField] TMP_Text countText;
    [SerializeField] bool showMax;               // true = แสดง 4/10

    bool started;

    void OnEnable()
    {
        inventory.Changed += OnChanged;
        UiThemeSource.Changed += Refresh;
        if (started) Refresh();
    }

    void OnDisable()
    {
        inventory.Changed -= OnChanged;
        UiThemeSource.Changed -= Refresh;
    }

    void Start()
    {
        started = true;
        if (icon != null && resource.icon != null) icon.sprite = resource.icon;
        Refresh();
    }

    void OnChanged(ResourceType t, int n)
    {
        if (t == resource) Refresh();
    }

    void Refresh()
    {
        int n = inventory.Get(resource);
        countText.text = showMax ? n + "/" + resource.maxAmount : n.ToString();

        // หมด = แดง (สีสถานะ) ปกติใช้สีหมึกของธีม
        Color c = n > 0 ? UiThemeSource.Role(ThemeRole.InkMid) : UiThemeSource.Critical;
        countText.color = c;
        if (icon != null) icon.color = c;
    }
}