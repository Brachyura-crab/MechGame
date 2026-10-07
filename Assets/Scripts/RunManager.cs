using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    public enum RunState { Playing, Dying, Ended }

    [SerializeField] Health playerHealth;
    [SerializeField] PlayerDeath playerDeath;   // ใช้ดึงความยาวช่วงตาย

    public static RunManager Instance { get; private set; }

    public RunState State { get; private set; } = RunState.Playing;
    public float Elapsed { get; private set; }
    public int Kills { get; private set; }

    public event Action<bool> RunEnded;   // true = ชนะ, false = แพ้

    void Awake() => Instance = this;

    void OnEnable()
    {
        playerHealth.Died += OnPlayerDied;
        Health.AnyDied += OnAnyDied;
    }

    void OnDisable()
    {
        playerHealth.Died -= OnPlayerDied;
        Health.AnyDied -= OnAnyDied;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (State != RunState.Ended) Elapsed += Time.deltaTime;
    }

    void OnAnyDied(Health victim, DamageResult r)
    {
        if (State == RunState.Ended) return;
        if (r.source == playerHealth.gameObject) Kills++;   // นับเฉพาะที่ผู้เล่นเป็นคนฆ่า
    }

    void OnPlayerDied()
    {
        if (State != RunState.Playing) return;
        State = RunState.Dying;
        StartCoroutine(EndRun(false));
    }

    IEnumerator EndRun(bool won)
    {
        float wait = playerDeath != null ? playerDeath.DyingDuration : 1.2f;
        yield return new WaitForSeconds(wait);   // ช่วงตาย เกมเดินปกติ ศัตรูไม่ช้าลง

        State = RunState.Ended;
        Time.timeScale = 0f;                     // หยุดเกมแล้วตัดเข้าหน้าสรุป
        RunEnded?.Invoke(won);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // หน้าสรุปชั่วคราว (แทนที่ด้วย UI จริงในรอบทำ UI)
    void OnGUI()
    {
        if (State != RunState.Ended) return;

        var title = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
        var body = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        GUI.skin.button.fontSize = 26;

        float w = 500f, h = 340f;
        Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(box, GUIContent.none);

        int m = Mathf.FloorToInt(Elapsed / 60f);
        int s = Mathf.FloorToInt(Elapsed % 60f);

        GUI.Label(new Rect(box.x, box.y + 20f, w, 60f), "GAME OVER", title);
        GUI.Label(new Rect(box.x, box.y + 110f, w, 40f), "Survived  " + m.ToString("00") + ":" + s.ToString("00"), body);
        GUI.Label(new Rect(box.x, box.y + 160f, w, 40f), "Kills  " + Kills, body);

        if (GUI.Button(new Rect(box.x + 150f, box.y + 240f, 200f, 60f), "Retry")) Retry();
    }
}