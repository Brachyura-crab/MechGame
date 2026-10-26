using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyV2
{
    public enum GateState { Active, Charging, Closed }

    // gate: พลังงาน + สถานะ + เช็คระยะผู้เล่น  ไม่เสกศัตรู ไม่ทำดาเมจระเบิด ไม่ถือภาพ (แยกเป็น camp / GateBlast / GateVisual)
    // เป็นผู้สั่งผ่าน LinkSource ที่ติดบน object เดียวกัน: ปิดแล้ว Deactivate และลบ gate ทิ้ง
    [RequireComponent(typeof(LinkSource))]
    public class Gate : MonoBehaviour
    {
        public static readonly List<Gate> All = new List<Gate>();

        // gate ตัวใดตัวหนึ่งปิด (ก่อนถูกลบ) ภารกิจใช้ฟังเพื่อนับ เพราะ gate ที่ปิดแล้วจะหายจาก All
        public static event Action<Gate> AnyClosed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
            AnyClosed = null;
        }

        [SerializeField] GateData data;

        public GateData Data => data;
        public LinkSource Source { get; private set; }
        public GateState State { get; private set; } = GateState.Active;
        public float Energy { get; private set; }
        public float MaxEnergy => data.maxEnergy;
        public float Ratio => data.maxEnergy > 0f ? Energy / data.maxEnergy : 0f;
        public bool PlayerInRange { get; private set; }
        public float ChargeRatio => State == GateState.Charging && data.chargeTime > 0f
            ? Mathf.Clamp01(chargeTimer / data.chargeTime) : 0f;

        public event Action<float, float> EnergyChanged;   // (ปัจจุบัน, สูงสุด)
        public event Action ChargeStarted;                 // พลังงานหมด เริ่มชาร์จ
        public event Action Exploded;                      // ชาร์จจบ (ก่อนปิด) GateBlast ใช้ทำระเบิด / camp ใช้เรียก final wave
        public event Action Closed;                        // ปิดแล้ว (ก่อนถูกลบ)

        float chargeTimer;

        void Awake()
        {
            Source = GetComponent<LinkSource>();

            if (data == null)
            {
                Debug.LogError("Gate: ต้องใส่ GateData", this);
                enabled = false;
                return;
            }
            Energy = data.maxEnergy;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // ---------- พลังงาน ----------

        // เรียกจาก CampMember ตอนศัตรูของ gate นี้ตายจริง
        public void OnMemberKilled(CampMember m)
        {
            if (State != GateState.Active) return;
            if (!PlayerInRange) return;            // กันฟาร์ม: ผู้เล่นอยู่นอกระยะ ฆ่าแล้วไม่ลด
            Drain(m.Weight);
        }

        public void Drain(float amount)
        {
            if (State != GateState.Active || amount <= 0f) return;

            Energy = Mathf.Max(0f, Energy - amount);
            EnergyChanged?.Invoke(Energy, data.maxEnergy);

            if (Energy <= 0f) BeginCharge();
        }

        void BeginCharge()
        {
            State = GateState.Charging;
            chargeTimer = 0f;
            ChargeStarted?.Invoke();
        }

        void Close()
        {
            State = GateState.Closed;
            Source.Deactivate();                   // ผู้ตามทั้งหมดกลายเป็นไม่มีเจ้านาย
            Closed?.Invoke();
            AnyClosed?.Invoke(this);
            Destroy(gameObject, data.destroyDelay);   // ไม่ทิ้งซาก
        }

        // ---------- Update ----------

        void Update()
        {
            UpdateRange();

            if (State == GateState.Charging)
            {
                chargeTimer += Time.deltaTime;
                if (chargeTimer >= data.chargeTime)
                {
                    Exploded?.Invoke();
                    Close();
                }
            }
        }

        void UpdateRange()
        {
            if (!EnemyTarget.Ensure()) { PlayerInRange = false; return; }

            float d = Vector2.Distance(transform.position, EnemyTarget.Transform.position);
            PlayerInRange = d <= data.activeRadius + (PlayerInRange ? data.exitBuffer : 0f);
        }

        void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, data.activeRadius);
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, data.activeRadius + data.exitBuffer);
        }
    }
}