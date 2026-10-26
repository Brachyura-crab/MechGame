using UnityEngine;

public class CameraTargetController : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Rigidbody2D playerRb;
    [SerializeField] MechAim playerAim;
    [SerializeField] WeaponArbiter arbiter;    // ว่างไว้ = ใช้ aimLead / maxAimOffset ด้านล่างกับทุกอาวุธ
    WeaponData lastWeapon;
    [SerializeField] float aimLead = 0.3f;        // สัดส่วนระยะเมาส์ที่กล้องเลื่อนไป (0-1)
    [SerializeField] float maxAimOffset = 4f;     // เลื่อนไปไกลสุดกี่หน่วย
    [SerializeField] float moveLeadTime = 0.4f;   // เท่ากับ Lookahead Time เดิม
    [SerializeField] float maxMoveOffset = 3f;
    [SerializeField] float followSpeed = 6f;      // ยิ่งต่ำ ยิ่งเปลี่ยนทิศนุ่ม

    [SerializeField] CameraShake shake;

    Camera cam;
    Vector2 offset;

    void Awake() => cam = Camera.main;

    void LateUpdate()
    {
        if (arbiter != null && arbiter.Active != null) lastWeapon = arbiter.Active.Data;
        Vector2 target;
        if (playerAim.IsAiming)
        {
            Vector2 world = playerAim.AimPoint;
            float lead = lastWeapon != null ? lastWeapon.cameraAimLead : aimLead;
            float maxOff = lastWeapon != null ? lastWeapon.cameraMaxOffset : maxAimOffset;
            lead = Mathf.Clamp(lead, 0f, 0.95f);

            // วัดระยะเมาส์จากกลางจอ (ไม่ใช่จากตัวหุ่น) ผลสุดท้ายกล้องเลื่อนไป lead ของระยะ ตัวหุ่น→เมาส์ เท่าเดิม
            Vector2 fromCenter = (Vector2)world - (Vector2)cam.transform.position;
            target = Vector2.ClampMagnitude(fromCenter * (lead / (1f - lead)), maxOff);
        }
        else
        {
            target = Vector2.ClampMagnitude(playerRb.linearVelocity * moveLeadTime, maxMoveOffset);
        }

        offset = Vector2.Lerp(offset, target, followSpeed * Time.deltaTime);
        Vector2 shakeOffset = shake != null ? shake.CurrentOffset : Vector2.zero;
        transform.position = (Vector2)player.position + offset + shakeOffset;
    }
}