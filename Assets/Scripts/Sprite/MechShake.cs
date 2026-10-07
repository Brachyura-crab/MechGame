using UnityEngine;

public class MechShake : MonoBehaviour
{
    [SerializeField] PlayerMove playerMove;
    [SerializeField] Transform bodySprite;        // สั่นเฉพาะตัวนี้ ไม่แตะ visualRoot
    [SerializeField] float burstAmplitude = 0.08f;
    [SerializeField] float burstDecay = 0.4f;
    [SerializeField] float sustainAmplitude = 0.015f;
    [SerializeField] float frequency = 25f;

    ShakePulse pulse;
    Vector3 basePos;
    float seed;

    void Awake()
    {
        pulse = new ShakePulse(burstAmplitude, burstDecay, sustainAmplitude, frequency);
        seed = Random.Range(0f, 100f);
        if (bodySprite != null) basePos = bodySprite.localPosition;
    }

    void LateUpdate()
    {
        if (bodySprite == null || playerMove == null) return;
        Vector2 offset = pulse.Update(playerMove.IsBoosting, seed);
        bodySprite.localPosition = basePos + (Vector3)offset;
    }
}