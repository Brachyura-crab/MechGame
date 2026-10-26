using TMPro;
using UnityEngine;
using UnityEngine.UI;

// เกจอัลติ: ชาร์จ = เพิ่มตาม % / พร้อมใช้ = เต็มและกะพริบ / ร่าย = ค้างเต็ม / ทำงาน = ลดตามเวลาที่เหลือ
public class UltimateGaugeHud : MonoBehaviour
{
    [SerializeField] UltimateGauge gauge;
    [SerializeField] Image fill;         // Image Type = Filled (Horizontal หรือ Radial ก็ได้)
    [SerializeField] TMP_Text text;
    [SerializeField] Image readyGlow;    // ว่างได้ ไฟที่กะพริบตอนพร้อมใช้

    [Header("เคลื่อนไหว")]
    [SerializeField] float fillSpeed = 2f;                        // ความเร็วหลอดวิ่งตามค่าจริงตอนชาร์จ (เต็มหลอดต่อวินาที)
    [SerializeField] float readyPulseHz = 1.5f;                   // รอบต่อวินาทีของการกะพริบตอนพร้อมใช้
    [SerializeField, Range(0f, 1f)] float readyPulseMinAlpha = 0.5f;
    [SerializeField] bool invertFill = true;   // true = เริ่ม 100% แล้วลดเป็น 0 เมื่อพร้อมใช้ / false = แบบเดิม (0 → 100%)

    [Header("ข้อความ")]
    [SerializeField] bool showPercent = true;
    [SerializeField] string readyString = "READY";
    [SerializeField] string castingString = "...";

    enum State { Charging, Ready, Casting, Active }

    float fillShown;
    int lastKey = -1;
    bool pulsing;
    Color fillBase, textBase;   // สีที่ตั้งไว้ จำไว้ตอนเริ่มกะพริบเพื่อคืนค่าตอนหยุด

    void Update()
    {
        if (gauge == null) return;

        State st = gauge.IsActive ? State.Active
                 : gauge.IsCasting ? State.Casting
                 : gauge.IsFull ? State.Ready
                 : State.Charging;

        // ร่าย/พร้อมใช้ = เต็ม, ทำงาน = ลดตามเวลาที่เหลือ, ชาร์จ = ตามสัดส่วน
        float target;
        switch (st)
        {
            case State.Active: target = gauge.Duration > 0f ? gauge.ActiveRemaining / gauge.Duration : 0f; break;
            case State.Casting:
            case State.Ready: target = 1f; break;
            default: target = gauge.Ratio; break;
        }

        if (st == State.Active) fillShown = target;   // ลดตามเวลาจริง ไม่หน่วง
        else fillShown = Mathf.MoveTowards(fillShown, target, fillSpeed * Time.unscaledDeltaTime);

        // กะพริบตอนพร้อมใช้ (ปรับเฉพาะความโปร่งใส สีใช้ตามที่ตั้งไว้)
        float pulse = 1f;
        if (st == State.Ready)
            pulse = Mathf.Lerp(readyPulseMinAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * readyPulseHz * Mathf.PI * 2f));

        if (fill != null) fill.fillAmount = invertFill ? 1f - fillShown : fillShown;
        ApplyPulse(st == State.Ready, pulse);

        if (readyGlow != null)
        {
            readyGlow.enabled = st == State.Ready;
            if (st == State.Ready)
            {
                Color g = readyGlow.color;
                g.a = pulse;
                readyGlow.color = g;
            }
        }

        UpdateText(st);
    }

    // กะพริบเฉพาะ alpha จากสีที่ตั้งไว้ และคืนสีเดิมเมื่อหยุด (ไม่เขียนสีทับตอนไม่ได้กะพริบ จึงไม่ชนกับสคริปต์ธีม)
    void ApplyPulse(bool on, float pulse)
    {
        if (on)
        {
            if (!pulsing)
            {
                pulsing = true;
                if (fill != null) fillBase = fill.color;
                if (text != null) textBase = text.color;
            }
            if (fill != null)
            {
                Color c = fillBase;
                c.a *= pulse;
                fill.color = c;
            }
            if (text != null)
            {
                Color c = textBase;
                c.a *= pulse;
                text.color = c;
            }
        }
        else if (pulsing)
        {
            pulsing = false;
            if (fill != null) fill.color = fillBase;
            if (text != null) text.color = textBase;
        }
    }

    // สร้างข้อความใหม่เมื่อค่าที่แสดงเปลี่ยนเท่านั้น (ไม่สร้างขยะทุกเฟรม)
    void UpdateText(State st)
    {
        if (text == null) return;

        int value;
        switch (st)
        {
            case State.Charging: value = Mathf.FloorToInt(gauge.Ratio * 100f); break;
            case State.Active: value = Mathf.CeilToInt(gauge.ActiveRemaining * 10f); break;
            default: value = 0; break;
        }
        int key = (int)st * 100000 + value;
        if (key == lastKey) return;
        lastKey = key;

        switch (st)
        {
            case State.Charging: text.text = showPercent ? value + "%" : ""; break;
            case State.Ready: text.text = readyString; break;
            case State.Casting: text.text = castingString; break;
            case State.Active: text.text = (value / 10f).ToString("0.0") + "s"; break;
        }
    }
}