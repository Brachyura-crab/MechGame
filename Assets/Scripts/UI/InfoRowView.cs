using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoRowView : MonoBehaviour
{
    [SerializeField] TMP_Text labelText;
    [SerializeField] TMP_Text valueText;
    [SerializeField] Image icon;        // ไม่บังคับ

    public void Set(string label, string value)
    {
        labelText.text = label;
        valueText.text = value;
    }

    public void SetValueColor(Color c) => valueText.color = c;

    public void SetIcon(Sprite s)
    {
        if (icon == null) return;
        icon.sprite = s;
        icon.gameObject.SetActive(s != null);
    }
}