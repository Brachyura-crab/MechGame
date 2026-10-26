using UnityEngine;

[DefaultExecutionOrder(-100)] // ให้สร้าง StatBlock ก่อนสคริปต์อื่น
public class MechStats : MonoBehaviour
{
    [SerializeField] StatProfile data;

    public StatBlock Stats { get; private set; }
        public MechData Data => data as MechData;   // ศัตรูใช้ EnemyStatData จึงคืน null (หน้า stat ของผู้เล่นใช้กับผู้เล่นเท่านั้น)

    void Awake()
    {
        Stats = new StatBlock();
        data.ApplyTo(Stats);
        if (data is MechData) UpgradeApplier.Apply(Stats, this);   // เฉพาะผู้เล่น ศัตรูใช้ EnemyStatData
    }
}