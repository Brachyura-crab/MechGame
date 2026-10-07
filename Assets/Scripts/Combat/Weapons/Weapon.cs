using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-30)]

[RequireComponent(typeof(MechAim))]
[RequireComponent(typeof(MechStats))]
public class Weapon : MonoBehaviour
{
    [SerializeField] WeaponData data;
    [SerializeField] InputActionReference fireAction;
    [SerializeField] bool debugGui = true;
    [SerializeField] Vector2 debugGuiPos = new Vector2(20f, 20f);

    MechAim aim;
    MechStats mech;
    Rigidbody2D rb;
    StatusController status;
    ResourceInventory inventory;
    [SerializeField] ModuleData module;
    ModuleSystem modules;
    float ModuleEff => modules != null ? modules.Efficiency(module) : 1f;
    AutoAim assist;

    [SerializeField] WeaponArbiter arbiter;
    [SerializeField] bool isRightArm;                  // false = แขนซ้าย (มุมมองหุ่น), true = แขนขวา   
    [SerializeField] PlayerSpriteDirection armReady;   // เช็คว่าท่าแขนยกจนสุดแล้วหรือยัง
    public bool HasControl { get; private set; }

    MechLimbs limbs;

    [SerializeField] MuzzleLightFake bodyLight;

    float nextFireTime;
    float lastFireTime = -999f;
    float reloadProgress;
    float chargeTime;

    public int Ammo { get; private set; }
    public bool IsReloading { get; private set; }
    public float ReloadRatio => IsReloading ? Mathf.Clamp01(reloadProgress / data.reloadTime) : 0f;
    public bool CanFire { get; set; } = true;   // ไว้ให้สถานะ (เช่น Stun) ปิดการยิง
    public bool IsCharging { get; private set; }
    public float ChargeRatio => IsCharging && data.chargeTime > 0f ? Mathf.Clamp01(chargeTime / data.chargeTime) : 0f;
    public bool IsRightArm => isRightArm;
    public WeaponData Data => data;
    public int TorsoSortingOrder => bodyLight != null ? bodyLight.TorsoSortingOrder : 0;
    bool Limited => data.ammoResource != null && inventory != null;
    public int Reserve => Limited ? inventory.Get(data.ammoResource) : -1;   // -1 = ไม่จำกัด
    bool CanReload => !Limited || inventory.Get(data.ammoResource) > 0;
    public bool IsOutOfAmmo => Ammo <= 0 && !CanReload;

    public event Action<int, int> AmmoChanged;  // ปัจจุบัน, สูงสุด

    void Awake()
    {
        aim = GetComponent<MechAim>();
        mech = GetComponent<MechStats>();
        Ammo = data.magazineSize;
        rb = GetComponent<Rigidbody2D>();
        status = GetComponent<StatusController>();
        assist = GetComponent<AutoAim>();
        limbs = GetComponent<MechLimbs>();
        inventory = GetComponent<ResourceInventory>();
        modules = GetComponent<ModuleSystem>();
    }

    // ไม่สั่ง Disable action ตอนปิด เพราะ Action Attack ถูกใช้ร่วมกับสคริปต์อื่น (หันหน้า/กล้อง)
    void OnEnable() => fireAction.action.Enable();

    void Update()
    {
        bool pressed = fireAction.action.IsPressed();
        bool justReleased = fireAction.action.WasReleasedThisFrame();
        bool isEngaged = pressed || justReleased;   // นับเฟรมที่เพิ่งปล่อยด้วย เพื่อให้ยิงได้ทันก่อนคืนสิทธิ์
            bool hasControl = arbiter == null || arbiter.RequestControl(this, isEngaged);
        HasControl = hasControl;

        bool armIsReady = armReady == null || (isRightArm ? armReady.RightArmReady : armReady.LeftArmReady);
        bool canFireNow = CanFire && hasControl && armIsReady && (status == null || status.CanAttack && ModuleEff > 0f);
        if (pressed && canFireNow && modules != null) modules.NotifyUsed(module);
        bool held = canFireNow && pressed;
        bool released = canFireNow && justReleased;
        bool releaseMode = data.fireMode == FireMode.ReleaseFire;
        // ชาร์จ = โหมดปล่อยปุ่ม + กดค้าง + ยังมีกระสุน
        IsCharging = releaseMode && held && Ammo > 0;
        chargeTime = IsCharging ? chargeTime + Time.deltaTime : 0f;

        // โหมดกดค้าง: ยิงตอนกดอยู่ / โหมดปล่อยปุ่ม: ยิงในเฟรมที่ปล่อย
        bool wantFire = releaseMode ? released : held;
        float now = Time.time;

        if (IsReloading)
        {
            // ยังมีกระสุน + ยิงได้จริงตอนนี้ = ยกเลิก reload แล้วยิง (กระสุนหมดต้อง reload จนจบ)
            if (wantFire && Ammo > 0 && now >= nextFireTime)
            {
                IsReloading = false;
            }
            else
            {
                reloadProgress += Time.deltaTime * mech.Stats.Get(StatId.ReloadSpeed);
                if (reloadProgress >= data.reloadTime) FinishReload();
                return;
            }
        }

        if (wantFire && Ammo > 0 && now >= nextFireTime) Fire(now);

        if (Ammo <= 0) { if (CanReload) StartReload(); }
        else if (Ammo < data.magazineSize && !held && CanReload && now - lastFireTime >= data.autoReloadIdleDelay)
            StartReload();
    }

