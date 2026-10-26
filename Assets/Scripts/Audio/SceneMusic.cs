using UnityEngine;

// เล่นเพลงของ scene นี้ตอนเริ่ม (crossfade กับเพลงก่อนหน้า)
public class SceneMusic : MonoBehaviour
{
    [SerializeField] SoundData music;
    [SerializeField] float fadeSeconds = 1f;
    [SerializeField] bool stopIfEmpty;   // ช่องเพลงว่างแล้วอยากให้หยุดเพลงเดิมไหม

    void Start()
    {
        if (music != null) AudioManager.PlayMusic(music, fadeSeconds);
        else if (stopIfEmpty) AudioManager.StopMusic(fadeSeconds);
    }
}