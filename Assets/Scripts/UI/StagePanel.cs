// StagePanel.cs
using TMPro;
using UnityEngine;

public class StagePanel : MonoBehaviour
{
    [SerializeField] TMP_Text timeText;
    [SerializeField] TMP_Text killText;

    void Update()
    {
        RunManager r = RunManager.Instance;
        if (r == null) return;
        int m = Mathf.FloorToInt(r.Elapsed / 60f);
        int s = Mathf.FloorToInt(r.Elapsed % 60f);
        timeText.text = "TIME  " + m.ToString("00") + ":" + s.ToString("00");
        killText.text = "KILLS  " + r.Kills;
    }
}