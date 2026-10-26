using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// รายการปุ่มและหน้าที่ (HUD มุมขวาล่าง) ตัวอักษรสีเทา เรียงบนลงล่าง
// กดปุ่มแล้วตัวอักษรของแถวนั้นเป็นสีขาว  ปุ่มที่มีคูลดาวน์ (Dash, สกิล, อัลติ) สีเทากลับมาจากขวาไปซ้ายตามเวลาคูลดาวน์
// ปุ่มที่ไม่มีคูลดาวน์ สลับขาว/เทาตามที่กดเท่านั้น
// วางบน object ว่างใต้ Canvas ของ HUD (โค้ดตั้ง anchor มุมขวาล่างให้เอง)
public class KeyHintBar : MonoBehaviour
{
    public enum Kind { Press, Dash, Boost, Skill, Ultimate }
    public enum Source { Keyboard, MouseLeft, MouseRight }

    [Serializable]
    public class Entry
    {
        public string key = "";            // ข้อความปุ่มที่โชว์ เช่น "W", "SPACE (TAP)"
        public string text = "";           // หน้าที่ เช่น "MOVE UP"
        public Kind kind = Kind.Press;
        public Source source = Source.Keyboard;
        public Key keyboardKey = Key.None; // ใช้ตรวจว่ากดอยู่ไหม (Kind.Press)
        public int skillIndex;             // Kind.Skill: ช่องใน MechSkills (ชื่อสกิลจะต่อท้ายให้เอง)
    }

    [SerializeField] Entry[] entries = DefaultEntries();

    [Header("Layout (หน่วย canvas)")]
    [SerializeField] Vector2 margin = new Vector2(24f, 24f);
    [SerializeField] float rowWidth = 270f;
    [SerializeField] float rowHeight = 20f;
    [SerializeField] float keyWidth = 120f;     // ความกว้างคอลัมน์ปุ่ม (ชิดขวา)
    [SerializeField] float fontSize = 14f;

    [Header("สี")]
    [SerializeField] Color idleColor = new Color(0.62f, 0.64f, 0.66f, 0.8f);   // เทา
    [SerializeField] Color litColor = Color.white;                             // สีตอนกด / ตอนคูลดาวน์

    [Header("Fonts (ว่างได้)")]
    [SerializeField] TMP_FontAsset headFont;
    [SerializeField] TMP_FontAsset monoFont;

    class Row
    {
        public Entry e; public RectTransform root; public RectTransform clip;
        public TextMeshProUGUI key, text;          // ชั้นสีเทา
        public TextMeshProUGUI keyLit, textLit;    // ชั้นสีขาว (ถูกตัดด้วย clip)
        public float total; public string lastText; public bool visible = true;
    }

    UiKit kit;
    Row[] rows;
    PlayerMove move; MechSkills skills; UltimateGauge ult;
    bool built, bound;

    static Entry[] DefaultEntries() => new[]
    {
        new Entry { key = "W",           text = "MOVE UP",          keyboardKey = Key.W },
        new Entry { key = "A",           text = "MOVE LEFT",        keyboardKey = Key.A },
        new Entry { key = "S",           text = "MOVE DOWN",        keyboardKey = Key.S },
        new Entry { key = "D",           text = "MOVE RIGHT",       keyboardKey = Key.D },
        new Entry { key = "SPACE (TAP)", text = "DASH",             kind = Kind.Dash },
        new Entry { key = "SPACE (HOLD)",text = "BOOST",            kind = Kind.Boost },
        new Entry { key = "LMB",         text = "FIRE WEAPON 1",    source = Source.MouseLeft },
        new Entry { key = "RMB",         text = "FIRE WEAPON 2",    source = Source.MouseRight },
        new Entry { key = "Q",           text = "SKILL 1",          kind = Kind.Skill, skillIndex = 0 },
        new Entry { key = "E",           text = "SKILL 2",          kind = Kind.Skill, skillIndex = 1 },
        new Entry { key = "R",           text = "ULTIMATE",         kind = Kind.Ultimate },
        new Entry { key = "TAB",         text = "STATUS PANEL",     keyboardKey = Key.Tab },
        new Entry { key = "M",           text = "MAP",              keyboardKey = Key.M },
        new Entry { key = "ESC",         text = "PAUSE",            keyboardKey = Key.Escape },
    };

    void OnEnable()
    {
        if (!built) { kit = new UiKit { headFont = headFont, monoFont = monoFont }; built = true; Build(); }
    }

    void Build()
    {
        var rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-margin.x, margin.y);
        rt.sizeDelta = new Vector2(rowWidth, entries.Length * rowHeight);

