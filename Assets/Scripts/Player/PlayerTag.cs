using UnityEngine;

// ติดไว้บน object ผู้เล่นที่เดียว ระบบอื่น (ศัตรู ฯลฯ) หาผู้เล่นผ่านตัวนี้ ไม่ต้องรู้จัก PlayerMove
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(Health))]
public class PlayerTag : MonoBehaviour
{
    public static PlayerTag Instance { get; private set; }
    public Health Health { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
        Health = GetComponent<Health>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}