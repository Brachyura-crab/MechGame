using UnityEngine;

// ภาพ/แสง/กล้อง ตอนยิง ฟัง event Fired จาก Weapon (ติดบน Player หนึ่งตัวต่ออาวุธหนึ่งอัน)
public class WeaponFireFx : MonoBehaviour
{
    [SerializeField] Weapon weapon;              // อาวุธที่ฟัง (ต้องลากเอง เพราะ Player มี Weapon สองตัว)
    [SerializeField] MuzzleLightFake bodyLight;  // ไฟวาบบนลำตัวตอนยิง

    MechLimbs limbs;
    Rigidbody2D rb;

    void Awake()
    {
        limbs = GetComponent<MechLimbs>();
        rb = GetComponent<Rigidbody2D>();
        if (weapon == null) Debug.LogWarning("WeaponFireFx: ยังไม่ได้ลาก Weapon", this);
    }

    void OnEnable()
    {
        if (weapon != null) weapon.Fired += OnFired;
    }

    void OnDisable()
    {
        if (weapon != null) weapon.Fired -= OnFired;
    }

    void OnFired(ShotInfo s)
    {
        WeaponData d = weapon.Data;

        if (d.armKick > 0f && limbs != null) limbs.Kick(weapon.IsRightArm, d.armKick);

        if (bodyLight != null) bodyLight.Flash(s.frontVisible);

        if (d.muzzleEffect != null)
        {
            float angle = Mathf.Atan2(s.dir.y, s.dir.x) * Mathf.Rad2Deg;
            EffectPool.Spawn(d.muzzleEffect, s.muzzle, Quaternion.Euler(0f, 0f, angle),
                             d.muzzleEffectLifetime, transform, s.sortingOrder);
        }

        if (d.screenDim > 0f && ScreenDimmer.Instance != null)
            ScreenDimmer.Instance.Pulse(d.screenDim);

        if (d.cameraKick > 0f && CameraShake.Instance != null)
            CameraShake.Instance.Kick(d.cameraKick);

        SpawnCasing(d, s);
    }

    void SpawnCasing(WeaponData d, ShotInfo s)
    {
        if (d.casingPrefab == null || limbs == null) return;

        bool right = weapon.IsRightArm;
        Vector2 pivotPos = limbs.PivotPosition(right);
        Vector2 port = pivotPos + (s.muzzle - pivotPos) * d.casingPortRatio;

        // ดีดออกด้านข้างลำกล้อง: แขนขวา = ด้านขวาของปืน, แขนซ้าย = ด้านซ้าย
        Vector2 dir = s.dir;
        Vector2 side = right ? new Vector2(dir.y, -dir.x) : new Vector2(-dir.y, dir.x);
        side = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f)) * side;

        float speed = Random.Range(d.casingSpeedRange.x, d.casingSpeedRange.y);
        Vector2 inherit = rb != null ? rb.linearVelocity * 0.5f : Vector2.zero;
        Vector2 velocity = side * speed - dir * (speed * 0.3f) + inherit;

        PooledEffect fx = EffectPool.Spawn(d.casingPrefab, port, Quaternion.identity,
                                           d.casingLifetime, null, s.sortingOrder);
        ShellCasing sc = fx.GetComponent<ShellCasing>();
        if (sc != null) sc.Launch(port, velocity, d.casingLifetime);
    }
}