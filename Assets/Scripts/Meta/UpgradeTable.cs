using System;
using UnityEngine;

public enum UpgradeKind { MaxHp, ShieldRegen, EnergyRegen, MaxSpeed, FireRate }

[Serializable]
public class UpgradeDef
{
    public UpgradeKind kind;
    public string label = "ARMOR PLATING";
    public string statLabel = "MAX HP";
    public float percentPerLevel = 8f;        // +% ต่อเลเวล (ใช้เป็น Increased modifier ขั้นถัดไป)
    public int baseCost = 10;                 // ราคา = baseCost * (level+1)
}

[CreateAssetMenu(menuName = "Game/Upgrade Table")]
public class UpgradeTable : ScriptableObject
{
    public int maxLevel = 5;
    public UpgradeDef[] defs = new UpgradeDef[0];

    public int CostOf(UpgradeDef d, int currentLevel) => d.baseCost * (currentLevel + 1);
    public float BonusPercent(UpgradeDef d, int level) => d.percentPerLevel * level;
}