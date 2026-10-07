using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// คุม trail 3 ชุดตามสถานะการเคลื่อนที่ เปิดได้ครั้งละชุดเดียว
//   Move  = 4 เส้น ความยาวคุมด้วยความเร็ว (โค้ดนี้)
//   Dash  = 4 เส้น จุดเดียวกับ Move ค่า time/ความกว้าง/สี ตั้งใน Inspector ของ TrailRenderer เอง
//   Boost = เส้นใหญ่เส้นเดียว (SpeedTrail เดิมคุมความยาวต่อ ส่วนที่นี่คุมแค่เปิด/ปิด)
public class JetTrailController : MonoBehaviour
{
    enum Mode { Move, Dash, Boost }

    [Header("Source")]
    [SerializeField] PlayerMove playerMove;
    [FormerlySerializedAs("rb")]
    [SerializeField] Rigidbody2D rb;

    [Header("Move (4 เส้น)")]
    [FormerlySerializedAs("jets")]
    [SerializeField] TrailRenderer[] moveJets = new TrailRenderer[4];
    [SerializeField] float moveMinSpeed = 0.5f;    // ต่ำกว่านี้ = หางสั้นสุด
    [SerializeField] float moveMaxSpeed = 6f;      // ใส่เท่า MaxSpeed ของหุ่น
    [SerializeField] float moveMinTime = 0.02f;
    [SerializeField] float moveMaxTime = 0.1f;
    [SerializeField] float smoothing = 10f;

    [Header("Dash (4 เส้น)")]
    [SerializeField] TrailRenderer[] dashJets = new TrailRenderer[4];

    [Header("Boost (เส้นเดียว)")]
    [SerializeField] TrailRenderer boostTrail;

    [Header("ตอนย้ายจุดวาง trail (เปลี่ยนทิศหุ่น)")]
    [Tooltip("true = ล้างหางของเส้นที่ย้าย กันเส้นลากข้ามจุด / false = ไม่ล้าง (ลองเทียบดู)")]
    [SerializeField] bool clearOnMove = true;

    int activeCount;                 // จำนวนจุดวางของทิศปัจจุบัน
    bool[] movePlaced, dashPlaced;   // เส้นนี้เคยถูกวางตำแหน่งแล้วหรือยัง (หลังถูกปิด ต้องวางใหม่)

    void Awake()
    {
        movePlaced = new bool[moveJets.Length];
        dashPlaced = new bool[dashJets.Length];

        // เมื่อก่อนโค้ดสั่ง SetActive ตามทิศ ตอนนี้ใช้ emitting แทน จึงต้องแน่ใจว่าทุกเส้นเปิดอยู่
        SetObjectsActive(moveJets);
        SetObjectsActive(dashJets);
        if (boostTrail != null) boostTrail.gameObject.SetActive(true);
    }

    static void SetObjectsActive(TrailRenderer[] jets)
    {
        foreach (TrailRenderer j in jets)
            if (j != null) j.gameObject.SetActive(true);
    }

    Mode CurrentMode()
    {
        if (playerMove == null) return Mode.Move;
        if (playerMove.IsBoosting) return Mode.Boost;
        if (playerMove.IsDashing) return Mode.Dash;
        return Mode.Move;
    }

    void LateUpdate()
    {
        Mode mode = CurrentMode();

        SetEmitting(moveJets, mode == Mode.Move);
        SetEmitting(dashJets, mode == Mode.Dash);
        if (boostTrail != null) boostTrail.emitting = mode == Mode.Boost;

        // ความยาวหาง Move ตามความเร็ว (Dash/Boost ตั้งใน Inspector / SpeedTrail)
        // ชุดที่ไม่ได้ใช้ปล่อยให้หางเก่าจางหายเองตาม time เดิม
        if (mode == Mode.Move)
        {
            float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
            float t = Mathf.InverseLerp(moveMinSpeed, moveMaxSpeed, speed);
            float target = Mathf.Lerp(moveMinTime, moveMaxTime, t * t);

            foreach (TrailRenderer jet in moveJets)
            {
                if (jet == null) continue;
                jet.time = Mathf.Lerp(jet.time, target, smoothing * Time.deltaTime);
            }
        }
    }

    void SetEmitting(TrailRenderer[] jets, bool on)
    {
        for (int i = 0; i < jets.Length; i++)
        {
            if (jets[i] == null) continue;
            jets[i].emitting = on && i < activeCount;
        }
    }

    // PlayerSpriteDirection เรียกทุกเฟรม: ย้ายจุดเฉพาะตอนตำแหน่งเปลี่ยนจริง
    public void ApplyDirection(DirectionArmData dir)
    {
        List<GlowPoint> points = dir != null ? dir.trailPoints : null;
        activeCount = points != null ? points.Count : 0;

        Place(moveJets, movePlaced, points);
        Place(dashJets, dashPlaced, points);
    }

    void Place(TrailRenderer[] jets, bool[] placed, List<GlowPoint> points)
    {
        for (int i = 0; i < jets.Length; i++)
        {
            TrailRenderer jet = jets[i];
            if (jet == null) continue;

            if (i >= activeCount)
            {
                placed[i] = false;   // ถูกปิดอยู่ รอบหน้าที่ถูกใช้ต้องวางใหม่
                continue;
            }

            Vector2 p = points[i].localPos;
            Vector3 cur = jet.transform.localPosition;
            Vector3 target = new Vector3(p.x, p.y, cur.z);
            bool moved = (cur - target).sqrMagnitude > 0.000001f;

            if (!placed[i] || moved)
            {
                jet.transform.localPosition = target;
                if (clearOnMove) jet.Clear();
                placed[i] = true;
            }
        }
    }
}