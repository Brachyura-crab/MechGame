using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class MechUpgradeSave
{
    public string mechId;
    public int[] levels = new int[5];         // index = (int)UpgradeKind
}

[Serializable]
public class SaveData
{
    public int points;
    public List<string> clearedMissions = new List<string>();
    public List<string> unlockedMechs = new List<string>();
    public List<MechUpgradeSave> upgrades = new List<MechUpgradeSave>();
    public int bestScoreTotal;
}

public static class SaveService
{
    static SaveData data;
    static string PathFile => Path.Combine(Application.persistentDataPath, "save.json");

    public static SaveData Data
    {
        get { if (data == null) Load(); return data; }
    }

    public static void Load()
    {
        try
        {
            data = File.Exists(PathFile)
                ? JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFile))
                : new SaveData();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Save load failed: " + e.Message);
            data = new SaveData();
        }
        if (data == null) data = new SaveData();
    }

    public static void Save()
    {
        try { File.WriteAllText(PathFile, JsonUtility.ToJson(Data, true)); }
        catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
    }

    public static void Reset() { data = new SaveData(); Save(); }

    // ---------- missions ----------
    public static bool IsCleared(MissionNode n) => n != null && Data.clearedMissions.Contains(n.id);

    public static bool IsUnlocked(MissionNode n)
        => n != null && (n.requires == null || IsCleared(n.requires));

    // ---------- mechs ----------
    public static bool IsUnlocked(MechEntry m)
    {
        if (m == null) return false;
        if (m.unlockedByDefault) return true;
        return Data.unlockedMechs.Contains(m.id);
    }

    public static void UnlockMech(string id)
    {
        if (!Data.unlockedMechs.Contains(id)) { Data.unlockedMechs.Add(id); Save(); }
    }

    // ---------- upgrades ----------
    public static int[] LevelsOf(string mechId)
    {
        foreach (var u in Data.upgrades) if (u.mechId == mechId) return u.levels;
        var n = new MechUpgradeSave { mechId = mechId };
        Data.upgrades.Add(n);
        return n.levels;
    }

    public static int Level(string mechId, UpgradeKind k) => LevelsOf(mechId)[(int)k];

    public static bool TryBuy(string mechId, UpgradeDef def, UpgradeTable table)
    {
        var lv = LevelsOf(mechId);
        int cur = lv[(int)def.kind];
        if (cur >= table.maxLevel) return false;
        int cost = table.CostOf(def, cur);
        if (Data.points < cost) return false;
        Data.points -= cost;
        lv[(int)def.kind] = cur + 1;
        Save();
        return true;
    }

    // ---------- results ----------
    /// ชนะ: แต้ม = basePoints*threat + โบนัสตามคะแนน, แพ้: ได้ 25% ของฐาน
    public static int CalcPoints(MissionNode n, bool won, int score)
    {
        if (n == null) return 0;
        int basePts = n.basePoints * Mathf.Max(1, n.threat);
        int bonus = n.scorePerBonusPoint > 0
            ? Mathf.Min(n.maxBonusPoints, score / n.scorePerBonusPoint) : 0;
        return won ? basePts + bonus : Mathf.RoundToInt(basePts * 0.25f);
    }

    public static int ApplyResult(MissionNode n, bool won, int score)
    {
        int pts = CalcPoints(n, won, score);
        Data.points += pts;
        if (won && n != null && !Data.clearedMissions.Contains(n.id))
            Data.clearedMissions.Add(n.id);
        if (won && n != null && n.id != null) { /* ปลดล็อกหุ่นผูกด่านทำในขั้นถัดไป */ }
        Data.bestScoreTotal = Mathf.Max(Data.bestScoreTotal, score);
        Save();
        GameSession.LastWon = won;
        GameSession.LastScore = score;
        GameSession.LastPointsEarned = pts;
        return pts;
    }
}