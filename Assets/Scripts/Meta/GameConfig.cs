using UnityEngine;

[CreateAssetMenu(menuName = "Game/Game Config")]
public class GameConfig : ScriptableObject
{
    public MechCatalog mechs;
    public MissionCatalog missions;
    public UpgradeTable upgrades;

    static GameConfig inst;

    // ต้องวาง asset ชื่อ "GameConfig" ไว้ในโฟลเดอร์ Resources
    public static GameConfig Instance
    {
        get
        {
            if (inst == null) inst = Resources.Load<GameConfig>("GameConfig");
            return inst;
        }
    }
}