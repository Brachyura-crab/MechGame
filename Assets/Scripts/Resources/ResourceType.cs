using UnityEngine;

[CreateAssetMenu(menuName = "Mech/ResourceType")]
public class ResourceType : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public int maxAmount = 999;   // เก็บได้สูงสุด
}