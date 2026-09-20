using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)] // ให้อัปเดตก่อนสคริปต์ที่อ่านทิศเล็ง
[RequireComponent(typeof(MechAim))]
public class PlayerAimInput : MonoBehaviour
{
    [SerializeField] InputActionReference aimAction;

    MechAim aim;
    Camera cam;

    void Awake()
    {
        aim = GetComponent<MechAim>();
        cam = Camera.main;
    }

    void OnEnable() => aimAction.action.Enable();
    void OnDisable() => aimAction.action.Disable();

    void Update()
    {
        Vector2 mouse = aimAction.action.ReadValue<Vector2>();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
        aim.SetAimPoint(world);
    }
}