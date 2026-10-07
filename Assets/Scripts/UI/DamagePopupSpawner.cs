using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamagePopupSpawner : MonoBehaviour
{
    [SerializeField] DamagePopup popupPrefab;
    [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
    [SerializeField] float randomX = 0.4f;

    Health health;

    void Awake() => health = GetComponent<Health>();
    void OnEnable() => health.Damaged += OnDamaged;
    void OnDisable() => health.Damaged -= OnDamaged;

    void OnDamaged(DamageResult r)
    {
        Vector3 pos = transform.position + (Vector3)offset + new Vector3(Random.Range(-randomX, randomX), 0f, 0f);
        DamagePopup p = Instantiate(popupPrefab, pos, Quaternion.identity);
        p.Init(r.amount, r.isCrit);
    }
}