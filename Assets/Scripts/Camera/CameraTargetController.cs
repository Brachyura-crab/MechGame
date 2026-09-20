using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTargetController : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Rigidbody2D playerRb;
    [SerializeField] InputActionReference aimAction;
    [SerializeField] InputActionReference attackAction;
        [SerializeField] InputActionReference attack2Action;
    [SerializeField] float aimLead = 0.3f;        // สัดส่วนระยะเมาส์ที่กล้องเลื่อนไป (0-1)
    [SerializeField] float maxAimOffset = 4f;     // เลื่อนไปไกลสุดกี่หน่วย
    [SerializeField] float moveLeadTime = 0.4f;   // เท่ากับ Lookahead Time เดิม
    [SerializeField] float maxMoveOffset = 3f;
    [SerializeField] float followSpeed = 6f;      // ยิ่งต่ำ ยิ่งเปลี่ยนทิศนุ่ม

    Camera cam;
    Vector2 offset;

    void Awake() => cam = Camera.main;

    void LateUpdate()
    {
        Vector2 target;

        if (attackAction.action.IsPressed() || attack2Action.action.IsPressed())
        {
            Vector2 mouse = aimAction.action.ReadValue<Vector2>();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
            Vector2 toMouse = (Vector2)world - (Vector2)player.position;
            target = Vector2.ClampMagnitude(toMouse * aimLead, maxAimOffset);
        }
        else
        {
            target = Vector2.ClampMagnitude(playerRb.linearVelocity * moveLeadTime, maxMoveOffset);
        }

        offset = Vector2.Lerp(offset, target, followSpeed * Time.deltaTime);
        transform.position = (Vector2)player.position + offset;
    }
}