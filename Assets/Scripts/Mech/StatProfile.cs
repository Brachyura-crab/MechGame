using UnityEngine;

// ตัวแม่ของ data ที่ใช้เติมค่าลง StatBlock (ผู้เล่น = MechData, ศัตรู = EnemyStatData)
public abstract class StatProfile : ScriptableObject
{
    public abstract void ApplyTo(StatBlock s);
}