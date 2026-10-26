using System;
using UnityEngine;
using UnityEngine.InputSystem;

// ข้อมูลนัดที่เพิ่งยิง ส่งให้ WeaponFireFx (และใครก็ตามที่อยากฟัง) ผ่าน event Fired
public struct ShotInfo
{
    public Vector2 muzzle;       // ตำแหน่งปลายกระบอก
    public Vector2 dir;          // ทิศกระสุน (รวม spread แล้ว)
    public bool frontVisible;    // แขนอยู่ด้านที่หันเข้าหากล้อง
    public int sortingOrder;     // ลำดับ sorting ของเอฟเฟกต์ปลายกระบอก
}

[DefaultExecutionOrder(-30)]
[RequireComponent(typeof(MechAim))]
[RequireComponent(typeof(MechStats))]
public class Weapon : MonoBehaviour
{
    [SerializeField] WeaponData data;
    [SerializeField] InputActionReference fireAction;
    [SerializeField] bool debugGui = true;
    [SerializeField] Vector2 debugGuiPos = new Vector2(20f, 20f);

    [SerializeField] ModuleData module;
    [SerializeField] WeaponArbiter arbiter;
    [SerializeField] bool isRightArm;                  // false = แขนซ้าย (มุมมองหุ่น), true = แขนขวา
    [SerializeField] PlayerSpriteDirection armReady;   // เช็คว่าท่าแขนยกจนสุดแล้วหรือยัง

    MechAim aim;
    MechStats mech;
    Rigidbody2D rb;
    StatusController status;
    ResourceInventory inventory;
    ModuleSystem modules;
    AutoAim assist;
    MechLimbs limbs;

    float ModuleEff => modules != null ? modules.Efficiency(module) : 1f;

    float nextFireTime;
    float lastFireTime = -999f;
    float reloadProgress;
    float heldTime;

    public bool HasControl { get; private set; }
    public int Ammo { get; private set; }
    public bool IsReloading { get; private set; }
    public float ReloadRatio => IsReloading ? Mathf.Clamp01(reloadProgress / data.reloadTime) : 0f;

    // โหมด ReleaseFire: กดค้างรอปล่อยปุ่มเพื่อยิง (ภาพเท่านั้น ไม่กระทบดาเมจ)
    public bool IsHolding { get; private set; }
    public float HoldRatio => IsHolding && data.holdTime > 0f ? Mathf.Clamp01(heldTime / data.holdTime) : 0f;

    public bool IsRightArm => isRightArm;
    public WeaponData Data => data;
    public ModuleData Module => module;
    public float ModuleEfficiency => ModuleEff;

    // ลำดับ sorting ของเอฟเฟกต์ที่ปลายกระบอก: แขนอยู่หน้า = เหนือลำตัว, อยู่หลัง = ใต้ลำตัว (แขนอยู่ที่ torso ±10)
    public int MuzzleSortingOrder
    {
        get
        {
            int torso = limbs != null ? limbs.TorsoSortingOrder : 0;
            bool front = limbs == null || limbs.IsArmVisibleInFront(isRightArm);
            return torso + (front ? 11 : -11);
        }
    }

    bool Limited => data.ammoResource != null && inventory != null;
    public int Reserve => Limited ? inventory.Get(data.ammoResource) : -1;   // -1 = ไม่จำกัด
    bool CanReload => !Limited || inventory.Get(data.ammoResource) > 0;
    public bool IsOutOfAmmo => Ammo <= 0 && !CanReload;

    public event Action<int, int> AmmoChanged;   // ปัจจุบัน, สูงสุด
    public event Action<ShotInfo> Fired;         // ยิงออกไปแล้วหนึ่งนัด

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

