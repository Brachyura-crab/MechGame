using System.Collections.Generic;
using UnityEngine;

public class JetGlowController : MonoBehaviour
{
    [SerializeField] MechLimbs limbs;             // ใช้ FindDirection หาภาพทิศต้นทางตอนมิเรอร์
    [SerializeField] SpriteRenderer jetRenderer;   // ลูกของ BodySprite วางไว้หลังลำตัว
    [SerializeField] Rigidbody2D rb;

    [Header("Flicker")]
    [SerializeField] float minSpeed = 0.5f;
    [SerializeField] float maxSpeed = 14f;
    [SerializeField] float minAlpha = 0.15f;
    [SerializeField] float maxAlpha = 1f;
    [SerializeField] float flickerAmount = 0.25f;   // สัดส่วนที่สั่นเพิ่ม/ลดจากค่าฐาน
    [SerializeField] float flickerSpeed = 18f;      // ยิ่งสูงยิ่งสั่นถี่
    [SerializeField] Color color = Color.white;
    [SerializeField] float hdrIntensity = 3f;

    static readonly Dictionary<string, string> MirrorFrom = new Dictionary<string, string>
    {
        { "Right", "Left" },
        { "UpRight", "UpLeft" },
        { "DownRight", "DownLeft" },
    };

    float noiseSeed;
    float currentAlpha;

    void Awake() => noiseSeed = Random.Range(0f, 100f);

    void Update()
    {
        if (jetRenderer == null || !jetRenderer.enabled) return;

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
        float baseAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        float noise = (Mathf.PerlinNoise(Time.time * flickerSpeed, noiseSeed) - 0.5f) * 2f; // -1..1
        float target = Mathf.Clamp01(baseAlpha + noise * flickerAmount * t); // ยิ่งเร็วยิ่งสั่นแรง

        currentAlpha = Mathf.Lerp(currentAlpha, target, 20f * Time.deltaTime);

        Color c = color * hdrIntensity;
        c.a = currentAlpha;
        jetRenderer.color = c;
    }

    public void ApplyDirection(DirectionArmData dir)
    {
        if (jetRenderer == null) return;

        if (dir == null)
        {
            jetRenderer.enabled = false;
            return;
        }

        Sprite sprite = dir.jetGlowSprite;
        bool flip = false;

        if (sprite == null && MirrorFrom.TryGetValue(dir.directionName, out string sourceName))
        {
            DirectionArmData source = limbs != null ? limbs.FindDirection(sourceName) : null;
            if (source != null)
            {
                sprite = source.jetGlowSprite;
                flip = true;
            }
        }

        jetRenderer.sprite = sprite;
        jetRenderer.flipX = flip;
        jetRenderer.enabled = sprite != null;
    }

    public void SetSortingOrder(int torsoOrder, DirectionArmData dir)
    {
        bool front = dir != null && dir.jetGlowInFront;
        if (jetRenderer != null) jetRenderer.sortingOrder = torsoOrder + (front ? 6 : -2);
    }
}