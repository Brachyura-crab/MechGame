using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class SpeedTrail : MonoBehaviour
{
    [SerializeField] Rigidbody2D playerRb;
    [SerializeField] float minSpeed = 4f;      // ต่ำกว่านี้ = หางสั้นสุด
    [SerializeField] float maxSpeed = 14f;     // ใส่เท่า boostSpeed
    [SerializeField] float minTime = 0.03f;    // หางสั้นมากตอน move
    [SerializeField] float maxTime = 0.35f;    // หางยาวตอน boost
    [SerializeField] float smoothing = 10f;

    TrailRenderer trail;

    void Awake() => trail = GetComponent<TrailRenderer>();

    void LateUpdate()
    {
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, playerRb.linearVelocity.magnitude);
        float target = Mathf.Lerp(minTime, maxTime, t * t); // t*t ทำให้หางยาวเฉพาะตอนเร็วจริง
        trail.time = Mathf.Lerp(trail.time, target, smoothing * Time.deltaTime);
    }
}