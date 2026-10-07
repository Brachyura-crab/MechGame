using UnityEngine;

public enum FireMode { HoldAuto, ReleaseFire }

[CreateAssetMenu(menuName = "Mech/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string id;
    public string displayName;

    [Header("Fire")]
    public float damage = 10f;
    public float critChance = 0.05f;
    public float shotsPerSecond = 3f;
    public float bulletSpeed = 40f;
    public float bulletLifetime = 0.8f;
    public float spreadDegrees = 0f;
    public DamageType damageType = DamageType.Kinetic;
    public FireMode fireMode = FireMode.HoldAuto;
    public int maxHits = 1;   // จำนวนศัตรูที่กระสุนผ่านได้ (-1 = ไม่จำกัด)
    public float recoil = 0f;   // ความเร็วที่ผลักผู้ยิงถอยหลังต่อนัด (0 = ไม่มี)

    [Header("Effects")]
    public GameObject muzzleEffect;                     // prefab ประกายปลายกระบอก
    public float muzzleEffectLifetime = 0.15f;          // วินาที ให้เท่าความยาว clip
    [Range(0f, 1f)] public float screenDim = 0.5f;      // ความมืดของฉากต่อนัด (0 = ไม่มืด)
    public float cameraKick = 0f;   // ความแรงกล้องสั่นต่อนัด (หน่วยโลก) 0 = ไม่สั่น
    public float armKick = 0.1f;   // ระยะที่ปืนกระตุกถอย (หน่วยโลก) 0 = ไม่กระตุก

    [Header("Casing (ปลอกกระสุน)")]
    public GameObject casingPrefab;
    [Range(0f, 1f)] public float casingPortRatio = 0.35f;     // ตำแหน่งช่องคายปลอก: 0 = จุดหมุนแขน, 1 = ปลายกระบอก
    public Vector2 casingSpeedRange = new Vector2(2f, 3.5f);  // ความเร็วดีดออกด้านข้าง (สุ่มระหว่างสองค่า)
    public float casingLifetime = 2f;                         // วินาทีตั้งแต่ดีดจนจางหาย

    [Header("Impact (ประกายตอนกระทบ)")]
    public GameObject impactEffect;          // prefab ParticleSystem
    public float impactLifetime = 0.6f;      // วินาที ควรมากกว่าอายุสูงสุดของ particle + duration
    public int impactSortingOrder = 20;      // ต้องสูงกว่า sprite ของศัตรู

    [Header("Charge (เฉพาะ ReleaseFire)")]
    public float chargeTime = 0.8f;   // วินาทีที่ภาพชาร์จเต็ม (ภาพเท่านั้น ยังไม่กระทบดาเมจ)

    [Header("Ammo")]
    public int magazineSize = 20;
    public float reloadTime = 2f;
    public float autoReloadIdleDelay = 1f;   // หยุดยิงกี่วินาทีถึงเริ่ม reload อัตโนมัติ
    public ResourceType ammoResource;   // กระสุนสำรองที่ใช้ reload (ว่าง = ไม่จำกัด)

    [Header("Bullet")]
    public Bullet bulletPrefab;

    [Header("Camera (ตอนใช้อาวุธนี้)")]
    [Range(0f, 0.95f)] public float cameraAimLead = 0.3f;   // สัดส่วนระยะ ตัวหุ่น→เมาส์ ที่กล้องเลื่อนไป
    public float cameraMaxOffset = 4f;                       // กล้องเลื่อนไปไกลสุดกี่หน่วย (เกินครึ่งจอ = ตัวหุ่นหลุดจอได้)
}