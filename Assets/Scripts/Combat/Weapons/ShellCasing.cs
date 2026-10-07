using UnityEngine;

// ปลอกกระสุนในอวกาศ: ลอยออกไปพร้อมหมุนตัว ชะลอเล็กน้อย แล้วจางหาย (ไม่มีแรงโน้มถ่วง)
// ใช้คู่กับ EffectPool (EffectPool คุมอายุและลำดับ sorting)
public class ShellCasing : MonoBehaviour
{
    [SerializeField] float drag = 0.8f;       // ชะลอความเร็วต่อวินาที (0 = ลอยเร็วคงที่เหมือนอวกาศจริง)
    [SerializeField] float maxSpin = 540f;    // องศา/วินาที
    [SerializeField] float fadeTime = 0.6f;   // ช่วงท้ายอายุที่ค่อย ๆ จาง

    SpriteRenderer[] renderers;
    Color[] baseColors;
    Vector2 vel;
    float spin, age, life;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
    }

    public void Launch(Vector2 startPos, Vector2 velocity, float lifetime)
    {
        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        vel = velocity;
        spin = Random.Range(-maxSpin, maxSpin);
        age = 0f;
        life = lifetime;
        SetAlpha(1f);
    }

    void Update()
    {
        if (life <= 0f) return;

        float dt = Time.deltaTime;
        age += dt;

        vel *= Mathf.Max(0f, 1f - drag * dt);
        transform.position += (Vector3)(vel * dt);
        transform.Rotate(0f, 0f, spin * dt);

        float remaining = life - age;
        if (remaining < fadeTime)
            SetAlpha(Mathf.Clamp01(remaining / Mathf.Max(0.01f, fadeTime)));
    }

    void SetAlpha(float a)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = baseColors[i];
            c.a = baseColors[i].a * a;
            renderers[i].color = c;
        }
    }
}