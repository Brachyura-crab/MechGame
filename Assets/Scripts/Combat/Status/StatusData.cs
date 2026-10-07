using System;
using UnityEngine;

[Flags]
public enum ControlFlags { None = 0, Move = 1, Attack = 2, Skills = 4, ShieldOff = 8 }

public enum StackRule { Refresh, Stack }

[Serializable]
public struct StatusModifierDef
{
    public StatId stat;
    public ModType type;
    public float value;
}

[CreateAssetMenu(menuName = "Mech/StatusData")]
public class StatusData : ScriptableObject
{
    [Header("Info")]
    public string id;
    public string displayName;
    public Sprite icon;

    [Header("Duration / Stacking")]
    public float duration = 1f;
    public StackRule stackRule = StackRule.Refresh;   // Refresh = ติดซ้ำแค่ต่อเวลา, Stack = ซ้อนหลายชั้น
    public int maxStacks = 1;                         // ใช้เมื่อเป็น Stack

    [Header("Effects")]
    public bool affectedByControlResist = false;      // ติ๊กสำหรับสถานะควบคุม (Stun ฯลฯ) ให้ลดตาม tier
    public ControlFlags blocks = ControlFlags.None;   // สิ่งที่สถานะนี้ห้ามทำ
    public StatusModifierDef[] modifiers;             // ตัวปรับสถิติที่ใส่ระหว่างติดสถานะ
}