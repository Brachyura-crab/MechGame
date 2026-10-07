using UnityEngine;

[CreateAssetMenu(menuName = "Mech/TierData")]
public class TierData : ScriptableObject
{
    public string displayName;
    [Range(0f, 1f)] public float controlResist;    // ลดระยะเวลา Stun/Root ฯลฯ (0.3 = ลด 30%)
    [Range(0f, 1f)] public float knockbackResist;  // ลดแรงผลัก
    public float ultChargeOnKill = 1f;   // เกจอัลติที่ผู้เล่นได้เมื่อฆ่าเป้าใน tier นี้ (เกจเต็ม = 100)
}