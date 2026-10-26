using System;
using UnityEngine;

// ค่าความดัง Master/Music/Sfx ที่ผู้เล่นตั้ง (เก็บใน PlayerPrefs) ให้หน้า Settings เรียกใช้
public static class AudioPrefs
{
    const string KMaster = "aud_master", KMusic = "aud_music", KSfx = "aud_sfx";

    static float master = 1f, music = 0.8f, sfx = 1f;
    static bool loaded;

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => loaded = false;

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        master = PlayerPrefs.GetFloat(KMaster, 1f);
        music = PlayerPrefs.GetFloat(KMusic, 0.8f);
        sfx = PlayerPrefs.GetFloat(KSfx, 1f);
    }

    public static float Get(AudioBus bus)
    {
        EnsureLoaded();
        switch (bus)
        {
            case AudioBus.Music: return music;
            case AudioBus.Sfx: return sfx;
            default: return master;
        }
    }

    // มีผลทันที ตอนออกจากหน้า Settings ให้เรียก Save()
    public static void Set(AudioBus bus, float v)
    {
        EnsureLoaded();
        v = Mathf.Clamp01(v);
        switch (bus)
        {
            case AudioBus.Music: music = v; PlayerPrefs.SetFloat(KMusic, v); break;
            case AudioBus.Sfx: sfx = v; PlayerPrefs.SetFloat(KSfx, v); break;
            default: master = v; PlayerPrefs.SetFloat(KMaster, v); break;
        }
        Changed?.Invoke();
    }

    public static void Save() => PlayerPrefs.Save();
}