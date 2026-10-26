using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    // ลำดับของภารกิจ: Mission (ปิด gate) → Extract (กลับจุดรับ) → Complete / Failed  (ยังไม่มีเฟส Deploy ของยานส่ง)
    public enum MissionPhase { Mission, Extract, Complete, Failed }

    // ข้อมูล gate สำหรับ map/minimap: gate ที่ปิดแล้วถูกลบออกจากฉาก แต่ตำแหน่งและสถานะยังอยู่ที่นี่
    public struct GateInfo
    {
        public Vector2 position;
        public bool closed;
    }

    // วางจุด gate ด้วย seed รอบจุดส่ง, นับ gate ที่ปิด, คุมเฟส, ตัดจบเมื่อผู้เล่นออกนอกพื้นที่, ชนะเมื่อกลับจุดรับ
    [DefaultExecutionOrder(-200)]
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [SerializeField] MissionData data;
        [SerializeField] Gate gatePrefab;       // ว่าง = ใช้ Gate ที่วางในฉาก แล้วย้ายตำแหน่ง
        [SerializeField] Transform origin;      // จุดส่ง = จุดรับ (ว่าง = ตำแหน่งของ object นี้)
        [SerializeField] int seed = 0;          // 0 = สุ่มใหม่ทุกรอบ ค่าอื่น = แมพเดิมทุกครั้ง

        readonly List<Gate> gates = new List<Gate>();
        readonly List<GateInfo> infos = new List<GateInfo>();
        int closedCount;
        float holdTimer;
        float abandonTimer;

        // ---------- ข้อมูลให้ UI / map / minimap อ่าน ----------
        public MissionPhase Phase { get; private set; } = MissionPhase.Mission;
        public int Seed { get; private set; }
        public int Total => infos.Count;
        public int ClosedCount => closedCount;
        public IReadOnlyList<GateInfo> GateInfos => infos;
        public Vector2 Origin => origin != null ? (Vector2)origin.position : (Vector2)transform.position;
        public float ExtractRadius => data.extractRadius;
        public float WarnRadius => data.warnRadius;
        public float ExtractProgress => data.extractHold > 0f ? Mathf.Clamp01(holdTimer / data.extractHold) : 0f;
        public bool OutOfArea { get; private set; }
        public float AbandonRemaining => Mathf.Max(0f, abandonTimer);

        public event Action<MissionPhase> PhaseChanged;
        public event Action<int, int> GateClosedChanged;   // (ปิดแล้ว, ทั้งหมด)

        void Awake()
        {
            Instance = this;

            var node = GameSession.SelectedMission;
            if (node != null && node.mission != null)
            {
                data = Instantiate(node.mission);      // ทำสำเนา จะได้ไม่เขียนทับ asset ต้นฉบับ
                data.gateCount = node.gatesToClose;
            }

            if (data == null)
            {
                Debug.LogError("MissionManager: ต้องใส่ MissionData", this);
                enabled = false;
                return;
            }

            Seed = seed != 0 ? seed : UnityEngine.Random.Range(1, int.MaxValue);
            var rng = new System.Random(Seed);

            abandonTimer = data.abandonCountdown;
            BuildGates(GeneratePoints(rng, Origin, data.gateCount));

            Gate.AnyClosed += OnGateClosed;
        }

        void OnDestroy()
        {
            Gate.AnyClosed -= OnGateClosed;
            if (Instance == this) Instance = null;
        }

        // ---------- วางจุด ----------

        List<Vector2> GeneratePoints(System.Random rng, Vector2 center, int count)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                bool placed = false;
                for (int t = 0; t < 60 && !placed; t++)
                {
                    float ang = (float)(rng.NextDouble() * Mathf.PI * 2f);
                    float dist = Mathf.Lerp(data.minDistance, data.maxDistance, (float)rng.NextDouble());
                    Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                    if (FarEnough(pts, p)) { pts.Add(p); placed = true; }
                }

                if (!placed)   // สุ่มไม่ผ่าน: วางกระจายเท่าๆ กันรอบจุดส่ง
                {
                    float ang = Mathf.PI * 2f * i / Mathf.Max(1, count);
                    pts.Add(center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * data.maxDistance);
                }
            }
            return pts;
        }

        bool FarEnough(List<Vector2> pts, Vector2 p)
        {
            float min = data.minGateSpacing;
            for (int i = 0; i < pts.Count; i++)
                if ((pts[i] - p).sqrMagnitude < min * min) return false;
            return true;
        }

        void BuildGates(List<Vector2> points)
        {
            if (gatePrefab != null)
            {
                for (int i = 0; i < points.Count; i++)
                    gates.Add(Instantiate(gatePrefab, points[i], Quaternion.identity));
            }
            else
            {
                Gate[] found = FindObjectsByType<Gate>();
                if (found.Length < points.Count)
                    Debug.LogWarning("MissionManager: Gate ในฉากมี " + found.Length + " ตัว น้อยกว่า gateCount (" + points.Count + ")", this);

                for (int i = 0; i < found.Length; i++)
                {
                    if (i < points.Count)
                    {
                        found[i].transform.position = points[i];
                        gates.Add(found[i]);
                    }
                    else found[i].gameObject.SetActive(false);   // เกินจำนวนที่ต้องการ
                }
            }

            for (int i = 0; i < gates.Count; i++)
                infos.Add(new GateInfo { position = gates[i].transform.position, closed = false });

            if (gates.Count == 0) Debug.LogWarning("MissionManager: ไม่มี gate เลย", this);
        }

        // gate ตัวใดตัวหนึ่งปิด (ก่อนถูกลบ) นับเฉพาะ gate ของภารกิจนี้
        void OnGateClosed(Gate g)
        {
            int i = gates.IndexOf(g);
            if (i < 0 || infos[i].closed) return;

            infos[i] = new GateInfo { position = infos[i].position, closed = true };
            closedCount++;
            GateClosedChanged?.Invoke(closedCount, infos.Count);

            if (closedCount >= infos.Count && Phase == MissionPhase.Mission)
                SetPhase(MissionPhase.Extract);
        }

        // ---------- วงจร ----------

        void Update()
        {
            if (Phase == MissionPhase.Complete || Phase == MissionPhase.Failed) return;

            RunManager rm = RunManager.Instance;
            if (rm != null && rm.State != RunManager.RunState.Playing) return;   // ตาย/หมดเวลา/จบแล้ว

            if (!EnemyTarget.Ensure()) return;
            Health ph = EnemyTarget.PlayerHealth;
            if (ph != null && ph.IsDead) return;

            float d = Vector2.Distance(EnemyTarget.Transform.position, Origin);
            float dt = Time.deltaTime;

            // ออกนอกพื้นที่ภารกิจ: นับถอยหลัง ครบ = ล้มเหลว กลับเข้าพื้นที่ = รีเซ็ต
            OutOfArea = d > data.warnRadius;
            if (OutOfArea)
            {
                abandonTimer -= dt;
                if (abandonTimer <= 0f) { Finish(false); return; }
            }
            else abandonTimer = data.abandonCountdown;

            // จุดรับ: ปิด gate ครบแล้ว อยู่ในโซนครบเวลา = ชนะ (ออกจากโซน = นับใหม่)
            if (Phase == MissionPhase.Extract)
            {
                holdTimer = d <= data.extractRadius ? holdTimer + dt : 0f;
                if (holdTimer >= data.extractHold) Finish(true);
            }
        }

        void SetPhase(MissionPhase p)
        {
            if (Phase == p) return;
            Phase = p;
            PhaseChanged?.Invoke(p);
        }

        void Finish(bool success)
        {
            SetPhase(success ? MissionPhase.Complete : MissionPhase.Failed);

            RunManager rm = RunManager.Instance;
            if (rm == null) return;
            if (success) rm.Win();
            else rm.Fail();
        }

        void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Vector3 o = Origin;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(o, data.extractRadius);
            Gizmos.color = new Color(1f, 0.9f, 0.2f);
            Gizmos.DrawWireSphere(o, data.minDistance);
            Gizmos.DrawWireSphere(o, data.maxDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(o, data.warnRadius);
        }
    }
}