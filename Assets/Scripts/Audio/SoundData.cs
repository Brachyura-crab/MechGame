using UnityEngine;

public enum AudioBus { Master, Music, Sfx }

// เสียงหนึ่งอัน (Create > Audio > Sound Data)
[CreateAssetMenu(menuName = "Audio/Sound Data", fileName = "Snd_")]
public class SoundData : ScriptableObject
{
    [Header("Clips (ใส่ได้หลายอัน ระบบสุ่ม ไม่เลือกซ้ำตัวเดิมติดกัน)")]
    public AudioClip[] clips;

    [Header("ช่องเสียง (Sfx หรือ Music)")]
    public AudioBus bus = AudioBus.Sfx;

    [Header("ความดัง / pitch")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.5f)] public float volumeJitter = 0.05f;
    [Range(0.5f, 2f)] public float pitchMin = 0.95f;
    [Range(0.5f, 2f)] public float pitchMax = 1.05f;

    [Header("เสียงในโลกเกม (ดังเบาตามระยะจากกล้อง)")]
    public bool spatial;
    public float minDistance = 12f;   // กล้องอยู่ z=-10 จึงมีระยะ 10 บวกเข้าไปแล้ว อย่าตั้งต่ำกว่า 10
    public float maxDistance = 45f;

    [Header("จำกัดเสียงซ้อน")]
    public int maxVoices = 4;          // เสียงนี้ดังพร้อมกันได้กี่ตัว (0 = ไม่จำกัด)
    public float minInterval = 0.03f;  // ห่างจากครั้งก่อนอย่างน้อยกี่วินาที

    [Header("อื่นๆ")]
    public bool ignorePause;           // ดังต่อแม้เกมหยุด (เสียง UI, เสียงหน้าสรุปผล)
    public bool loop;                  // ติ๊กสำหรับเพลง

    int lastIndex = -1;

    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        int i;
        do { i = Random.Range(0, clips.Length); } while (i == lastIndex);
        lastIndex = i;
        return clips[i];
    }
}