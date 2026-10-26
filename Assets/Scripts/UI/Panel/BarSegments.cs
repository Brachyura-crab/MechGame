using UnityEngine;
using UnityEngine.UI;

/// วางเป็น Image ลูกของแถบ ทับ Fill: ขีดแบ่งช่องเท่าๆ กัน (เห็นเป็นแถบแบ่งช่อง)
[RequireComponent(typeof(RawImage))]
[ExecuteAlways]
public class BarSegments : MonoBehaviour
{
    [SerializeField, Min(1)] int segments = 20;
    [SerializeField] ThemeRole gapRole = ThemeRole.Background;

    static Texture2D tex;
    RawImage raw;

    static Texture2D GetTex()
    {
        if (tex != null) return tex;
        tex = new Texture2D(10, 1, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color32[10];
        px[0] = new Color32(255, 255, 255, 255);   // 1 ใน 10 ของแต่ละช่องเป็นรอยแบ่ง
        for (int i = 1; i < 10; i++) px[i] = new Color32(255, 255, 255, 0);
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    void OnEnable()
    {
        UiThemeSource.Changed += Apply;
        Apply();
    }

    void OnDisable() => UiThemeSource.Changed -= Apply;
    void OnValidate() => Apply();

    void Apply()
    {
        if (raw == null) raw = GetComponent<RawImage>();
        raw.raycastTarget = false;
        raw.texture = GetTex();
        raw.uvRect = new Rect(0f, 0f, segments, 1f);
        raw.color = UiThemeSource.Role(gapRole);
    }
}