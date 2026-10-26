using UnityEngine;

// ค่าที่กระสุนต้องใช้จริง ไม่ผูกกับ WeaponData (ผู้เล่นสร้างจาก WeaponData ศัตรูกรอกเองใน AttackData)
[System.Serializable]
public struct BulletParams
{
    public float damage;
    public float critChance;
    public float speed;
    public float lifetime;
    public DamageType type;
    public int maxHits;                  // -1 = ทะลุไม่จำกัด
    public GameObject impactEffect;
    public float impactLifetime;
    public int impactSortingOrder;
}