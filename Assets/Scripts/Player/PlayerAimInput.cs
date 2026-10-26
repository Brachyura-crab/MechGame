using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)] // ให้อัปเดตก่อนสคริปต์ที่อ่านทิศเล็ง
[RequireComponent(typeof(MechAim))]
public class PlayerAimInput : MonoBehaviour
{
    [SerializeField] InputActionReference aimAction;
    [SerializeField] InputActionReference attackAction;    // ปุ่มยิง 1
    [SerializeField] InputActionReference attack2Action;   // ปุ่มยิง 2

    MechAim aim;
    Camera cam;

    void Awake() => aim = GetComponent<MechAim>();

    // ไม่ Disable ปุ่มยิงตอนปิด เพราะ Weapon ใช้ action ชุดเดียวกันร่วมกัน
    void OnEnable()
    {
        aimAction.action.Enable();
        attackAction.action.Enable();
        attack2Action.action.Enable();
    }

    void OnDisable() => aimAction.action.Disable();

    void Update()
    {
        if (PauseMenu.InputBlocked) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 mouse = aimAction.action.ReadValue<Vector2>();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
        aim.SetAimPoint(world);
        aim.SetFireHeld(attackAction.action.IsPressed() || attack2Action.action.IsPressed());
    }
}