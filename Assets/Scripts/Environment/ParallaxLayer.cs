using UnityEngine;

// ชั้นฉากหลังแบบ parallax ต่อกันไม่รู้จบ: ใช้ sprite ซ้ำเป็นตาราง (กี่ชิ้นก็ได้ตามขนาดจอ) แล้วย้ายชิ้นตามกล้อง
//   parallax = ชั้นนี้ "ขยับบนจอ" กี่เปอร์เซ็นต์ของโลกจริง  0.1 = ไกลมาก (แทบนิ่ง)  1 = เท่าโลก
// วางบน GameObject ว่างหนึ่งชั้นต่อหนึ่งตัว  ชิ้น sprite ถูกสร้างเป็นลูกเองตอนเริ่ม
// ทำงานหลัง Cinemachine (ExecutionOrder สูง) กันชั้นสั่น
[DefaultExecutionOrder(10000)]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] Sprite sprite;
    [SerializeField, Range(0f, 1f)] float parallax = 0.3f;
    [SerializeField] Material material;          // ว่าง = Sprite-Lit-Default (โดนแสง 2D)  ใส่ Sprite-Unlit-Default ถ้าอยากให้ไม่โดนแสง
    [SerializeField] Color tint = Color.white;
    [SerializeField] string sortingLayer = "Default";
    [SerializeField] int sortingOrder = -100;    // ต่ำ = อยู่หลัง
    [SerializeField] Camera cam;                 // ว่าง = Camera.main

    Transform[] tiles;
    int rx, ry;
    float tw, th;

    void Start()
    {
        if (sprite == null) { Debug.LogError("ParallaxLayer: ต้องใส่ Sprite", this); enabled = false; return; }
        if (cam == null) cam = Camera.main;
        if (cam == null) { Debug.LogError("ParallaxLayer: ไม่พบกล้อง", this); enabled = false; return; }

        Vector3 size = sprite.bounds.size;
        tw = size.x * transform.lossyScale.x;
        th = size.y * transform.lossyScale.y;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        rx = Mathf.Max(1, Mathf.CeilToInt(halfW / tw));
        ry = Mathf.Max(1, Mathf.CeilToInt(halfH / th));

        int nx = rx * 2 + 1, ny = ry * 2 + 1;
        tiles = new Transform[nx * ny];
        for (int i = 0; i < tiles.Length; i++)
        {
            var go = new GameObject("tile");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            if (material != null) sr.sharedMaterial = material;
            tiles[i] = go.transform;
        }
    }

    void LateUpdate()
    {
        if (tiles == null) return;

        Vector3 c = cam.transform.position;
        float follow = 1f - parallax;                  // ชั้นถูกพาไปกับกล้องมากแค่ไหน
        Vector2 anchor = new Vector2(c.x * follow, c.y * follow);

        // ช่องที่ใกล้กล้องที่สุดในพิกัดของชั้นนี้ แล้ววางกริดรอบช่องนั้น
        int cx = Mathf.RoundToInt((c.x - anchor.x) / tw);
        int cy = Mathf.RoundToInt((c.y - anchor.y) / th);

        int idx = 0;
        for (int j = -ry; j <= ry; j++)
            for (int i = -rx; i <= rx; i++)
                tiles[idx++].position = new Vector3(anchor.x + (cx + i) * tw, anchor.y + (cy + j) * th, transform.position.z);
    }
}