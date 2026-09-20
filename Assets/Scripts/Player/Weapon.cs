using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

    float nextFireTime;
    float lastFireTime = -999f;
    float reloadProgress;

    public int Ammo { get; private set; }
    public bool IsReloading { get; private set; }
    public float ReloadRatio => IsReloading ? Mathf.Clamp01(reloadProgress / data.reloadTime) : 0f;
    public bool CanFire { get; set; } = true;   // ไว้ให้สถานะ (เช่น Stun) ปิดการยิง

    public event Action<int, int> AmmoChanged;  // ปัจจุบัน, สูงสุด

    void Awake()
    {
        aim = GetComponent<MechAim>();
        mech = GetComponent<MechStats>();
        Ammo = data.magazineSize;
    }

    // ไม่สั่ง Disable action ตอนปิด เพราะ Action Attack ถูกใช้ร่วมกับสคริปต์อื่น (หันหน้า/กล้อง)
    void OnEnable() => fireAction.action.Enable();

    void Update()
    {
        bool held = CanFire && fireAction.action.IsPressed();
        bool released = CanFire && fireAction.action.WasReleasedThisFrame();
        bool releaseMode = data.fireMode == FireMode.ReleaseFire;

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

        if (Ammo <= 0) StartReload();
        else if (Ammo < data.magazineSize && !held && now - lastFireTime >= data.autoReloadIdleDelay)
            StartReload();
    }

    void Fire(float now)
    {
        float rate = data.shotsPerSecond * mech.Stats.Get(StatId.FireRate);
        nextFireTime = now + 1f / rate;
        lastFireTime = now;

        Vector2 dir = aim.AimDirection;
        if (data.spreadDegrees > 0f)
            dir = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-data.spreadDegrees, data.spreadDegrees)) * dir;

        Bullet b = BulletPool.Get(data.bulletPrefab);
        b.Fire(aim.MuzzlePosition, dir, data, mech);

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
        Ammo = data.magazineSize;
        AmmoChanged?.Invoke(Ammo, data.magazineSize);
    }

    void OnGUI()
    {
        if (!debugGui) return;
        string s = data.displayName + "  " + Ammo + " / " + data.magazineSize;
        if (IsReloading) s += "   Reloading " + Mathf.RoundToInt(ReloadRatio * 100f) + "%";
        GUI.Label(new Rect(debugGuiPos.x, debugGuiPos.y, 400f, 30f), s);
    }
}