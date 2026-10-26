using EnemyV2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// แถบพลังงาน gate บน HUD ผู้เล่น: โชว์เฉพาะตอนอยู่ในระยะของ gate (ระยะเดียวกับที่ฆ่าศัตรูแล้วลดพลังงานได้)
// หลาย gate ซ้อนระยะกัน = โชว์ตัวที่ใกล้สุด
public class GateEnergyHud : MonoBehaviour
{
    [Header("อ้างอิง")]
    [SerializeField] CanvasGroup group;      // ตัวแม่ของแถบทั้งหมด (ใช้ fade เข้า/ออก)
    [SerializeField] Image fill;             // Image Type = Filled, Horizontal
    [SerializeField] TMP_Text text;          // ว่างได้

    [Header("สี")]
    [SerializeField] Color energyColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField] Color chargingColor = new Color(1f, 0.35f, 0.25f);
    [SerializeField] float blinkHz = 4f;     // กะพริบตอนชาร์จ (0 = ไม่กะพริบ)

    [Header("เคลื่อนไหว")]
    [SerializeField] float fadeSpeed = 6f;   // ความเร็ว fade (ต่อวินาที)
    [SerializeField] float fillSpeed = 3f;   // ความเร็วหลอดวิ่งตามค่าจริง (เต็มหลอดต่อวินาที)

    float shown;          // fillAmount ที่แสดงอยู่
    float alpha;
    Gate current;

    void Awake()
    {
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    void Update()
    {
        current = FindNearestInRange();
        float dt = Time.unscaledDeltaTime;

        alpha = Mathf.MoveTowards(alpha, current != null ? 1f : 0f, fadeSpeed * dt);
        if (group != null) group.alpha = alpha;
        if (current == null) return;   // กำลังจางหาย ไม่ต้องอัปเดตค่า

        bool charging = current.State == GateState.Charging;
        // ปกติ = พลังงานที่เหลือ / ชาร์จ = เวลาที่เหลือก่อนระเบิด (ลดลงเรื่อย ๆ)
        float target = charging ? 1f - current.ChargeRatio : current.Ratio;
        shown = charging ? target : Mathf.MoveTowards(shown, target, fillSpeed * dt);

        if (fill != null)
        {
            fill.fillAmount = shown;
            Color c = charging ? chargingColor : energyColor;
            if (charging && blinkHz > 0f)
                c.a = Mathf.Lerp(0.5f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * blinkHz * Mathf.PI * 2f));
            fill.color = c;
        }

        if (text != null)
            text.text = charging ? "DANGER" : "GATE  " + Mathf.CeilToInt(current.Ratio * 100f) + "%";
    }

    static Gate FindNearestInRange()
    {
        if (!EnemyTarget.Ensure()) return null;
        Vector2 p = EnemyTarget.Transform.position;

        Gate best = null;
        float bestSqr = float.MaxValue;
        var all = Gate.All;
        for (int i = 0; i < all.Count; i++)
        {
            Gate g = all[i];
            if (g == null || g.State == GateState.Closed || !g.PlayerInRange) continue;
            float d = ((Vector2)g.transform.position - p).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = g; }
        }
        return best;
    }
}