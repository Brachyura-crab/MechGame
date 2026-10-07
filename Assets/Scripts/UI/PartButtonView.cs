using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PartButtonView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] BodyPartData part;
    [SerializeField] ModuleSystem system;
    [SerializeField] PartDetailView detail;
    [SerializeField] bool alphaHitTest = true;     // กดเฉพาะพื้นที่ที่มีสี (ต้องเปิด Read/Write ที่รูป)
    [SerializeField, Range(0f, 1f)] float alphaThreshold = 0.01f;   // พิกเซลที่ทึบเกินค่านี้ถึงจะกดโดน
    [SerializeField] float idleAlpha = 0.55f;      // ชิ้นที่ไม่ได้เลือก
    [SerializeField] float hoverAlpha = 0.8f;

    Image image;
    bool hover;

    void Awake()
    {
        image = GetComponent<Image>();
        image.raycastTarget = true;
        if (alphaHitTest) image.alphaHitTestMinimumThreshold = 0.1f;
        if (alphaHitTest) image.alphaHitTestMinimumThreshold = alphaThreshold;
    }

    void OnDisable() => hover = false;

    public void OnPointerClick(PointerEventData e) => detail.Select(part);
    public void OnPointerEnter(PointerEventData e) => hover = true;
    public void OnPointerExit(PointerEventData e) => hover = false;

    void Update()
    {
        Color c = UiThemeSource.TierColor(system.PartTier(part));   // สีตามระดับเสียของชิ้นนี้
        bool selected = detail.Selected == part;
        c.a = selected ? 1f : hover ? hoverAlpha : idleAlpha;       // ชิ้นที่เลือกอยู่ทึบเต็ม
        image.color = c;
    }
}