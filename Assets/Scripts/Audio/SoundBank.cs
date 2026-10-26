using UnityEngine;

// รวมเสียงที่ใช้ทั่วเกม (Create > Audio > Sound Bank ตั้งชื่อ "SoundBank" วางใน Assets/Resources)
[CreateAssetMenu(menuName = "Audio/Sound Bank", fileName = "SoundBank")]
public class SoundBank : ScriptableObject
{
    [Header("UI")]
    public SoundData uiClick;      // กดปุ่มทุกปุ่ม (ผูกให้แล้ว)
    public SoundData uiHover;      // ชี้ปุ่มเมนูหลัก (ผูกให้แล้ว)
    public SoundData uiConfirm;    // CONFIRM UNIT / DEPLOY (ยังไม่ผูก)
    public SoundData uiBack;       // BACK (ยังไม่ผูก)
    public SoundData uiUpgrade;    // ซื้ออัปเกรดสำเร็จ (ยังไม่ผูก)
    public SoundData uiDenied;     // กดแล้วทำไม่ได้ (ยังไม่ผูก)

    [Header("Game (ผูกให้แล้วผ่าน GameAudioEvents)")]
    public SoundData playerDeath;
    public SoundData swarmDeath;
    public SoundData leaderDeath;
    public SoundData gateClosed;
    public SoundData missionComplete;
    public SoundData missionFailed;
}

// ตัวช่วยเรียกเสียงใน SoundBank จากที่ไหนก็ได้ (ช่องว่างไม่มีผลอะไร ไม่ error)
public static class GameAudio
{
    static SoundBank bank;
    static bool tried;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { bank = null; tried = false; }

    public static SoundBank Bank
    {
        get
        {
            if (!tried) { tried = true; bank = Resources.Load<SoundBank>("SoundBank"); }
            return bank;
        }
    }

    public static void UiClick()   { if (Bank != null) AudioManager.Play(Bank.uiClick); }
    public static void UiHover()   { if (Bank != null) AudioManager.Play(Bank.uiHover); }
    public static void UiConfirm() { if (Bank != null) AudioManager.Play(Bank.uiConfirm); }
    public static void UiBack()    { if (Bank != null) AudioManager.Play(Bank.uiBack); }
    public static void UiUpgrade() { if (Bank != null) AudioManager.Play(Bank.uiUpgrade); }
    public static void UiDenied()  { if (Bank != null) AudioManager.Play(Bank.uiDenied); }
}