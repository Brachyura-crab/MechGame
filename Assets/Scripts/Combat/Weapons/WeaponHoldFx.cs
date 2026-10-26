using UnityEngine;
using UnityEngine.Rendering.Universal;

// ก้อนพลังงานที่ปลายกระบอก + ปืนสั่น ระหว่างชาร์จ (ใช้กับอาวุธโหมด ReleaseFire)
public class WeaponHoldFx : MonoBehaviour
{
    [SerializeField] Weapon weapon;
    [SerializeField] GameObject orbPrefab;       // วาดขนาดเต็มที่ตอนชาร์จเต็ม

    [Header("ก้อนพลังงาน")]
    [SerializeField] float startDelay = 0.06f;   // กดค้างเกินนี้ถึงเริ่มแสดง กันกะพริบตอนกดแตะ
    [SerializeField] float minScale = 0.15f;     // ตัวคูณขนาดของ prefab ตอนเริ่มชาร์จ
    [SerializeField] float maxScale = 1f;        // ตัวคูณขนาดตอนเต็ม
    [SerializeField] float pulseAmount = 0.12f;  // ขนาดสั่นตอนชาร์จเต็ม
    [SerializeField] float pulseSpeed = 30f;

    [Header("ปืนสั่น")]
    [SerializeField] float maxShake = 0.04f;     // ระยะสั่นสูงสุด (หน่วยโลก) 0.04 ≈ 4 พิกเซลบนจอ

    MechLimbs limbs;
    GameObject orb;
    Vector3 orbBaseScale;
    Renderer[] renderers;
    int[] baseOrders;
    Light2D[] lights;
    float[] baseLightIntensity;
    float heldTimer;
    bool shown;

    void Awake()
    {
        limbs = GetComponent<MechLimbs>();
        if (orbPrefab == null) return;

        orb = Instantiate(orbPrefab, transform);
        orbBaseScale = orb.transform.localScale;

        renderers = orb.GetComponentsInChildren<Renderer>(true);
        baseOrders = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseOrders[i] = renderers[i].sortingOrder;

        lights = orb.GetComponentsInChildren<Light2D>(true);
        baseLightIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseLightIntensity[i] = lights[i].intensity;

        orb.SetActive(false);
    }

    void OnDisable()
    {
        if (orb != null) orb.SetActive(false);
        shown = false;
        if (limbs != null && weapon != null) limbs.SetShake(weapon.IsRightArm, 0f);
    }

    void LateUpdate()
    {
        if (weapon == null) return;

        bool charging = weapon.isActiveAndEnabled && weapon.IsHolding;
        heldTimer = charging ? heldTimer + Time.deltaTime : 0f;
        bool show = charging && heldTimer >= startDelay;
        float ratio = weapon.HoldRatio;

        if (limbs != null)
            limbs.SetShake(weapon.IsRightArm, show ? maxShake * Mathf.Lerp(0.25f, 1f, ratio) : 0f);

        if (orb == null) return;

        if (!show)
        {
            if (shown) { orb.SetActive(false); shown = false; }
            return;
        }

        if (!shown) { orb.SetActive(true); shown = true; }   // เปิดใหม่ = particle/animation ใน prefab เริ่มจากต้น

        // ตามปลายกระบอกที่คำนวณจากท่าแขนปัจจุบัน (รวมการสั่นและแรงกระตุก)
        Vector2 muzzle = limbs != null
            ? limbs.MuzzleWorldPosition(weapon.IsRightArm, ArmPose.Aim)
            : (Vector2)transform.position;
        orb.transform.position = new Vector3(muzzle.x, muzzle.y, orb.transform.position.z);

        int order = weapon.MuzzleSortingOrder;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = order + baseOrders[i];

        float s = Mathf.Lerp(minScale, maxScale, ratio);
        if (ratio >= 1f) s *= 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
        orb.transform.localScale = orbBaseScale * s;

        for (int i = 0; i < lights.Length; i++)
            lights[i].intensity = baseLightIntensity[i] * Mathf.Lerp(0.3f, 1f, ratio);
    }
}