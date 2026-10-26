using System.Collections.Generic;
using UnityEngine;

// ส่วนที่สกิลระเบิดใช้ร่วมกัน: หาเป้าในรัศมี + สร้างภาพระเบิด
// (ลำดับ "ติดสถานะ / ดาเมจ / ผลัก" ยังอยู่ในสกิลของตัวเอง เพราะแต่ละสกิลลำดับไม่เหมือนกัน)
public static class AreaDamage
{
    static readonly Collider2D[] buffer = new Collider2D[128];

    // หา Health ที่ยังมีชีวิตในรัศมี ไม่ซ้ำ (ศัตรูหนึ่งตัวมีหลาย Collider) ใส่ลง results คืนจำนวนที่ได้
    public static int Collect(Vector2 point, float radius, ContactFilter2D filter, List<Health> results)
    {
        results.Clear();
        int n = Physics2D.OverlapCircle(point, radius, filter, buffer);
        for (int i = 0; i < n; i++)
        {
            Health h = buffer[i].GetComponentInParent<Health>();
            if (h == null || h.IsDead || results.Contains(h)) continue;
            results.Add(h);
        }
        return results.Count;
    }

    // ภาพระเบิดชั่วคราว ขนาดตามรัศมีจริง (visualScale 1 = เท่ากัน) ไม่ใส่ prefab = ไม่ทำอะไร
    public static void SpawnBlastFx(GameObject prefab, Vector2 point, float radius, float visualScale, float lifetime)
    {
        if (prefab == null) return;
        GameObject fx = Object.Instantiate(prefab, point, Quaternion.identity);
        fx.transform.localScale = Vector3.one * radius * 2f * visualScale;
        Object.Destroy(fx, lifetime);
    }
}