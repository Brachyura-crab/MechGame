using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerSpriteDirection))]
[RequireComponent(typeof(MechStats))]
[RequireComponent(typeof(Energy))]
public class PlayerMove : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference dashAction;
    [SerializeField] float boostHitchTime = 0.08f;

    MechStats mechStats;
    StatBlock S => mechStats.Stats;
    Energy energy;

    float maxSpeed => S.Get(StatId.MaxSpeed);
    float acceleration => S.Get(StatId.Acceleration);
    float deceleration => S.Get(StatId.Deceleration);
    float dashSpeed => S.Get(StatId.DashSpeed);
    float dashTime => S.Get(StatId.DashTime);
    float dashCooldown => S.Get(StatId.DashCooldown);
    float boostSpeed => S.Get(StatId.BoostSpeed);
    float boostTurnRate => S.Get(StatId.BoostTurnRate);
    float dashEnergyCost => S.Get(StatId.DashEnergyCost);
    float boostEnergyPerSec => S.Get(StatId.BoostEnergyPerSec);

    enum State { Normal, Dash, Hitch, Boost }
    State state;

    Rigidbody2D rb;
    PlayerSpriteDirection facing;
    Vector2 moveInput;
    Vector2 dashDir;
    float dashStartTime, nextDashTime, boostAngle;
    bool dashPressed;
    float hitchEndTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        facing = GetComponent<PlayerSpriteDirection>();
        mechStats = GetComponent<MechStats>();
        energy = GetComponent<Energy>();
    }

    void OnEnable()
    {
        moveAction.action.Enable();
        dashAction.action.Enable();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        dashAction.action.Disable();
    }

    void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();
        if (dashAction.action.WasPressedThisFrame()) dashPressed = true;
    }

    void FixedUpdate()
    {
        float now = Time.time;
        bool pressed = dashPressed;
        dashPressed = false;

        switch (state)
        {
            case State.Normal:
                if (pressed && now >= nextDashTime && HasEnergy(dashEnergyCost)) StartDash(now);
                else Move();
                break;

            case State.Dash:
                rb.linearVelocity = dashDir * dashSpeed;
                if (now >= dashStartTime + dashTime)
                {
                    rb.linearVelocity = Vector2.zero; // จบ Dash = ความเร็วเป็น 0
                    if (dashAction.action.IsPressed() && HasEnergy(0.01f)) StartHitch(now);
                    else
                    {
                        nextDashTime = now + dashCooldown; // ไม่ได้กดค้าง = ติด cooldown
                        state = State.Normal;
                    }
                }
                break;

            case State.Hitch: // ชะงักสั้นๆ ก่อนพุ่ง Boost
                rb.linearVelocity = Vector2.zero;
                if (now >= hitchEndTime) StartBoost();
                break;

            case State.Boost:
                UpdateBoost();
                break;
        }
    }

    void Move()
    {
        Vector2 target = Vector2.ClampMagnitude(moveInput, 1f) * maxSpeed;
        float rate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, rate * Time.fixedDeltaTime);
    }

    void StartDash(float now)
    {
        UseEnergy(dashEnergyCost);
        dashDir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : facing.FacingDirection;
        dashStartTime = now;
        state = State.Dash;
    }

    void StartHitch(float now)
    {
        hitchEndTime = now + boostHitchTime;
        state = State.Hitch;
    }

    void StartBoost()
    {
        boostAngle = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg;
        state = State.Boost;
    }

    void UpdateBoost()
    {
        // เลิกกดเดิน หรือ energy หมด = จบ Boost แล้วกลับไปเบรกนุ่มด้วย deceleration
        if (moveInput.sqrMagnitude < 0.01f || !HasEnergy(0.01f))
        {
            state = State.Normal;
            return;
        }

        UseEnergy(boostEnergyPerSec * Time.fixedDeltaTime);

        float target = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
        boostAngle = Mathf.MoveTowardsAngle(boostAngle, target, boostTurnRate * Time.fixedDeltaTime);
        float r = boostAngle * Mathf.Deg2Rad;
        rb.linearVelocity = new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * boostSpeed;
    }

    bool HasEnergy(float amount) => energy.Has(amount);
    void UseEnergy(float amount) => energy.Use(amount);
}