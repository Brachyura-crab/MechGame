using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] float lifetime = 0.8f;
    [SerializeField] float riseSpeed = 1.5f;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color critColor = new Color(1f, 0.85f, 0.1f);
    [SerializeField] float critScale = 1.4f;

    float t;
    Color baseColor;

    public void Init(float amount, bool crit)
    {
        int value = Mathf.RoundToInt(amount);
        text.text = crit ? value + "!" : value.ToString();
        baseColor = crit ? critColor : normalColor;
        text.color = baseColor;
        if (crit) transform.localScale *= critScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        Color c = baseColor;
        c.a = 1f - Mathf.Clamp01(t / lifetime);
        text.color = c;

        if (t >= lifetime) Destroy(gameObject); // ตอนมีศัตรูเยอะค่อยเปลี่ยนเป็น pool
    }
}