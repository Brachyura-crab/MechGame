using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DummyPatrol : MonoBehaviour
{
    [SerializeField] Vector2 direction = Vector2.right;
    [SerializeField] float speed = 4f;
    [SerializeField] float halfDistance = 6f;

    Rigidbody2D rb;
    Vector2 origin;
    float sign = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        origin = transform.position;
    }

    void FixedUpdate()
    {
        Vector2 dir = direction.normalized;
        float offset = Vector2.Dot((Vector2)transform.position - origin, dir);
        if (offset > halfDistance) sign = -1f;
        else if (offset < -halfDistance) sign = 1f;

        rb.linearVelocity = dir * speed * sign;
    }
}