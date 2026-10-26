using UnityEngine;

namespace EnemyV2
{
    // ภาพของ gate (ไม่บังคับ): แถบพลังงาน, สีตามสถานะ, วงรัศมีระเบิดที่ขยายตามเวลาชาร์จ  ทุกช่องเว้นว่างได้
    [RequireComponent(typeof(Gate))]
    public class GateVisual : MonoBehaviour
    {
        [Header("แถบพลังงาน")]
        [SerializeField] Transform energyFill;          // sprite แถบพลังงาน (pivot กลาง)
        [SerializeField] float fillWidth = 3f;
        [SerializeField] float fillHeight = 0.25f;

        [Header("สีตามสถานะ")]
        [SerializeField] SpriteRenderer tint;
        [SerializeField] Color activeColor = Color.white;
        [SerializeField] Color shieldedColor = new Color(0.6f, 0.6f, 0.7f, 1f);   // ผู้เล่นอยู่นอกระยะ (ฆ่าแล้วไม่ลดพลังงาน)
        [SerializeField] Color chargingColor = new Color(1f, 0.4f, 0.3f, 1f);

        [Header("วงรัศมีระเบิดตอนชาร์จ (ต้องมี GateBlast บน object เดียวกัน)")]
        [SerializeField] Transform blastRing;
        [SerializeField] Transform blastFill;           // ขยายตามเวลาชาร์จ
        [SerializeField] float indicatorSpriteSize = 1f;   // เส้นผ่านศูนย์กลาง sprite ที่ scale 1 (หน่วย world)

        Gate gate;
        GateBlast blast;

        void Awake()
        {
            gate = GetComponent<Gate>();
            TryGetComponent(out blast);
        }

        void OnEnable()
        {
            gate.EnergyChanged += OnEnergyChanged;
            gate.ChargeStarted += OnChargeStarted;
        }

        void OnDisable()
        {
            gate.EnergyChanged -= OnEnergyChanged;
            gate.ChargeStarted -= OnChargeStarted;
        }

        void Start()
        {
            RefreshFill();
            SetIndicators(false);
        }

        void OnEnergyChanged(float cur, float max) => RefreshFill();
        void OnChargeStarted() => SetIndicators(true);

        void SetIndicators(bool on)
        {
            if (blastRing != null) blastRing.gameObject.SetActive(on);
            if (blastFill != null) blastFill.gameObject.SetActive(on);
        }

        void RefreshFill()
        {
            if (energyFill == null) return;

            float ratio = Mathf.Clamp01(gate.Ratio);
            energyFill.localScale = new Vector3(fillWidth * ratio, fillHeight, 1f);
            energyFill.localPosition = new Vector3(-fillWidth * 0.5f + fillWidth * ratio * 0.5f, energyFill.localPosition.y, 0f);
        }

        void Update()
        {
            UpdateIndicators();
            UpdateTint();
        }

        void UpdateIndicators()
        {
            if (gate.State != GateState.Charging || blast == null) return;

            float size = blast.Radius * 2f / Mathf.Max(0.01f, indicatorSpriteSize);
            if (blastRing != null) blastRing.localScale = new Vector3(size, size, 1f);
            if (blastFill != null)
            {
                float s = size * gate.ChargeRatio;
                blastFill.localScale = new Vector3(s, s, 1f);
            }
        }

        void UpdateTint()
        {
            if (tint == null) return;

            Color c;
            if (gate.State == GateState.Charging)
                c = Color.Lerp(chargingColor, Color.white, Mathf.PingPong(Time.time * 6f, 1f) * 0.5f);
            else
                c = gate.PlayerInRange ? activeColor : shieldedColor;
            tint.color = c;
        }
    }
}