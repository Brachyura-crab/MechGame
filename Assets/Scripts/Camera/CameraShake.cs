using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] PlayerMove playerMove;
    [SerializeField] float burstAmplitude = 0.5f;
    [SerializeField] float burstDecay = 2.5f;
    [SerializeField] float sustainAmplitude = 0.05f;
    [SerializeField] float frequency = 20f;

    [Header("Kick (สั่นตอนยิง)")]
    [SerializeField] float kickDecay = 3f;        // ความแรงที่ลดต่อวินาที ยิ่งมากยิ่งสั้น
    [SerializeField] float kickFrequency = 35f;

    public static CameraShake Instance { get; private set; }
    float kickStrength;
    float kickDecayNow;

    ShakePulse pulse;
    float seed;

    public Vector2 CurrentOffset { get; private set; }

    void Awake()
    {
        pulse = new ShakePulse(burstAmplitude, burstDecay, sustainAmplitude, frequency);
        seed = Random.Range(0f, 100f);
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // duration > 0 = ให้ความแรงนี้จางหมดในเวลานั้น, 0 = ใช้ kickDecay ตามปกติ
    public void Kick(float amplitude, float duration = 0f)
    {
        if (amplitude < kickStrength) return;   // ต้องแรงกว่าที่กำลังสั่นอยู่ถึงจะแทนที่
        kickStrength = amplitude;
        kickDecayNow = duration > 0f ? amplitude / duration : kickDecay;
    }

    void Update()
    {
        Vector2 boostOffset = playerMove != null ? pulse.Update(playerMove.IsBoosting, seed) : Vector2.zero;

        Vector2 kickOffset = Vector2.zero;
        if (kickStrength > 0.0001f)
        {
            float t = Time.time * kickFrequency;
            float x = (Mathf.PerlinNoise(t, seed + 17.3f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(seed + 17.3f, t) - 0.5f) * 2f;
            kickOffset = new Vector2(x, y) * kickStrength;
            kickStrength = Mathf.MoveTowards(kickStrength, 0f, kickDecayNow * Time.deltaTime);
        }

        CurrentOffset = boostOffset + kickOffset;
    }
}