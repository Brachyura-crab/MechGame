using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class DummyRespawn : MonoBehaviour
{
    [SerializeField] GameObject body;        // ลูกที่มี sprite, collider, แถบ HP
    [SerializeField] float respawnDelay = 3f;

    Health health;

    void Awake() => health = GetComponent<Health>();
    void OnEnable() => health.Died += OnDied;
    void OnDisable() => health.Died -= OnDied;

    void OnDied() => StartCoroutine(Respawn());

    IEnumerator Respawn()
    {
        body.SetActive(false);
        yield return new WaitForSeconds(respawnDelay);
        health.ResetFull();
        body.SetActive(true);
    }
}