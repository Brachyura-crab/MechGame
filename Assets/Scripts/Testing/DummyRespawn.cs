using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class DummyRespawn : MonoBehaviour
{
    [SerializeField] GameObject body;        // ลูกที่มี sprite, collider, แถบ HP
    [SerializeField] float respawnDelay = 3f;

    Health health;
    StatusController status;
    Rigidbody2D rb;
    Vector3 startPos;

    void Awake()
    {
        health = GetComponent<Health>();
        status = GetComponent<StatusController>();
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }
    void OnEnable() => health.Died += OnDied;
    void OnDisable() => health.Died -= OnDied;

    void OnDied() => StartCoroutine(Respawn());

    IEnumerator Respawn()
    {
        body.SetActive(false);
        yield return new WaitForSeconds(respawnDelay);
        if (status != null) status.ClearAll();
        transform.position = startPos;   // กลับที่เดิมหลังโดนผลัก
        if (rb != null) rb.linearVelocity = Vector2.zero;
        health.ResetFull();
        body.SetActive(true);
    }
}