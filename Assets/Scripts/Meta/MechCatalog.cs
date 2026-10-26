using System;
using UnityEngine;

[Serializable]
public class MechEntry
{
    public string id = "mech01";              // ใช้เป็นคีย์ในเซฟ ห้ามซ้ำ ห้ามเปลี่ยนทีหลัง
    public MechData data;                     // ชื่อ/โค้ด/portrait/stat เดิม
    public GameObject playerPrefab;           // Player prefab ของหุ่นนี้ (รูปร่าง+สกิลต่างกัน)
    [TextArea] public string description;
    public bool unlockedByDefault = true;
    public string unlockedByMissionId;        // ว่าง = ไม่ผูกด่าน

    [Header("Bars บนหน้าเลือกหุ่น (0-1)")]
    [Range(0, 1)] public float barArmor = .5f;
    [Range(0, 1)] public float barSpeed = .5f;
    [Range(0, 1)] public float barPower = .5f;
    public string[] loadoutNames = new string[0];   // เช่น ปืน/สกิล/ULT ไว้โชว์เฉยๆ
}

[CreateAssetMenu(menuName = "Game/Mech Catalog")]
public class MechCatalog : ScriptableObject
{
    public MechEntry[] mechs;

    public MechEntry Get(string id)
    {
        if (mechs == null) return null;
        foreach (var m in mechs) if (m != null && m.id == id) return m;
        return null;
    }
}