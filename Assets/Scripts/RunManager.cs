using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    public enum RunState { Playing, Dying, Ended }

    [SerializeField] Health playerHealth;
    [SerializeField] PlayerDeath playerDeath;   // ใช้ดึงความยาวช่วงตาย

    [Header("Mission")]
    [SerializeField] bool missionMode = true;
    [SerializeField] float timeLimit = 600f;       // 10 นาที
    [SerializeField] float winDelay = 1.5f;        // หน่วงก่อนขึ้นหน้าสรุปตอนชนะ

    public static RunManager Instance { get; private set; }

    public RunState State { get; private set; } = RunState.Playing;
    public float Elapsed { get; private set; }
    public int Kills { get; private set; }

    public bool IsWin { get; private set; }
    public bool TimedOut { get; private set; }
    public bool Abandoned { get; private set; }
    public float Remaining => missionMode ? Mathf.Max(0f, timeLimit - Elapsed) : 0f;
    public bool MissionMode => missionMode;

    public event Action<bool> RunEnded;   // true = ชนะ, false = แพ้
    Health subscribed;   // ตัวที่ผูก Died ไว้จริง ใช้ถอดตอนปิด

    void Awake()
    {
        Instance = this;
        if (GameSession.SelectedMission != null)
            timeLimit = GameSession.SelectedMission.timeLimit;
    }

    void OnEnable()
    {
        // reference หาย/ว่าง: หยิบจากผู้เล่นที่ลงทะเบียนไว้แทน
        if (playerHealth == null && PlayerTag.Instance != null) playerHealth = PlayerTag.Instance.Health;
        if (playerHealth == null) Debug.LogError("RunManager: ไม่พบ Health ของผู้เล่น", this);

        subscribed = playerHealth;
        if (subscribed != null) subscribed.Died += OnPlayerDied;
        Health.AnyDied += OnAnyDied;
    }

    void OnDisable()
    {
        if (subscribed != null) subscribed.Died -= OnPlayerDied;
        subscribed = null;
        Health.AnyDied -= OnAnyDied;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (State != RunState.Playing) return;   // เวลาหยุดเดินเมื่อตายหรือชนะ

        Elapsed += Time.deltaTime;

        if (missionMode && Elapsed >= timeLimit)
        {
            Elapsed = timeLimit;
            TimedOut = true;
            State = RunState.Dying;
            StartCoroutine(EndRun(false, 1f));
        }
    }

    public void Win()
    {
        if (State != RunState.Playing) return;
        IsWin = true;
        State = RunState.Dying;
        StartCoroutine(EndRun(true, winDelay));
    }

    public void Fail()
    {
        if (State != RunState.Playing) return;
        Abandoned = true;
        State = RunState.Dying;
        StartCoroutine(EndRun(false, 1f));
    }

    void OnAnyDied(Health victim, DamageResult r)
    {
        if (State == RunState.Ended) return;
        if (playerHealth != null && r.source == playerHealth.gameObject) Kills++;
    }

    void OnPlayerDied()
    {
        if (State != RunState.Playing) return;
        State = RunState.Dying;
        StartCoroutine(EndRun(false, playerDeath != null ? playerDeath.DyingDuration : 1.2f));
    }

    IEnumerator EndRun(bool won, float wait)
    {
        yield return new WaitForSeconds(wait);   // ช่วงตาย เกมเดินปกติ ศัตรูไม่ช้าลง

        State = RunState.Ended;
        Time.timeScale = 0f;                     // หยุดเกมแล้วตัดเข้าหน้าสรุป
        if (GameSession.SelectedMission != null)
        {
            var sp = playerHealth != null ? playerHealth.GetComponentInParent<ScorePoints>() : null;
            SaveService.ApplyResult(GameSession.SelectedMission, won, sp != null ? sp.TotalEarned : 0);
        }
        RunEnded?.Invoke(won);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}