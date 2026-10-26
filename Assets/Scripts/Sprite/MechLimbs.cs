using UnityEngine;

public class MechLimbs : MonoBehaviour
{
    [SerializeField] MechArmSet armSet;
    [SerializeField] SpriteRenderer torsoRenderer;   // ตัวเดียวกับที่ PlayerSpriteDirection ใช้อยู่แล้ว
    [SerializeField] Transform leftArmPivot;
    [SerializeField] Transform rightArmPivot;
    [SerializeField] SpriteRenderer leftArmRenderer;
    [SerializeField] SpriteRenderer rightArmRenderer;

    [SerializeField] SpriteRenderer torsoGlowRenderer;
    [SerializeField] SpriteRenderer rightArmGlowRenderer;

    [Header("Missile Launcher")]
    [SerializeField] SpriteRenderer launcherLeftRenderer;    // local (0,0) เหมือนลำตัว
    [SerializeField] SpriteRenderer launcherRightRenderer;
    [SerializeField] int launcherOrderOffset = 5;            // หน้าลำตัว = +ค่านี้, หลังลำตัว = -ค่านี้ (แขนอยู่ที่ ±10)

    DirectionArmData current;   // ตอนนี้มีแค่ Down รองรับทิศเดียวก่อน

    bool leftFrontCached = true;
    bool rightFrontCached = true;

    [Header("Recoil (กระตุกตอนยิง)")]
    [SerializeField] float kickStiffness = 600f;   // ยิ่งมาก ยิ่งกลับเร็ว
    [SerializeField] float kickDamping = 28f;      // ยิ่งน้อย ยิ่งเด้งเลยไปข้างหน้ามาก (ไม่เด้งเลย ≈ 49)

    float leftKickPos, leftKickVel, rightKickPos, rightKickVel;

    float leftShakeAmp, rightShakeAmp;

    public Vector2 MuzzleWorldPosition(bool isRight, ArmPose pose)
    {
        ArmPieceData piece = GetPiece(pose, isRight);
        Transform pivot = isRight ? rightArmPivot : leftArmPivot;
        if (piece == null) return pivot.position;
        return pivot.TransformPoint(piece.muzzleLocal);
    }

    public void SetDirection(DirectionArmData dir)
    {
        current = dir;
        if (torsoRenderer != null) torsoRenderer.sprite = dir.torso;
        if (torsoGlowRenderer != null) torsoGlowRenderer.sprite = dir.torsoGlow;
        if (launcherLeftRenderer != null) launcherLeftRenderer.sprite = dir.launcherLeft;
        if (launcherRightRenderer != null) launcherRightRenderer.sprite = dir.launcherRight;
        leftArmPivot.localPosition = dir.leftElbowLocal;
        rightArmPivot.localPosition = dir.rightElbowLocal;
    }

    public DirectionArmData FindDirection(string name)
    {
        if (armSet == null) return null;
        foreach (DirectionArmData d in armSet.directions)
            if (d.directionName == name) return d;
        return null;
    }

    public bool IsArmVisibleInFront(bool isRight) => isRight ? rightFrontCached : leftFrontCached;
    public int TorsoSortingOrder => torsoRenderer != null ? torsoRenderer.sortingOrder : 0;

    public string CurrentDirectionName => current != null ? current.directionName : null;

    // จุดปล่อยในโลก (index วนตามจำนวนจุดที่ mark ไว้) คืน false ถ้าทิศนี้ไม่มีตัวปล่อยหรือไม่มีจุด
    public bool TryGetLauncherPoint(bool isRight, int index, out Vector2 world)
    {
        world = default;
        if (current == null) return false;

        System.Collections.Generic.List<GlowPoint> pts = isRight ? current.launcherRightPoints : current.launcherLeftPoints;
        if (pts == null || pts.Count == 0) return false;

        SpriteRenderer sr = isRight ? launcherRightRenderer : launcherLeftRenderer;
        Transform t = torsoRenderer != null ? torsoRenderer.transform : (sr != null ? sr.transform : transform);
        world = t.TransformPoint(pts[index % pts.Count].localPos);
        return true;
    }

    // ลำดับ sorting ปัจจุบันของตัวปล่อย (ไม่มีตัวปล่อยให้ใช้ของลำตัวแทน)
    public int LauncherSortingOrder(bool isRight)
    {
        SpriteRenderer sr = isRight ? launcherRightRenderer : launcherLeftRenderer;
        if (sr != null) return sr.sortingOrder;
        return torsoRenderer != null ? torsoRenderer.sortingOrder : 0;
    }

    public void SetArmsVisible(bool visible)
    {
        if (leftArmRenderer != null) leftArmRenderer.enabled = visible;
        if (rightArmRenderer != null) rightArmRenderer.enabled = visible;
        if (torsoGlowRenderer != null) torsoGlowRenderer.enabled = visible;
        if (rightArmGlowRenderer != null) rightArmGlowRenderer.enabled = visible;
        if (launcherLeftRenderer != null) launcherLeftRenderer.enabled = visible;
        if (launcherRightRenderer != null) launcherRightRenderer.enabled = visible;
    }

