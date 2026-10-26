using UnityEngine;

[CreateAssetMenu(menuName = "Mech/ModuleStatusStyle")]
public class ModuleStatusStyle : ScriptableObject
{
    public Sprite lightIcon;
    public Sprite mediumIcon;
    public Sprite heavyIcon;
    public Sprite repairIcon;
    public float spinDegPerSec = 180f;
    public float[] blinkSpeed = { 0f, 1f, 2f, 3f };   // รอบ/วินาที ตามระดับ: None, Light, Medium, Heavy
    [Range(0f, 1f)] public float blinkMinAlpha = 0.25f;

    public Sprite IconFor(ModuleDamage t)
    {
        switch (t)
        {
            case ModuleDamage.Light:  return lightIcon;
            case ModuleDamage.Medium: return mediumIcon;
            case ModuleDamage.Heavy:  return heavyIcon;
            default:                  return null;
        }
    }
}