        rows = new Row[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            var r = new Row { e = entries[i] };
            r.root = UiKit.Rect("Row" + i, rt);
            UiKit.TopLeft(r.root, 0, i * rowHeight, rowWidth, rowHeight);

            r.key = kit.Label(r.root, "Key", r.e.key, fontSize, ThemeRole.InkDim, false, TextAlignmentOptions.MidlineRight);
            UiKit.TopLeft(r.key.rectTransform, 0, 0, keyWidth, rowHeight);
            r.text = kit.Label(r.root, "Text", r.e.text, fontSize, ThemeRole.InkDim, true, TextAlignmentOptions.MidlineLeft);
            UiKit.TopLeft(r.text.rectTransform, keyWidth + 12, 0, rowWidth - keyWidth - 12, rowHeight);

            // ชั้นสีขาว: ตัดด้วยกรอบที่ยึดซ้าย ความกว้างลดลงจากขวาไปซ้าย
            r.clip = UiKit.Rect("Clip", r.root);
            r.clip.anchorMin = new Vector2(0, 0); r.clip.anchorMax = new Vector2(0, 1); r.clip.pivot = new Vector2(0, 0.5f);
            r.clip.anchoredPosition = Vector2.zero; r.clip.sizeDelta = new Vector2(0, 0);
            r.clip.gameObject.AddComponent<RectMask2D>();
            r.keyLit = kit.Label(r.clip, "KeyLit", r.e.key, fontSize, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
            UiKit.TopLeft(r.keyLit.rectTransform, 0, 0, keyWidth, rowHeight);
            r.textLit = kit.Label(r.clip, "TextLit", r.e.text, fontSize, ThemeRole.InkBright, true, TextAlignmentOptions.MidlineLeft);
            UiKit.TopLeft(r.textLit.rectTransform, keyWidth + 12, 0, rowWidth - keyWidth - 12, rowHeight);
            rows[i] = r;
        }
    }

    static T Find<T>(PlayerTag tag) where T : Component
    {
        var c = tag.GetComponentInChildren<T>(true);
        if (c == null) c = tag.GetComponentInParent<T>();
        return c;
    }

    bool Bind()
    {
        if (bound) return true;
        var tag = PlayerTag.Instance;
        if (tag == null) return false;
        move = Find<PlayerMove>(tag);
        skills = Find<MechSkills>(tag);
        ult = Find<UltimateGauge>(tag);

        // ซ่อนแถวที่ไม่มีของ (ช่องสกิลว่าง) แล้วจัดเรียงใหม่ ชิดล่าง
        int k = 0;
        foreach (var r in rows)
        {
            switch (r.e.kind)
            {
                case Kind.Dash: case Kind.Boost: r.visible = move != null; break;
                case Kind.Skill: r.visible = skills != null && skills.GetSkill(r.e.skillIndex) != null; break;
                case Kind.Ultimate: r.visible = ult != null; break;
                default: r.visible = true; break;
            }
            r.root.gameObject.SetActive(r.visible);
            if (r.visible) UiKit.TopLeft(r.root, 0, (k++) * rowHeight, rowWidth, rowHeight);
        }
        ((RectTransform)transform).sizeDelta = new Vector2(rowWidth, k * rowHeight);
        bound = true;
        return true;
    }

    bool Pressed(Entry e)
    {
        switch (e.source)
        {
            case Source.MouseLeft: return Mouse.current != null && Mouse.current.leftButton.isPressed;
            case Source.MouseRight: return Mouse.current != null && Mouse.current.rightButton.isPressed;
            default:
                return e.keyboardKey != Key.None && Keyboard.current != null && Keyboard.current[e.keyboardKey].isPressed;
        }
    }

    void Update()
    {
        if (!built || !Bind()) return;

        foreach (var r in rows)
        {
            if (!r.visible) continue;
            float ratio = 0f;       // ส่วนที่ยังเป็นสีขาว (1 = ขาวทั้งแถว 0 = เทา)

            switch (r.e.kind)
            {
                case Kind.Dash:
                    ratio = move.DashHeld && !move.IsBoosting ? 1f : move.DashCooldownRatio;
                    break;
                case Kind.Boost:
                    ratio = move.IsBoosting ? 1f : 0f;
                    break;
                case Kind.Skill:
                    ratio = skills.CooldownRatio(r.e.skillIndex);
                    break;
                case Kind.Ultimate:
                    if (ult.IsActive) ratio = ult.Duration > 0f ? ult.ActiveRemaining / ult.Duration : 0f;
                    else ratio = ult.IsFull ? 1f : 0f;     // พร้อมใช้ = ขาวค้าง
                    break;
                default:
                    ratio = Pressed(r.e) ? 1f : 0f;
                    break;
            }
            ratio = Mathf.Clamp01(ratio);

            // ข้อความที่เปลี่ยนตามเกม (ชื่อสกิล, เปอร์เซ็นต์เกจอัลติ)
            string txt = r.e.text;
            if (r.e.kind == Kind.Skill) { var sk = skills.GetSkill(r.e.skillIndex); if (sk != null) txt = sk.displayName.ToUpper(); }
            else if (r.e.kind == Kind.Ultimate && !ult.IsActive && !ult.IsFull)
                txt = r.e.text + "  " + Mathf.FloorToInt(ult.Ratio * 100f) + "%";
            if (txt != r.lastText)
            {
                r.lastText = txt;
                r.text.text = txt; r.textLit.text = txt;
                r.total = keyWidth + 12f + r.text.GetPreferredValues(txt).x;   // ความยาวจริงของข้อความ
            }

            r.key.color = idleColor; r.text.color = idleColor;
            r.keyLit.color = litColor; r.textLit.color = litColor;
            r.clip.sizeDelta = new Vector2(r.total * ratio + (ratio >= 1f ? rowWidth : 0f), 0f);
        }
    }
}