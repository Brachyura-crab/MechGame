using System.Collections.Generic;
using UnityEngine;

public class UltimateAura : MonoBehaviour
{
    [SerializeField] UltimateGauge gauge;
    [SerializeField] Health health;
    [SerializeField] float fadeInSpeed = 2f;
    [SerializeField] float fadeOutSpeed = 1.5f;

    [Header("Aura")]
    [SerializeField] SpriteRenderer auraRenderer;    // ลูกของ visualRoot ที่ (0,0) material Additive
    [SerializeField] SpriteRenderer torsoRenderer;
    [SerializeField] Color auraColor = new Color(1f, 0.1f, 0.05f);
    [SerializeField] float auraHdr = 2.5f;
    [SerializeField] float auraAlpha = 0.6f;
    [SerializeField] float auraBaseScale = 1f;
    [SerializeField] float pulseAmount = 0.12f;
    [SerializeField] float pulseSpeed = 4f;
    [SerializeField] int auraOrderOffset = -20;

    [Header("Rim glow")]
    [SerializeField] SpriteRenderer[] bodyRenderers;    // ลำตัว + แขนซ้าย + แขนขวา
    [SerializeField] Material rimMaterial;           // Additive HDR ตัวเดียวกับ glow
    [SerializeField] Color rimColor = new Color(1f, 0.05f, 0.02f);
    [SerializeField] float rimHdr = 4f;
    [SerializeField] float rimWidth = 0.03f;         // หน่วยโลก
    [SerializeField] Color bodyTint = new Color(1f, 0.35f, 0.3f);

    struct Rim { public SpriteRenderer src, copy; public Vector2 offset; }
    readonly List<Rim> rims = new List<Rim>();
    float intensity;

    void Awake()
    {
        foreach (SpriteRenderer s in bodyRenderers)
        {
            if (s == null) continue;
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                var go = new GameObject("Rim");
                go.transform.SetParent(s.transform, false);
                SpriteRenderer c = go.AddComponent<SpriteRenderer>();
                c.sharedMaterial = rimMaterial;
                c.enabled = false;
                rims.Add(new Rim { src = s, copy = c, offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rimWidth });
            }
        }
    }

    void LateUpdate()
    {
        bool dead = health != null && health.IsDead;
        float target = gauge.IsActive && !dead ? 1f : 0f;
        float speed = target > intensity ? fadeInSpeed : fadeOutSpeed;
        intensity = Mathf.MoveTowards(intensity, target, speed * Time.deltaTime);

        bool show = intensity > 0.001f && (health == null || !health.IsDead);
        Color tint = Color.Lerp(Color.white, bodyTint, intensity);
        foreach (SpriteRenderer b in bodyRenderers)
            if (b != null) b.color = tint;

        if (auraRenderer != null)
        {
            auraRenderer.enabled = show;
            if (show)
            {
                Color c = auraColor * auraHdr;
                c.a = auraAlpha * intensity;
                auraRenderer.color = c;
                float p = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                auraRenderer.transform.localScale = Vector3.one * auraBaseScale * p;
                auraRenderer.sortingLayerID = torsoRenderer.sortingLayerID;
                auraRenderer.sortingOrder = torsoRenderer.sortingOrder + auraOrderOffset;
            }
        }

        Color rc = rimColor * rimHdr;
        rc.a = intensity;
        foreach (Rim r in rims)
        {
            bool on = show && r.src.enabled && r.src.sprite != null;
            r.copy.enabled = on;
            if (!on) continue;
            r.copy.sprite = r.src.sprite;
            r.copy.flipX = r.src.flipX;
            r.copy.color = rc;
            r.copy.sortingLayerID = r.src.sortingLayerID;
            r.copy.sortingOrder = r.src.sortingOrder - 1;
            r.copy.transform.position = r.src.transform.position + (Vector3)r.offset;
        }
    }
}