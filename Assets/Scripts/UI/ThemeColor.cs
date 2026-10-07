using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ThemeColor : MonoBehaviour
{
    [SerializeField] ThemeRole role = ThemeRole.InkMid;
    [SerializeField] bool keepAlpha = true;

    Graphic graphic;   // Image และ TMP_Text ต่างก็เป็น Graphic

    void OnEnable()
    {
        UiThemeSource.Changed += Apply;
        Apply();
    }

    void OnDisable() => UiThemeSource.Changed -= Apply;

    void OnValidate() => Apply();

    void Apply()
    {
        if (graphic == null) graphic = GetComponent<Graphic>();
        if (graphic == null) return;

        UiThemeSource src = UiThemeSource.Current;
        if (src == null || src.Theme == null) return;

        Color c = src.Theme.Get(role);
        if (keepAlpha) c.a = graphic.color.a;
        graphic.color = c;
    }
}