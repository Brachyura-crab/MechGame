using System.Collections.Generic;
using UnityEngine;
using EnemyV2;

/// ตอน Gate ชาร์จ: ชั้นภาพยุบหดเข้าหาจุดกลางทีละชั้น จากในสุดไปนอกสุด
/// วางบน GameObject เดียวกับ Gate  (ไม่ต้องแก้ GateRiftLayers / GateSmoke)
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Gate))]
public class GateCollapse : MonoBehaviour
{
    [Tooltip("ลำดับยุบ จากในสุด -> นอกสุด แต่ละช่องคือคำที่อยู่ในชื่อ GameObject ลูก (ไม่สนตัวพิมพ์) ใส่หลายคำคั่นด้วย , จะยุบพร้อมกัน")]
    [SerializeField] string[] steps = { "eye", "blob", "small", "mid", "big", "crack,smoke" };

    [Tooltip("สัดส่วนของช่วงชาร์จที่ใช้ยุบทั้งหมด 1 = ยุบตลอดช่วงชาร์จ จบพอดีตอนระเบิด")]
    [Range(0.2f, 1f)] [SerializeField] float span = 1f;

    [Tooltip("ความเหลื่อมเวลาระหว่างชั้น 0 = ยุบทีละชั้นเสร็จแล้วค่อยชั้นถัดไป, มาก = หลายชั้นยุบซ้อนกัน")]
    [Range(0f, 0.9f)] [SerializeField] float overlap = 0.35f;

    [Tooltip("1 = หดสม่ำเสมอ, มากกว่า 1 = ช้าตอนแรกแล้วเร่งเหมือนถูกดูด")]
    [Range(0.5f, 5f)] [SerializeField] float curve = 2f;

    [Tooltip("ตำแหน่งชั้นถูกดึงเข้าหาจุดกลางของ Gate ด้วย")]
    [SerializeField] bool pullToCenter = true;

    [Tooltip("องศาที่หมุนระหว่างยุบ (0 = ไม่หมุน)")]
    [SerializeField] float spinDegrees = 60f;

    class Item
    {
        public Transform tr;
        public SpriteRenderer sr;
        public int step;
        public bool init;
        public Vector3 baseScale, lastScale, basePos, lastPos;
        public Quaternion baseRot, lastRot;
    }

    readonly List<Item> items = new List<Item>();
    Gate gate;
    bool scanned, scannedCharging;

    void Awake() => gate = GetComponent<Gate>();

    void LateUpdate()
    {
        if (gate == null) return;

        if (!scanned || (gate.State == GateState.Charging && !scannedCharging)) Scan();
        if (items.Count == 0) return;

        float p = gate.State == GateState.Closed ? 1f : gate.ChargeRatio;

        int n = Mathf.Max(1, steps.Length);
        float L = span / ((n - 1) * (1f - overlap) + 1f);

        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it.tr == null) continue;

            float s = it.step * L * (1f - overlap);
            float t = Mathf.Clamp01((p - s) / L);
            float f = 1f - Mathf.Pow(t, curve);        // 1 = ปกติ, 0 = หายสนิท
            Apply(it, f, t);
        }
    }

    void Apply(Item it, float f, float t)
    {
        var tr = it.tr;

        if (!it.init)
        {
            it.baseScale = tr.localScale;
            it.basePos = tr.localPosition;
            it.baseRot = tr.localRotation;
            it.init = true;
        }
        else
        {
            // ถ้าสคริปต์อื่น (เช่น GateRiftLayers / GateSmoke) เขียนค่าใหม่ทับ ให้ถือเป็นค่าฐานใหม่
            if (tr.localScale != it.lastScale) it.baseScale = tr.localScale;
            if (tr.localPosition != it.lastPos) it.basePos = tr.localPosition;
            if (Quaternion.Angle(tr.localRotation, it.lastRot) > 0.01f) it.baseRot = tr.localRotation;
        }

        float k = Mathf.Max(f, 0f);
        Vector3 scale = it.baseScale * k;
        Vector3 pos = pullToCenter ? it.basePos * k : it.basePos;
        Quaternion rot = it.baseRot * Quaternion.Euler(0f, 0f, spinDegrees * t);

        tr.localScale = scale;
        tr.localPosition = pos;
        tr.localRotation = rot;

        it.lastScale = scale;
        it.lastPos = pos;
        it.lastRot = rot;

        if (it.sr != null) it.sr.enabled = f > 0.002f;
    }

    void Scan()
    {
        items.Clear();
        scanned = true;
        if (gate.State == GateState.Charging) scannedCharging = true;

        int n = steps.Length;
        var found = new bool[n];

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.transform == transform) continue;
            string nm = sr.name.ToLowerInvariant();

            for (int si = 0; si < n; si++)
            {
                if (string.IsNullOrEmpty(steps[si])) continue;
                bool hit = false;
                foreach (var raw in steps[si].Split(','))
                {
                    string kw = raw.Trim().ToLowerInvariant();
                    if (kw.Length > 0 && nm.Contains(kw)) { hit = true; break; }
                }
                if (!hit) continue;

                items.Add(new Item { tr = sr.transform, sr = sr, step = si });
                found[si] = true;
                break;
            }
        }

        for (int si = 0; si < n; si++)
            if (!found[si])
                Debug.LogWarning($"GateCollapse: ไม่พบชั้นที่ชื่อมี '{steps[si]}' (ตรวจชื่อลูกของ Gate ว่าตรงกับคำใน Steps)", this);
    }
}