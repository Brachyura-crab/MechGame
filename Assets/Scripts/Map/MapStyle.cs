using UnityEngine;

// ค่าหน้าตาของแผนที่ทั้งหมดรวมที่เดียว (minimap, และคำอธิบาย/แผนที่ใหญ่ภายหลังจะอ่านค่าชุดเดียวกัน)
[CreateAssetMenu(menuName = "Map/MapStyle", fileName = "MapStyle")]
public class MapStyle : ScriptableObject
{
    [Header("Layout (หน่วย canvas)")]
    public float diameter = 200f;
    public float margin = 24f;                 // ห่างขอบซ้าย/บนของจอ

    [Header("View")]
    public float visibleRadius = 45f;          // รัศมีโลกที่เห็นในวง
    public float edgeMargin = 15f;             // swarm/leader เกินรัศมีไม่เกินนี้ = เกาะขอบ ไกลกว่านี้ = หาย
    public float edgePad = 2f;                 // icon ที่เกาะขอบอยู่ห่างขอบวงเท่านี้
    public float gridSpacing = 10f;            // ระยะช่อง grid (หน่วยโลก)

    [Header("Icon size (px)")]
    public float swarmSize = 6f;
    public float leaderSize = 10f;
    public float gateSize = 14f;
    public float extractionSize = 8f;
    public float playerSize = 14f;

    [Header("Icon colors")]
    public Color swarmColor = new Color(1f, 0.2f, 0.2f, 1f);
    public Color leaderColor = new Color(1f, 0.2f, 0.2f, 1f);
    public Color gateColor = new Color(0.72f, 0.3f, 1f, 1f);
    public Color extractionColor = Color.white;
    public Color playerColor = new Color(0.3f, 1f, 0.4f, 1f);

    [Header("Lines")]
    public float lineThickness = 1.5f;
    public float borderThickness = 2f;
    public Color bgColor = new Color(0f, 0f, 0f, 0.6f);
    public Color gridColor = new Color(0.25f, 1f, 0.35f, 0.18f);
    public Color borderColor = new Color(0.3f, 1f, 0.4f, 0.9f);
    public Color warnColor = new Color(1f, 0.25f, 0.25f, 1f);      // วงเตือนออกนอกพื้นที่
    public Color fieldColor = new Color(0.72f, 0.3f, 1f, 1f);      // วงสนามอันตรายรอบ gate
    public Color activeColor = new Color(0.3f, 0.65f, 1f, 1f);     // วงที่ฆ่าศัตรูแล้ว energy gate ลด

    [Header("Misc")]
    public float arrowMinSpeed = 0.5f;         // ช้ากว่านี้ลูกศรผู้เล่นคงทิศเดิม
    public float pulseSpeed = 8f;              // gate Charging / จุดส่งตอน Extract กะพริบ

    void OnValidate()
    {
        diameter = Mathf.Max(40f, diameter);
        visibleRadius = Mathf.Max(5f, visibleRadius);
        edgeMargin = Mathf.Max(0f, edgeMargin);
        lineThickness = Mathf.Max(0.5f, lineThickness);
    }
}