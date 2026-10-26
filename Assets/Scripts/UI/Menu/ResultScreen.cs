using TMPro;
using UnityEngine;

// หน้าสรุปผลหลังจบด่าน วางบน GameObject ใน scene เกม (ซ่อนจนกว่าด่านจะจบ)
public class ResultScreen : MenuScreen
{
    [Header("Scenes (ต้องอยู่ใน Build Settings)")]
    [SerializeField] string missionScene = "MissionSelect";
    [SerializeField] string menuScene = "MainMenu";

    public static bool Active { get; private set; }   // RunManager ใช้เช็กว่ามีหน้านี้อยู่ไหม
    public static bool Showing { get; private set; }   // true เฉพาะตอนหน้าผลลัพธ์แสดงอยู่

    Transform canvasRoot;
    bool subscribed;

    protected override void Awake()
    {
        base.Awake();   // สร้าง canvas + CRT (ตั้ง timeScale = 1 ตอนเริ่ม scene ซึ่งปกติอยู่แล้ว)
        canvasRoot = stage.parent;
        var canvas = canvasRoot.GetComponent<Canvas>();
        canvas.sortingOrder = 1100;                // อยู่เหนือ HUD และ PauseMenu
        canvasRoot.gameObject.SetActive(false);    // ซ่อนไว้ก่อน
        Active = true;
        Showing = false;
    }

    protected override void OnBuild() { }   // สร้างเนื้อหาตอนด่านจบ

    void Start()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.RunEnded += OnRunEnded;
            subscribed = true;
        }
    }

    void OnDestroy()
    {
        Active = false;
        Showing = false;
        if (subscribed && RunManager.Instance != null) RunManager.Instance.RunEnded -= OnRunEnded;
    }

    void OnRunEnded(bool won)
    {
        canvasRoot.gameObject.SetActive(true);
        Showing = true;

        // ทำพื้นหลังให้โปร่งบางส่วน เห็นเกมจางๆ ด้านหลัง
        var bg = canvasRoot.Find("Background");
        if (bg != null)
        {
            var img = bg.GetComponent<UnityEngine.UI.Image>();
            if (img != null) { var c = img.color; c.a = 0.82f; img.color = c; }
        }

        Build(won);
    }

    void Build(bool won)
    {
        var run = RunManager.Instance;
        var mm = EnemyV2.MissionManager.Instance;
        var node = GameSession.SelectedMission;
        var mech = GameSession.SelectedMech;

        // คะแนนรวม (จาก ScorePoints ของผู้เล่น)
        int score = GameSession.LastScore;
        if (score == 0 && PlayerTag.Instance != null)
        {
            var sp = PlayerTag.Instance.GetComponentInChildren<ScorePoints>();
            if (sp != null) score = sp.TotalEarned;
        }

        string head = run.IsWin ? "MISSION COMPLETE" : run.TimedOut ? "TIME UP" : run.Abandoned ? "MISSION FAILED" : "GAME OVER";
        ThemeRole headRole = run.IsWin ? ThemeRole.InkBright : run.TimedOut ? ThemeRole.Warn : ThemeRole.Danger;

        ResetDyn();

        float pw = 640f, ph = 540f, px = (W - pw) / 2f, py = (H - ph) / 2f;
        var card = Card(dyn, px, py, pw, ph, headRole, 0.9f, ThemeRole.Surface, 0.95f);
        var p = card.rectTransform;

        L(p, head, 0, 24, pw, 56, 38, headRole, true, TextAlignmentOptions.Center);

        string sub = (node != null ? node.displayName : "FREE RUN");
        if (mech != null && mech.data != null) sub += "    UNIT: " + mech.data.shortName;
        L(p, "// " + sub, 0, 82, pw, 24, 13, ThemeRole.InkLow, false, TextAlignmentOptions.Center);

        B(p, 40, 116, pw - 80, 1, ThemeRole.InkLow, 0.6f);

        int m = Mathf.FloorToInt(run.Elapsed / 60f), s = Mathf.FloorToInt(run.Elapsed % 60f);
        string gates = mm != null ? mm.ClosedCount + " / " + mm.Total : "--";

        Row(p, pw, 132, "TIME", m.ToString("00") + ":" + s.ToString("00"));
        Row(p, pw, 176, "GATES", gates);
        Row(p, pw, 220, "KILLS", run.Kills.ToString());
        Row(p, pw, 264, "SCORE", score.ToString("N0"));

        B(p, 40, 318, pw - 80, 1, ThemeRole.InkLow, 0.6f);

        // แต้มอัปเกรด
        L(p, "UPGRADE POINTS", 40, 332, 260, 40, 15, ThemeRole.InkMid, true);
        string pts = node != null ? "+" + GameSession.LastPointsEarned : "--";
        L(p, pts, pw - 40 - 200, 326, 200, 48, 34, ThemeRole.InkBright, true, TextAlignmentOptions.MidlineRight);
        L(p, "TOTAL", 40, 378, 260, 24, 12, ThemeRole.InkLow);
        L(p, SaveService.Data.points.ToString(), pw - 40 - 200, 378, 200, 24, 14, ThemeRole.InkMid, false,
          TextAlignmentOptions.MidlineRight);
        if (node == null)
            L(p, "NO MISSION SELECTED: RESULT NOT SAVED", 40, 408, pw - 80, 22, 12, ThemeRole.Warn);

        // ปุ่ม
        float bw = 184f, gap = 16f, bx = (pw - (bw * 3 + gap * 2)) / 2f, by = ph - 76f;
        Btn(p, bx, by, bw, 48, "RETRY", Retry, true);
        Btn(p, bx + bw + gap, by, bw, 48, "MISSION SELECT", () => Go(missionScene), false);
        Btn(p, bx + (bw + gap) * 2, by, bw, 48, "MAIN MENU", () => Go(menuScene), false);
    }

    void Row(Transform p, float pw, float y, string label, string value)
    {
        B(p, 40, y, pw - 80, 36, ThemeRole.InkMid, 0.07f);
        B(p, 40, y, 3, 36, ThemeRole.InkBright, 1f);
        L(p, label, 56, y, 200, 36, 15, ThemeRole.InkMid, true);
        L(p, value, pw - 56 - 240, y, 240, 36, 18, ThemeRole.InkBright, false, TextAlignmentOptions.MidlineRight);
    }

    void Btn(Transform p, float x, float y, float w, float h, string label, System.Action act, bool primary)
    {
        var role = primary ? ThemeRole.InkBright : ThemeRole.InkMid;
        var f = Card(p, x, y, w, h, role, 1f, ThemeRole.Surface, primary ? 0.9f : 0.5f);
        L(f.rectTransform, label, 0, 0, w, h, 15, role, true, TextAlignmentOptions.Center);
        Hit(f.rectTransform, () => act());
    }

    void Retry()
    {
        if (RunManager.Instance != null) RunManager.Instance.Retry();   // ตั้ง timeScale = 1 ให้เอง
    }

    void Go(string scene)
    {
        Time.timeScale = 1f;
        LoadScene(scene);
    }
}