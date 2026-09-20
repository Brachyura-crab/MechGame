using UnityEngine;

[DefaultExecutionOrder(-100)] // ให้สร้าง StatBlock ก่อนสคริปต์อื่น
public class MechStats : MonoBehaviour
{
    [SerializeField] MechData data;

    public StatBlock Stats { get; private set; }

    void Awake()
    {
        Stats = new StatBlock();
        data.ApplyTo(Stats);
    }
}