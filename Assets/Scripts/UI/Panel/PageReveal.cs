using UnityEngine;
using UnityEngine.UI;

/// หน้าถูกเปิดแล้วค่อยๆ เผยจากบนลงล่าง + สว่างขึ้น (สไตล์จอ CRT)
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectMask2D))]
public class PageReveal : MonoBehaviour
{
    [SerializeField] float duration = 0.35f;
    [SerializeField, Range(0f, 1f)] float startAlpha = 0.2f;

    CanvasGroup group;
    RectMask2D mask;
    RectTransform rt;
    float t;
    bool playing;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        mask = GetComponent<RectMask2D>();
        rt = (RectTransform)transform;
    }

    void OnEnable()
    {
        if (!CrtScreenFx.GlobalEnabled || duration <= 0f) { Finish(); return; }
        t = 0f;
        playing = true;
        mask.enabled = true;
        Apply(0f);
    }

    void OnDisable() => Finish();

    void Update()
    {
        if (!playing) return;
        t += Time.unscaledDeltaTime / duration;
        if (t >= 1f) { Finish(); return; }
        Apply(1f - (1f - t) * (1f - t));   // ease-out
    }

    void Apply(float k)
    {
        group.alpha = Mathf.Lerp(startAlpha, 1f, k);
        mask.padding = new Vector4(0f, rt.rect.height * (1f - k), 0f, 0f);   // ตัดด้านล่าง เผยจากบนลงล่าง
    }

    void Finish()
    {
        playing = false;
        if (group != null) group.alpha = 1f;
        if (mask != null) { mask.padding = Vector4.zero; mask.enabled = false; }
    }
}