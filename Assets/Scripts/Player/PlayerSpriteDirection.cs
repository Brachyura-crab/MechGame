using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MechAim))]
public class PlayerSpriteDirection : MonoBehaviour
{
    [SerializeField] InputActionReference attackAction;
    [SerializeField] InputActionReference attack2Action;
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
    float currentTilt;
    Rigidbody2D rb;

    [SerializeField] int orderAboveTrail = 1;   // ทิศที่หันหน้าเข้าหากล้อง: อยู่บน trail
    [SerializeField] int orderBelowTrail = -1;  // ทิศที่หันหลังให้กล้อง: อยู่ใต้ trail

    public Vector2 FacingDirection => new Vector2(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        aim = GetComponent<MechAim>();
    }

    void OnEnable()
    {
        attackAction.action.Enable();
        attack2Action.action.Enable();
    }   

    void OnDisable()
    {
        attackAction.action.Disable();
        attack2Action.action.Disable();
    }

    void Update()
    {
        Vector2 v = rb.linearVelocity;
        float targetTilt = -Mathf.Clamp(v.x / tiltRefSpd, -1f, 1f) * maxTiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSmoothing * Time.deltaTime);
        spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
        
        bool hasTarget = false;
        float targetAngle = facingAngle;

        if (attackAction.action.IsPressed() || attack2Action.action.IsPressed())
        {
            Vector2 toMouse = aim.AimPoint - (Vector2)transform.position;
            if (toMouse.sqrMagnitude > minAimDistance * minAimDistance)
            {
                targetAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
                hasTarget = true;
            }
        }

        if (!hasTarget && v.sqrMagnitude >= minSpeedToTurn * minSpeedToTurn)
        {
            targetAngle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            hasTarget = true;
        }

        if (hasTarget)
            facingAngle = Mathf.MoveTowardsAngle(facingAngle, targetAngle, turnSpd * Time.deltaTime);

        int index = Mathf.RoundToInt(facingAngle / 45f);
        index = ((index % 8) + 8) % 8;
        spriteRenderer.sprite = directionSprites[index];

        // index: 0=ขวา 1=ขวาบน 2=บน 3=ซ้ายบน 4=ซ้าย 5=ซ้ายล่าง 6=ล่าง 7=ขวาล่าง
        bool facingUp = index == 1 || index == 2 || index == 3;
        spriteRenderer.sortingOrder = facingUp ? orderBelowTrail : orderAboveTrail;
    }
}