    public void SetSortingOrder(int torsoOrder, bool armsInFront = true)
    {
        bool leftFront = armsInFront && (current == null || current.leftArmInFront);
        bool rightFront = armsInFront && (current == null || current.rightArmInFront);
        leftFrontCached = leftFront;
        rightFrontCached = rightFront;

        int leftOrder = torsoOrder + (leftFront ? 10 : -10);
        int rightOrder = torsoOrder + (rightFront ? 10 : -10);

        if (leftArmRenderer != null) leftArmRenderer.sortingOrder = leftOrder;
        if (rightArmRenderer != null) rightArmRenderer.sortingOrder = rightOrder;

        if (torsoGlowRenderer != null) torsoGlowRenderer.sortingOrder = torsoOrder + 4;
        if (rightArmGlowRenderer != null) rightArmGlowRenderer.sortingOrder = rightOrder + 2;   // ติดตามแขนขวาเสมอ

        bool leftLauncherFront = current != null && current.launcherLeftInFront;
        bool rightLauncherFront = current != null && current.launcherRightInFront;
        if (launcherLeftRenderer != null)
            launcherLeftRenderer.sortingOrder = torsoOrder + (leftLauncherFront ? launcherOrderOffset : -launcherOrderOffset);
        if (launcherRightRenderer != null)
            launcherRightRenderer.sortingOrder = torsoOrder + (rightLauncherFront ? launcherOrderOffset : -launcherOrderOffset);
    }

    public void SetPose(ArmPose leftPose, ArmPose rightPose, float leftFacingAngleDeg, float rightFacingAngleDeg)
    {
        if (current == null) return;

        ArmPieceData l = GetPiece(leftPose, false);
        ArmPieceData r = GetPiece(rightPose, true);

        leftArmRenderer.sprite = l != null ? l.sprite : null;
        rightArmRenderer.sprite = r != null ? r.sprite : null;

        if (rightArmGlowRenderer != null) rightArmGlowRenderer.sprite = r != null ? r.glowSprite : null;

        // ท่า Aim ใช้การหมุนแบบโลก (ไม่นับการเอียงของ visualRoot) ท่าอื่นตามลำตัวตามปกติ
        if (IsAimPose(leftPose)) leftArmPivot.rotation = AimRotation(l, leftFacingAngleDeg);
        else leftArmPivot.localRotation = Quaternion.identity;

        if (IsAimPose(rightPose)) rightArmPivot.rotation = AimRotation(r, rightFacingAngleDeg);
        else rightArmPivot.localRotation = Quaternion.identity;

        ApplyKick(leftArmPivot, IsAimPose(leftPose), leftFacingAngleDeg, ref leftKickPos, ref leftKickVel);
        ApplyKick(rightArmPivot, IsAimPose(rightPose), rightFacingAngleDeg, ref rightKickPos, ref rightKickVel);

        if (IsAimPose(leftPose) && leftShakeAmp > 0f)
            leftArmPivot.position += (Vector3)(UnityEngine.Random.insideUnitCircle * leftShakeAmp);
        if (IsAimPose(rightPose) && rightShakeAmp > 0f)
            rightArmPivot.position += (Vector3)(UnityEngine.Random.insideUnitCircle * rightShakeAmp);
    }

    // หมุนจากมุมที่ปืนชี้ "จริง" ในภาพต้นฉบับ (คำนวณจากตำแหน่งปลายกระบอกที่สแกนไว้)
    // ไปยัง facingAngleDeg (มุมทิศเล็งจริงในโลก) แทนการสมมติว่าภาพชี้ตรงทิศหลักเป๊ะ
    static Quaternion AimRotation(ArmPieceData piece, float facingAngleDeg)
    {
        if (piece == null || piece.muzzleLocal.sqrMagnitude < 0.0001f)
            return Quaternion.Euler(0, 0, facingAngleDeg);

        float drawnAngle = Mathf.Atan2(piece.muzzleLocal.y, piece.muzzleLocal.x) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(drawnAngle, facingAngleDeg);
        return Quaternion.Euler(0, 0, delta);
    }

    static bool IsAimPose(ArmPose p) => p == ArmPose.Aim;

    ArmPieceData GetPiece(ArmPose pose, bool right)
    {
        if (current == null) return null;
        foreach (ArmPoseSet p in current.poses)
            if (p.pose == pose) return right ? p.right : p.left;
        return null;
    }

    // มุมจากจุดหมุนแขนไปยังจุดเล็ง (ไม่ใช่จากกลางลำตัว) ใกล้เกินไปใช้ค่าสำรอง
    public float AimAngleFromPivot(bool isRight, float fallbackDeg, Vector2 aimPoint)
    {
        Transform pivot = isRight ? rightArmPivot : leftArmPivot;
        Vector2 to = aimPoint - (Vector2)pivot.position;
        if (to.sqrMagnitude < 0.25f) return fallbackDeg;
        return Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
    }

    public Vector2 PivotPosition(bool isRight) =>
        (Vector2)(isRight ? rightArmPivot : leftArmPivot).position;

        public void Kick(bool isRight, float distance)
    {
        if (isRight) { rightKickPos = distance; rightKickVel = 0f; }
        else { leftKickPos = distance; leftKickVel = 0f; }
    }

    // สปริง: pos = ระยะที่ถอยหลังจากตำแหน่งปกติ (ค่าลบ = เด้งเลยไปข้างหน้า)
    void ApplyKick(Transform pivot, bool aiming, float angleDeg, ref float pos, ref float vel)
    {
        if (!aiming) { pos = 0f; vel = 0f; return; }

        float dt = Mathf.Min(Time.deltaTime, 0.033f);
        vel += (-kickStiffness * pos - kickDamping * vel) * dt;
        pos += vel * dt;

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 back = -new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        pivot.position += (Vector3)(back * pos);   // ตำแหน่งฐานถูกตั้งใหม่ทุกเฟรมใน SetDirection จึงไม่สะสม
    }

    public void SetShake(bool isRight, float amplitude)
    {
        if (isRight) rightShakeAmp = amplitude; else leftShakeAmp = amplitude;
    }
}