// Scripts/Meta/MissionCatalog.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Mission Catalog")]
public class MissionCatalog : ScriptableObject
{
    public MissionNode[] nodes;

    public MissionNode Get(string id)
    {
        if (nodes == null) return null;
        foreach (var n in nodes) if (n != null && n.id == id) return n;
        return null;
    }
}