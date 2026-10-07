using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Mech/Skills/Missile Volley")]
public class MissileVolleySkill : SkillData
{
    [Header("Volley")]
    public Missile missilePrefab;
    public int pairs = 4;               // จำนวนคู่ (ซ้าย+ขวา) รวม = pairs x 2 นัด
    public override int MagazineSize => pairs * 2;
    public float pairInterval = 0.15f;  // เว้นระหว่างคู่ (วินาที)
    public float sideOffset = 0.6f;     // ปล่อยห่างจากกลางตัวไปข้างละเท่าไร
    public float launchAngle = 45f;     // องศาที่เอียงจากทิศขึ้นบนของหน้าจอ
    public float sideViewSpread = 8f;   // องศา แยกสองลูกในทิศหันข้างออกจากกัน (0 = ไม่แยก)

    [Header("Launch Smoke (ควันที่ปากท่อ)")]
    public GameObject smokePrefab;          // ว่าง = ไม่มีควัน
    public float smokeLifetime = 0.6f;      // วินาที ให้เท่าความยาว clip
    public float smokeScale = 1f;           // ตัวคูณขนาดของ prefab
    public bool smokeFollowsMech = true;    // true = ควันติดไปกับหุ่น, false = อยู่กับที่ในโลก

    [Header("Missile")]
    public float speed = 18f;
    public float lifetime = 3f;
    public float damage = 50f;
    public float blastRadius = 2.5f;
    public float turnRate = 360f;       // องศาต่อวินาที
    public float homingDelay = 0.15f;   // บินตรงกี่วินาทีก่อนเริ่มเลี้ยวหาเป้า
    public float searchRadius = 30f;    // รัศมีค้นหาศัตรู
    public float fuseDistance = 1f;     // ระเบิดเมื่อเข้าใกล้เป้าเท่านี้ (กัน missile วนรอบเป้า) 0 = ปิด

    [Header("Stun / Knockback")]
    public StatusData stun;
    public float stunDuration = 1f;
    public float knockbackSpeed = 10f;

    public override void Activate(MechSkills user, int rounds)
    {
        user.StartCoroutine(Volley(user, rounds));
    }

    IEnumerator Volley(MechSkills user, int rounds)
    {
        MechLimbs limbs = user.GetComponent<MechLimbs>();
        int pairCount = (rounds + 1) / 2;

        for (int i = 0; i < pairCount; i++)
        {
            if (user == null || user.IsBlocked) yield break;   // ถูก Stun ระหว่างปล่อย = ยกเลิกที่เหลือ

            // อ่านทิศหุ่นใหม่ทุกคู่ ถ้าหันระหว่างปล่อย ตำแหน่งและทิศจะตามไป
            GetLaunchDirections(limbs, out Vector2 leftDir, out Vector2 rightDir);
            Launch(user, limbs, false, i, leftDir);
            if (i * 2 + 1 < rounds) Launch(user, limbs, true, i, rightDir);   // จำนวนคี่ = คู่สุดท้ายออกข้างซ้ายข้างเดียว

            yield return new WaitForSeconds(pairInterval);
        }
    }

    // Up/Down = ซ้ายบน+ขวาบน, ฝั่งซ้าย = ซ้ายบนทั้งคู่, ฝั่งขวา = ขวาบนทั้งคู่
    void GetLaunchDirections(MechLimbs limbs, out Vector2 leftDir, out Vector2 rightDir)
    {
        Vector2 upLeft = Quaternion.Euler(0f, 0f, launchAngle) * Vector2.up;
        Vector2 upRight = Quaternion.Euler(0f, 0f, -launchAngle) * Vector2.up;

        leftDir = upLeft;
        rightDir = upRight;

        string dirName = limbs != null ? limbs.CurrentDirectionName : null;
        if (dirName == null) return;

        if (dirName.EndsWith("Left") || dirName.EndsWith("Right"))
        {
            Vector2 baseDir = dirName.EndsWith("Left") ? upLeft : upRight;
            leftDir = Quaternion.Euler(0f, 0f, sideViewSpread) * baseDir;
            rightDir = Quaternion.Euler(0f, 0f, -sideViewSpread) * baseDir;
        }
    }

    void Launch(MechSkills user, MechLimbs limbs, bool isRight, int index, Vector2 dir)
    {
        // ไม่มีจุด mark ของทิศนี้ = ใช้วิธีเดิม (กลางตัว ± sideOffset)
        Vector2 pos = (Vector2)user.transform.position + new Vector2(sideOffset * (isRight ? 1f : -1f), 0f);
        if (limbs != null && limbs.TryGetLauncherPoint(isRight, index, out Vector2 launcherPos)) pos = launcherPos;

        Missile m = Instantiate(missilePrefab, pos, Quaternion.identity);
        m.Launch(dir, this, user.Mech);

        if (smokePrefab != null)
        {
            // อยู่เหนือตัวปล่อยของลูกนี้หนึ่งชั้น หันไปตามทิศ missile ที่ปากท่อ และอยู่กับที่ในโลก
            int order = (limbs != null ? limbs.LauncherSortingOrder(isRight) : 0) + 1;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            PooledEffect fx = EffectPool.Spawn(smokePrefab, pos, Quaternion.Euler(0f, 0f, angle),
                                            smokeLifetime, smokeFollowsMech ? user.transform : null, order);
            fx.transform.localScale = smokePrefab.transform.localScale * smokeScale;
        }
    }
}