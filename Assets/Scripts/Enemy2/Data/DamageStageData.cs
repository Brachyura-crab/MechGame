using System;
using System.Collections.Generic;
using UnityEngine;

// ขั้นความบาดเจ็บของศัตรู: HP ต่ำกว่าเท่าไรให้ปรับ stat อะไร
[CreateAssetMenu(menuName = "Enemy/Damage Stage Data", fileName = "Stages_")]
public class DamageStageData : ScriptableObject
{
    [Serializable]
    public class Mod
    {
        public StatId stat = StatId.MaxSpeed;
        public ModType type = ModType.More;   // More: 0.8 = x0.8 | Increased: -0.2 = -20% | Flat: บวกตรงๆ
        public float value = 0.8f;
    }

    [Serializable]
    public class Stage
    {
        public string label = "Damaged";
        [Range(0f, 1f)] public float hpBelow = 0.5f;   // เข้าขั้นนี้เมื่อ HP ต่ำกว่าสัดส่วนนี้ (0.5 = 50%)
        public List<Mod> mods = new List<Mod>();
    }

    // ถ้า HP เข้าหลายขั้นพร้อมกัน จะใช้ "ขั้นที่ hpBelow ต่ำสุด" ขั้นเดียว (ค่าที่ตั้งคือค่าสุดท้าย ไม่ซ้อนกัน)
    public List<Stage> stages = new List<Stage>();
}