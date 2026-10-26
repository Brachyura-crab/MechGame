using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD ช่องสกิล: ไอคอน, คูลดาวน์ (วงเติม + ตัวเลข), นัดที่โหลด/สำรอง, dim (กระสุนหมดหรือถูกห้ามใช้), แฟลชตอนพร้อมใช้
public class SkillSlotsHud : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public int skillIndex;                 // ช่องใน MechSkills (0 = ช่องแรก, 1 = ช่องที่สอง)
        public GameObject root;                // ช่องทั้งอัน (ซ่อนเมื่อช่องนี้ไม่มีสกิล) ห้ามเป็นตัวเดียวกับที่มีสคริปต์นี้
        public Image icon;

        [Tooltip("Image Type = Filled และใส่ Sprite: เต็มตอนกดใช้ ลดเป็น 0 เมื่อพร้อม (ว่างได้)")]
        public Image cooldownFill;
        public TMP_Text cooldownText;

        [Tooltip("Image Type = Filled และใส่ Sprite: นัดที่โหลดอยู่ ÷ แมก (ว่างได้)")]
        public Image loadFill;
        public TMP_Text loadedText;
        public TMP_Text reserveText;

        [Tooltip("เปิดเมื่อกระสุนหมดหรือสกิลถูกห้ามใช้ เปิด/ปิดอย่างเดียว (ว่างได้)")]
        public GameObject dim;
        [Tooltip("วาบสั้นๆ ตอนคูลดาวน์จบ (ว่างได้)")]
        public Image readyFlash;

        [NonSerialized] public SkillData skill;
        [NonSerialized] public int cdKey = -1, loaded = -2, reserve = -3;
        [NonSerialized] public bool cooling;
        [NonSerialized] public float flash;
        [NonSerialized] public Color flashBase;
    }

    [SerializeField] MechSkills skills;
    [SerializeField] Slot[] slots;
    [SerializeField] float flashTime = 0.3f;

    void Start()
    {
        foreach (Slot s in slots)
        {
            s.skill = skills.GetSkill(s.skillIndex);
            if (s.root != null) s.root.SetActive(s.skill != null);
            if (s.skill == null) continue;

            if (s.icon != null)
            {
                s.icon.sprite = s.skill.icon;
                s.icon.enabled = s.skill.icon != null;
            }
            if (s.readyFlash != null)
            {
                s.flashBase = s.readyFlash.color;
                s.readyFlash.enabled = false;
            }
            if (s.dim != null) s.dim.SetActive(false);
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        foreach (Slot s in slots)
        {
            if (s.skill == null) continue;
            int i = s.skillIndex;

            // คูลดาวน์
            float remaining = skills.CooldownRemaining(i);
            if (s.cooldownFill != null) s.cooldownFill.fillAmount = skills.CooldownRatio(i);
            int key = remaining > 0f ? Mathf.CeilToInt(remaining * 10f) : 0;
            if (s.cooldownText != null && key != s.cdKey)
            {
                s.cdKey = key;
                s.cooldownText.text = key > 0 ? (key / 10f).ToString("0.0") : "";
            }

            // นัด (-1 = ไม่จำกัด)
            int loaded = skills.Loaded(i);
            int reserve = skills.Reserve(i);
            bool limited = loaded >= 0;
            bool noAmmo = limited && loaded == 0 && reserve == 0;

            if (s.loadFill != null)
                s.loadFill.fillAmount = limited ? (float)loaded / Mathf.Max(1, s.skill.MagazineSize) : 1f;
            if (s.loadedText != null && loaded != s.loaded)
            {
                s.loaded = loaded;
                s.loadedText.text = limited ? loaded.ToString() : "";
            }
            if (s.reserveText != null && reserve != s.reserve)
            {
                s.reserve = reserve;
                s.reserveText.text = !limited ? "" : (reserve < 0 ? "INF" : reserve.ToString());
            }

            // dim
            if (s.dim != null)
            {
                bool on = noAmmo || skills.IsBlocked;
                if (s.dim.activeSelf != on) s.dim.SetActive(on);
            }

            // วาบตอนคูลดาวน์จบ (ไม่วาบถ้าไม่มีกระสุนให้ใช้)
            if (s.cooling && remaining <= 0f && !noAmmo) s.flash = 1f;
            s.cooling = remaining > 0f;

            if (s.readyFlash != null)
            {
                s.flash = Mathf.MoveTowards(s.flash, 0f, dt / Mathf.Max(0.01f, flashTime));
                bool show = s.flash > 0f;
                if (s.readyFlash.enabled != show) s.readyFlash.enabled = show;
                if (show)
                {
                    Color c = s.flashBase;
                    c.a = s.flashBase.a * s.flash;
                    s.readyFlash.color = c;
                }
            }
        }
    }
}