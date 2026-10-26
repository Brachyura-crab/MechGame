using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/Attack Data", fileName = "Attack_")]
public class AttackData : ScriptableObject
{
    public float damage = 5f;                 // ดาเมจฐาน (คูณ DamageMultiplier จาก Stat อีกที)
    public float range = 1.5f;                // ต้องมากกว่ารัศมี collider ผู้เล่น + ศัตรูรวมกัน
    public float cooldown = 1.2f;             // วินาที (หารด้วย FireRate จาก Stat)
    public DamageType damageType = DamageType.Kinetic;
}