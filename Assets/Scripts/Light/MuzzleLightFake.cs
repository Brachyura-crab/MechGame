using UnityEngine;

public class MuzzleLightFake : MonoBehaviour
{
    [SerializeField] SpriteRenderer torsoRenderer;    // ตัวเดียวกับที่ PlayerSpriteDirection ใช้
    [SerializeField] SpriteRenderer flashRenderer;    // ลูกของ torso เดียวกัน material Additive
    [SerializeField] Color flashColor = new Color(1f, 0.85f, 0.6f);
    [SerializeField] float hdrIntensity = 3f;   // ความสว่างเกิน 1 เพื่อให้ Bloom จับได้
    [SerializeField] float peakAlpha = 0.5f;
    [SerializeField] float fadeTime = 0.08f;

    float timer;
    bool active;

    public int TorsoSortingOrder => torsoRenderer != null ? torsoRenderer.sortingOrder : 0;

    void Update()
    {
        if (!active) return;

        timer -= Time.deltaTime;
        float a = Mathf.Clamp01(timer / fadeTime) * peakAlpha;
        Color c = flashColor * hdrIntensity;
        c.a = a;
        flashRenderer.color = c;

        if (timer <= 0f)
        {
            active = false;
            flashRenderer.enabled = false;
        }
    }

    // เรียกตอนยิง: frontVisible = ปลายกระบอกอยู่ฝั่งที่หันเข้าหากล้องไหม (ถ้าไม่ใช่ = แสงโดนตัวบัง ไม่ทะลุมา)
    public void Flash(bool frontVisible)
    {
        if (!frontVisible || torsoRenderer == null || flashRenderer == null) return;

        flashRenderer.sprite = torsoRenderer.sprite;
        flashRenderer.sortingOrder = torsoRenderer.sortingOrder + 5;
        flashRenderer.enabled = true;
        timer = fadeTime;
        active = true;
    }
}