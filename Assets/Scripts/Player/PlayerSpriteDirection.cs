using UnityEngine;

class ArmPoseState
{
    enum Phase { Idle, Raising, Aim, Lowering }
    Phase phase = Phase.Idle;
    float t;

    public ArmPose CurrentPose { get; private set; } = ArmPose.Idle;

    public void Update(bool engaged, float raiseTime, float lowerTime)
    {
        switch (phase)
        {
            case Phase.Idle:
                CurrentPose = ArmPose.Idle;
                if (engaged) { phase = Phase.Raising; t = 0f; }
                break;

            case Phase.Raising:
                CurrentPose = ArmPose.Raised;
                if (!engaged) { phase = Phase.Lowering; t = 0f; break; }
                t += Time.deltaTime;
                if (t >= raiseTime) phase = Phase.Aim;
                break;

            case Phase.Aim:
                CurrentPose = ArmPose.Aim;
                if (!engaged) { phase = Phase.Lowering; t = 0f; }
                break;

            case Phase.Lowering:
                CurrentPose = ArmPose.Raised;
                t += Time.deltaTime;
                if (t >= lowerTime) phase = Phase.Idle;
                break;
        }
    }
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MechAim))]
public class PlayerSpriteDirection : MonoBehaviour
{
    [SerializeField] Sprite[] directionSprites = new Sprite[8];
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] float minSpeedToTurn = 0.1f;
    [SerializeField] float maxTiltAngle = 12f;
    [SerializeField] float tiltRefSpd = 5f;
    [SerializeField] float tiltSmoothing = 12f;

    [SerializeField] float turnSpd = 720f;
    [SerializeField] float minAimDistance = 0.5f;
    float facingAngle = -90f;

    MechAim aim;

    MechLimbs limbs;

    [SerializeField] JetTrailController jetTrail;
    [SerializeField] JetGlowController jetGlow;

    [SerializeField] SpriteRenderer bodyShadowRenderer;

    readonly ArmPoseState leftArmState = new ArmPoseState();
    readonly ArmPoseState rightArmState = new ArmPoseState();
    [SerializeField] float armRaiseTime = 0.08f;
    [SerializeField] float armLowerTime = 0.08f;
    [SerializeField] Weapon rifleWeapon;
    [SerializeField] Weapon magnumWeapon;

    public bool LeftArmReady { get; private set; }
    public bool RightArmReady { get; private set; }

    public float? ForcedAngle { get; set; }
    public bool HideMech { get; set; }
    public bool IsFacing(float angle) => Mathf.Abs(Mathf.DeltaAngle(facingAngle, angle)) < 1f;

    static readonly string[] DirectionNames =
        { "Right", "UpRight", "Up", "UpLeft", "Left", "DownLeft", "Down", "DownRight" };

    float currentTilt;
    Rigidbody2D rb;

    [SerializeField] int orderAboveTrail = 1;   // ทิศที่หันหน้าเข้าหากล้อง: อยู่บน trail
    [SerializeField] int orderBelowTrail = -1;  // ทิศที่หันหลังให้กล้อง: อยู่ใต้ trail
    [SerializeField] Transform visualRoot;   // พ่อร่วมของ spriteRenderer + แขนทั้งสอง ใช้หมุน tilt รวมเป็นก้อนเดียว

    public Vector2 FacingDirection => new Vector2(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        aim = GetComponent<MechAim>();
        limbs = GetComponent<MechLimbs>();
    }

    void Update()
    {
        Vector2 v = rb.linearVelocity;
        float targetTilt = -Mathf.Clamp(v.x / tiltRefSpd, -1f, 1f) * maxTiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothing * Time.deltaTime);
        visualRoot.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
        
        bool hasTarget = false;
        float targetAngle = facingAngle;
        bool aiming = ForcedAngle == null && aim.IsAiming;
        if (aiming)
        {
            Vector2 toMouse = aim.AimPoint - (Vector2)transform.position;
            if (toMouse.sqrMagnitude > minAimDistance * minAimDistance)
            {
                targetAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
                hasTarget = true;
            }
        }

        if (ForcedAngle.HasValue) { targetAngle = ForcedAngle.Value; hasTarget = true; }

        if (!hasTarget && v.sqrMagnitude >= minSpeedToTurn * minSpeedToTurn)
        {
            targetAngle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            hasTarget = true;
        }

        if (hasTarget)
        {
            facingAngle = Mathf.MoveTowardsAngle(facingAngle, targetAngle, turnSpd * Time.deltaTime);
        }
        else
        {
            // ไม่ได้เล็งและไม่ได้เคลื่อนที่: ค่อยๆ ปรับให้ลงตัวที่ทิศตรง 45° ไม่ค้างเยื้อง
            float nearest = Mathf.Round(facingAngle / 45f) * 45f;
            facingAngle = Mathf.MoveTowardsAngle(facingAngle, nearest, turnSpd * Time.deltaTime);
        }

        int index = Mathf.RoundToInt(facingAngle / 45f);
        index = ((index % 8) + 8) % 8;

        DirectionArmData dir = limbs != null ? limbs.FindDirection(DirectionNames[index]) : null;

        if (dir != null)
        {
            limbs.SetDirection(dir);
            if (bodyShadowRenderer != null) bodyShadowRenderer.sprite = dir.torso;
            jetTrail?.ApplyDirection(dir);
            jetGlow?.ApplyDirection(dir);
            limbs.SetArmsVisible(true);

            bool leftEngaged = ForcedAngle == null && rifleWeapon != null && rifleWeapon.HasControl;
            bool rightEngaged = ForcedAngle == null && magnumWeapon != null && magnumWeapon.HasControl;

            leftArmState.Update(leftEngaged, armRaiseTime, armLowerTime);
            rightArmState.Update(rightEngaged, armRaiseTime, armLowerTime);

            LeftArmReady = leftArmState.CurrentPose == ArmPose.Aim;
            RightArmReady = rightArmState.CurrentPose == ArmPose.Aim;

            float leftAimAngle = limbs.AimAngleFromPivot(false, facingAngle, aim.AimPoint);
            float rightAimAngle = limbs.AimAngleFromPivot(true, facingAngle, aim.AimPoint);
            limbs.SetPose(leftArmState.CurrentPose, rightArmState.CurrentPose, leftAimAngle, rightAimAngle);
        }
        else
        {
            limbs?.SetArmsVisible(false);
            jetTrail?.ApplyDirection(null);
            jetGlow?.ApplyDirection(null);
            LeftArmReady = true;
            RightArmReady = true;
            spriteRenderer.sprite = directionSprites[index];
            if (bodyShadowRenderer != null) bodyShadowRenderer.sprite = directionSprites[index];
        }

        // index: 0=ขวา 1=ขวาบน 2=บน 3=ซ้ายบน 4=ซ้าย 5=ซ้ายล่าง 6=ล่าง 7=ขวาล่าง
        bool facingUp = index == 1 || index == 2 || index == 3;
        spriteRenderer.sortingOrder = facingUp ? orderBelowTrail : orderAboveTrail;
        if (bodyShadowRenderer != null) bodyShadowRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
        limbs?.SetSortingOrder(spriteRenderer.sortingOrder, !facingUp);
        jetGlow?.SetSortingOrder(spriteRenderer.sortingOrder, dir);

        if (HideMech)
        {
            spriteRenderer.enabled = false;
            if (bodyShadowRenderer != null) bodyShadowRenderer.enabled = false;
            limbs?.SetArmsVisible(false);
        }
        else
        {
            spriteRenderer.enabled = true;
            if (bodyShadowRenderer != null) bodyShadowRenderer.enabled = true;
        }
    }
}