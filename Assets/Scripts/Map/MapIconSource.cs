using System.Collections.Generic;
using UnityEngine;

public enum MapIconType { Swarm, Leader }

// ติดบน prefab ที่อยากให้ขึ้นบนแผนที่ (swarm / leader)  gate และจุดส่งดึงจาก Gate.All / MissionManager อยู่แล้ว
public class MapIconSource : MonoBehaviour
{
    public static readonly List<MapIconSource> All = new List<MapIconSource>();

    [SerializeField] MapIconType type = MapIconType.Swarm;

    public MapIconType Type => type;
    public Vector2 Position => transform.position;
    public bool IsAlive => health == null || !health.IsDead;

    Health health;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null) health = GetComponentInParent<Health>();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);
}