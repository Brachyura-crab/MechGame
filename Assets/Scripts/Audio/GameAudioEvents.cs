using EnemyV2;
using UnityEngine;

// เล่นเสียงจากเหตุการณ์ส่วนกลาง: ศัตรูตาย, ผู้เล่นตาย, ปิดประตู, จบด่าน
public class GameAudioEvents : MonoBehaviour
{
    RunManager run;

    void OnEnable()
    {
        Health.AnyDied += OnDied;
        Gate.AnyClosed += OnGateClosed;
    }

    void OnDisable()
    {
        Health.AnyDied -= OnDied;
        Gate.AnyClosed -= OnGateClosed;
    }

    void Start()
    {
        run = RunManager.Instance;
        if (run != null) run.RunEnded += OnRunEnded;
    }

    void OnDestroy()
    {
        if (run != null) run.RunEnded -= OnRunEnded;
    }

    void OnDied(Health victim, DamageResult r)
    {
        var b = GameAudio.Bank;
        if (b == null || victim == null) return;

        if (PlayerTag.Instance != null && victim == PlayerTag.Instance.Health)
        {
            AudioManager.Play(b.playerDeath);
            return;
        }

        Vector2 pos = victim.transform.position;
        var cm = victim.GetComponent<CampMember>();
        AudioManager.Play(cm != null && cm.IsLeader ? b.leaderDeath : b.swarmDeath, pos);
    }

    void OnGateClosed(Gate g)
    {
        var b = GameAudio.Bank;
        if (b != null && g != null) AudioManager.Play(b.gateClosed, g.transform.position);
    }

    void OnRunEnded(bool won)
    {
        var b = GameAudio.Bank;
        if (b != null) AudioManager.Play(won ? b.missionComplete : b.missionFailed);
    }
}