    // ทิศที่กระสุนจะไป (ก่อนใส่ spread): จากปลายกระบอกจริงไปหาเมาส์ แล้วให้ auto aim ปรับถ้าล็อกอยู่
    // ใช้ร่วมกับเลเซอร์เล็ง เพื่อให้เส้นกับกระสุนไปทางเดียวกันเสมอ
    public void GetShotRay(out Vector2 muzzle, out Vector2 dir)
    {
        muzzle = limbs != null ? limbs.MuzzleWorldPosition(isRightArm, ArmPose.Aim) : aim.MuzzlePosition;

        // คำนวณจากปลายกระบอกจริง ไม่ใช่จากกลางลำตัว ป้องกันกระสุนเบี้ยว
        Vector2 toMouse = aim.AimPoint - muzzle;
        dir = toMouse.sqrMagnitude > 0.0001f ? toMouse.normalized : aim.AimDirection;

        if (assist != null && assist.TryGetAimDirection(muzzle, data.bulletSpeed, data.bulletLifetime, out Vector2 assisted))
            dir = assisted;   // อัลติ: ยิงไปหาเป้าที่ล็อกพร้อมดักยิง
    }

    void Update()
    {
        if (PauseMenu.InputBlocked) return;
        bool pressed = fireAction.action.IsPressed();
        bool justReleased = fireAction.action.WasReleasedThisFrame();
        bool isEngaged = pressed || justReleased;   // นับเฟรมที่เพิ่งปล่อยด้วย เพื่อให้ยิงได้ทันก่อนคืนสิทธิ์
        bool hasControl = arbiter == null || arbiter.RequestControl(this, isEngaged);
        HasControl = hasControl;

        bool armIsReady = armReady == null || (isRightArm ? armReady.RightArmReady : armReady.LeftArmReady);
        bool canFireNow = hasControl && armIsReady && (status == null || status.CanAttack && ModuleEff > 0f);
        if (pressed && canFireNow && modules != null) modules.NotifyUsed(module);
        bool held = canFireNow && pressed;
        bool released = canFireNow && justReleased;
        bool releaseMode = data.fireMode == FireMode.ReleaseFire;

        // ถือปุ่ม = โหมดปล่อยปุ่ม + กดค้าง + ยังมีกระสุน
        IsHolding = releaseMode && held && Ammo > 0;
        heldTime = IsHolding ? heldTime + Time.deltaTime : 0f;

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

    // ตรรกะการยิงล้วน: อัตรายิง, กระสุน, แรงถีบ, จำนวนกระสุน (ภาพและเสียงอยู่ใน WeaponFireFx)
    void Fire(float now)
    {
        float rate = data.shotsPerSecond * mech.Stats.Get(StatId.FireRate) * Mathf.Max(0.01f, ModuleEff);
        nextFireTime = now + 1f / rate;
        lastFireTime = now;

        GetShotRay(out Vector2 muzzle, out Vector2 dir);

        if (data.spreadDegrees > 0f)
            dir = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-data.spreadDegrees, data.spreadDegrees)) * dir;

        Bullet b = BulletPool.Get(data.bulletPrefab);
        b.Fire(muzzle, dir, data, mech);
        aim.NotifyShot();

        if (rb != null && data.recoil > 0f)
            rb.linearVelocity -= dir * data.recoil;   // ถีบถอยหลังสวนทิศยิง

        Ammo--;
        AmmoChanged?.Invoke(Ammo, data.magazineSize);

        Fired?.Invoke(new ShotInfo
        {
            muzzle = muzzle,
            dir = dir,
            frontVisible = limbs == null || limbs.IsArmVisibleInFront(isRightArm),
            sortingOrder = MuzzleSortingOrder
        });
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
        if (!debugGui || !DebugGate.Visible) return;
        string s = data.displayName + "  " + Ammo + " / " + data.magazineSize;
        if (Limited) s += "   Reserve " + Reserve;
        if (IsOutOfAmmo) s += "   NO AMMO";
        if (IsReloading) s += "   Reloading " + Mathf.RoundToInt(ReloadRatio * 100f) + "%";
        DebugOverlay.Label(s);
    }
}