    void Fire(float now)
    {
        float rate = data.shotsPerSecond * mech.Stats.Get(StatId.FireRate) * Mathf.Max(0.01f, ModuleEff);
        nextFireTime = now + 1f / rate;
        lastFireTime = now;

        Vector2 muzzlePos = limbs != null ? limbs.MuzzleWorldPosition(isRightArm, ArmPose.Aim) : aim.MuzzlePosition;

        // ทิศคำนวณจากปลายกระบอกจริงไปหาเมาส์ ไม่ใช่จากกลางลำตัว ป้องกันกระสุนเบี้ยว
        Vector2 toMouse = aim.AimPoint - muzzlePos;
        Vector2 dir = toMouse.sqrMagnitude > 0.0001f ? toMouse.normalized : aim.AimDirection;

        if (assist != null && assist.TryGetAimDirection(muzzlePos, data.bulletSpeed, data.bulletLifetime, out Vector2 assisted))
            dir = assisted;   // อัลติ: ยิงไปหาเป้าที่ล็อกพร้อมดักยิง

        if (data.spreadDegrees > 0f)
            dir = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-data.spreadDegrees, data.spreadDegrees)) * dir;

        Bullet b = BulletPool.Get(data.bulletPrefab);
        b.Fire(muzzlePos, dir, data, mech);
        aim.NotifyShot();

        if (data.armKick > 0f && limbs != null) limbs.Kick(isRightArm, data.armKick);
                
        bool frontVisible = limbs == null || limbs.IsArmVisibleInFront(isRightArm);
        bodyLight?.Flash(frontVisible);

        if (data.muzzleEffect != null)
        {
            int torsoOrder = bodyLight != null ? bodyLight.TorsoSortingOrder : 0;
            int order = torsoOrder + (frontVisible ? 11 : -11);   // แขนอยู่ที่ torso ±10
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            EffectPool.Spawn(data.muzzleEffect, muzzlePos, Quaternion.Euler(0f, 0f, angle),
                             data.muzzleEffectLifetime, transform, order);
        }

        if (data.screenDim > 0f && ScreenDimmer.Instance != null)
            ScreenDimmer.Instance.Pulse(data.screenDim);

        if (data.cameraKick > 0f && CameraShake.Instance != null)
            CameraShake.Instance.Kick(data.cameraKick);

        if (data.casingPrefab != null && limbs != null)
        {
            Vector2 pivotPos = limbs.PivotPosition(isRightArm);
            Vector2 port = pivotPos + (muzzlePos - pivotPos) * data.casingPortRatio;

            // ดีดออกด้านข้างลำกล้อง: แขนขวา = ด้านขวาของปืน, แขนซ้าย = ด้านซ้าย
            Vector2 side = isRightArm ? new Vector2(dir.y, -dir.x) : new Vector2(-dir.y, dir.x);
            side = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-25f, 25f)) * side;

            float speed = UnityEngine.Random.Range(data.casingSpeedRange.x, data.casingSpeedRange.y);
            Vector2 inherit = rb != null ? rb.linearVelocity * 0.5f : Vector2.zero;
            Vector2 velocity = side * speed - dir * (speed * 0.3f) + inherit;

            int baseOrder = bodyLight != null ? bodyLight.TorsoSortingOrder : 0;
            int airOrder = baseOrder + (frontVisible ? 11 : -11);   // ตอนอยู่กลางอากาศ ตามแขน

            PooledEffect fx = EffectPool.Spawn(data.casingPrefab, port, Quaternion.identity,
                                               data.casingLifetime, null, airOrder);
            ShellCasing sc = fx.GetComponent<ShellCasing>();
            if (sc != null) sc.Launch(port, velocity, data.casingLifetime);
        }

        if (rb != null && data.recoil > 0f)
            rb.linearVelocity -= dir * data.recoil;   // ถีบถอยหลังสวนทิศยิง

        Ammo--;
        AmmoChanged?.Invoke(Ammo, data.magazineSize);
    }

    void StartReload()
    {
        IsReloading = true;
        reloadProgress = 0f;
    }

    void FinishReload()
    {
        IsReloading = false;
        int need = data.magazineSize - Ammo;
        int got = Limited ? inventory.Take(data.ammoResource, need) : need;
        Ammo += got;
        AmmoChanged?.Invoke(Ammo, data.magazineSize);
    }

    void OnGUI()
    {
        if (!debugGui) return;
        string s = data.displayName + "  " + Ammo + " / " + data.magazineSize;
        if (Limited) s += "   Reserve " + Reserve;
        if (IsOutOfAmmo) s += "   NO AMMO";
        if (IsReloading) s += "   Reloading " + Mathf.RoundToInt(ReloadRatio * 100f) + "%";
        GUI.Label(new Rect(debugGuiPos.x, debugGuiPos.y, 400f, 30f), s);
    }
}