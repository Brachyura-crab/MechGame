using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ตัวเล่นเสียงกลาง: pool ของ AudioSource, จำกัดเสียงซ้อน, เพลง crossfade
// สร้างตัวเองอัตโนมัติ ไม่ต้องวางใน scene
public class AudioManager : MonoBehaviour
{
    const int PoolSize = 32;

    static AudioManager inst;

    class Voice
    {
        public AudioSource src;
        public SoundData sound;
        public AudioBus bus;
        public float baseVolume;
        public float startTime;
    }

    class MusicSlot
    {
        public AudioSource src;
        public SoundData sound;
        public float level;    // 0-1 ตามการ fade
        public float target;
        public float rate;     // หน่วยต่อวินาที
    }

    readonly List<Voice> voices = new List<Voice>();
    readonly Dictionary<SoundData, float> lastPlay = new Dictionary<SoundData, float>();
    readonly MusicSlot[] music = new MusicSlot[2];
    int musicCur = -1;

    // ---------- API ----------
    public static void Play(SoundData s) { if (inst != null) inst.PlayInternal(s, false, Vector2.zero); }
    public static void Play(SoundData s, Vector2 position) { if (inst != null) inst.PlayInternal(s, true, position); }
    public static void PlayMusic(SoundData s, float fadeSeconds = 1f) { if (inst != null) inst.PlayMusicInternal(s, fadeSeconds); }
    public static void StopMusic(float fadeSeconds = 1f) { if (inst != null) inst.StopMusicInternal(fadeSeconds); }
    public static void SetPaused(bool paused) => AudioListener.pause = paused;

    // ---------- เริ่มต้น ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => inst = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (inst != null) return;
        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (inst != null && inst != this) { Destroy(gameObject); return; }
        inst = this;

        for (int i = 0; i < music.Length; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = true;   // เพลงดังต่อตอน Pause
            music[i] = new MusicSlot { src = src };
        }
    }

    void OnEnable()
    {
        AudioPrefs.Changed += OnPrefs;
        SceneManager.sceneLoaded += OnScene;
        OnPrefs();
    }

    void OnDisable()
    {
        AudioPrefs.Changed -= OnPrefs;
        SceneManager.sceneLoaded -= OnScene;
    }

    void OnPrefs()
    {
        AudioListener.volume = AudioPrefs.Get(AudioBus.Master);
        for (int i = 0; i < voices.Count; i++)
        {
            var v = voices[i];
            if (v.src != null && v.src.isPlaying) v.src.volume = v.baseVolume * AudioPrefs.Get(v.bus);
        }
    }

    void OnScene(Scene scene, LoadSceneMode mode)
    {
        AudioListener.pause = false;   // กันเสียงค้างปิดหลังเปลี่ยน scene ตอน Pause อยู่
        EnsureListener();
    }

    // ต้องมี AudioListener พอดี 1 ตัว: ไม่มีก็เพิ่มให้ มีของกล้องแล้วก็ถอดของเราออก
    void EnsureListener()
    {
        var all = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
        var mine = GetComponent<AudioListener>();
        if (mine != null && all.Length > 1) Destroy(mine);
        else if (mine == null && all.Length == 0) gameObject.AddComponent<AudioListener>();
    }

    // ---------- เสียงเอฟเฟกต์ ----------
    void PlayInternal(SoundData s, bool hasPos, Vector2 pos)
    {
        if (s == null) return;
        AudioClip clip = s.PickClip();
        if (clip == null) return;

        float now = Time.unscaledTime;
        if (s.minInterval > 0f && lastPlay.TryGetValue(s, out float last) && now - last < s.minInterval) return;
        if (s.maxVoices > 0 && CountActive(s) >= s.maxVoices) return;
        lastPlay[s] = now;

        Voice v = Acquire();
        AudioBus bus = s.bus == AudioBus.Music ? AudioBus.Music : AudioBus.Sfx;

        v.sound = s;
        v.bus = bus;
        v.startTime = now;
        v.baseVolume = Mathf.Clamp01(s.volume * (1f + Random.Range(-s.volumeJitter, s.volumeJitter)));

        var src = v.src;
        src.clip = clip;
        src.loop = false;
        src.pitch = Random.Range(s.pitchMin, s.pitchMax);
        src.volume = v.baseVolume * AudioPrefs.Get(bus);
        src.ignoreListenerPause = s.ignorePause;
        src.dopplerLevel = 0f;

        if (hasPos && s.spatial)
        {
            src.transform.position = new Vector3(pos.x, pos.y, 0f);
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = s.minDistance;
            src.maxDistance = s.maxDistance;
        }
        else src.spatialBlend = 0f;

        src.Play();
    }

    int CountActive(SoundData s)
    {
        int n = 0;
        for (int i = 0; i < voices.Count; i++)
            if (voices[i].sound == s && voices[i].src.isPlaying) n++;
        return n;
    }

    Voice Acquire()
    {
        for (int i = 0; i < voices.Count; i++)
            if (!voices[i].src.isPlaying) return voices[i];

        if (voices.Count < PoolSize)
        {
            var go = new GameObject("Voice");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            var nv = new Voice { src = src };
            voices.Add(nv);
            return nv;
        }

        // เต็ม: ตัดตัวที่เล่นมานานสุด
        Voice oldest = voices[0];
        for (int i = 1; i < voices.Count; i++)
            if (voices[i].startTime < oldest.startTime) oldest = voices[i];
        oldest.src.Stop();
        return oldest;
    }

    // ---------- เพลง ----------
    void PlayMusicInternal(SoundData s, float fade)
    {
        if (s == null) return;
        AudioClip clip = s.PickClip();
        if (clip == null) return;

        // เพลงเดิมกำลังเล่นอยู่ ไม่เริ่มใหม่
        if (musicCur >= 0 && music[musicCur].sound == s && music[musicCur].target > 0f) return;

        float rate = fade > 0f ? 1f / fade : 1000f;
        if (musicCur >= 0) { music[musicCur].target = 0f; music[musicCur].rate = rate; }

        int next = musicCur == 0 ? 1 : 0;
        var m = music[next];
        m.sound = s;
        m.level = 0f; m.target = 1f; m.rate = rate;
        m.src.clip = clip;
        m.src.loop = s.loop;
        m.src.pitch = 1f;
        m.src.volume = 0f;
        m.src.Play();
        musicCur = next;
    }

    void StopMusicInternal(float fade)
    {
        if (musicCur < 0) return;
        music[musicCur].target = 0f;
        music[musicCur].rate = fade > 0f ? 1f / fade : 1000f;
        musicCur = -1;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float bus = AudioPrefs.Get(AudioBus.Music);
        for (int i = 0; i < music.Length; i++)
        {
            var m = music[i];
            if (m.sound == null) continue;

            m.level = Mathf.MoveTowards(m.level, m.target, m.rate * dt);
            m.src.volume = m.sound.volume * m.level * bus;

            if (m.target <= 0f && m.level <= 0f) { m.src.Stop(); m.sound = null; }
        }
    }
}