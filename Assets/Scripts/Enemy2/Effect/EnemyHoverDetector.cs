using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyHoverDetector : MonoBehaviour
{
    [SerializeField] LayerMask enemyLayers;
    [SerializeField] float checkInterval = 0.05f;

    Camera cam;
    float timer;

    void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        if (cam == null) cam = Camera.main;
        if (cam == null || Mouse.current == null) return;

        Vector2 world = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(world, enemyLayers);
        if (hit == null) return;

        EnemyHpBarVisibility v = hit.GetComponentInParent<EnemyHpBarVisibility>();
        if (v != null) v.Hover();
    }
}