using System;
using UnityEngine;
using System.Collections.Generic;

public enum ArmPose { Idle, Raised, Aim }

[Serializable]
public class ArmPieceData
{
    public Sprite sprite;
    public Vector2 muzzleLocal;   // ปลายกระบอก หน่วยโลก สัมพัทธ์กับ pivot ของ sprite นี้ (ตั้งโดยเครื่องมือสแกน)
    public Sprite glowSprite;
    public List<GlowPoint> glowPoints = new List<GlowPoint>();
}

[Serializable]
public class GlowPoint
{
    public Vector2 localPos;   // หน่วยโลก สัมพัทธ์กับ pivot ของชิ้นที่มันอยู่
}

[Serializable]
public class ArmPoseSet
{
    public ArmPose pose;
    public ArmPieceData left;
    public ArmPieceData right;
}

[Serializable]
public class DirectionArmData
{
    public string directionName;     // "Down", "DownRight" ฯลฯ (ไว้ขยายทีหลัง)
    public Sprite torso;
    public Vector2 leftElbowLocal;   // หน่วยโลก สัมพัทธ์กับกึ่งกลางลำตัว
    public Vector2 rightElbowLocal;
    public Sprite torsoGlow;
    public Sprite jetGlowSprite;   // วาดเฉพาะ Down, Up, Left, DownLeft, UpLeft; Right/UpRight/DownRight เว้นว่างไว้ (มิเรอร์อัตโนมัติ)
    public List<GlowPoint> torsoGlowPoints = new List<GlowPoint>();
    public List<GlowPoint> trailPoints = new List<GlowPoint>();
    public bool leftArmInFront = true;
    public bool rightArmInFront = true;
    public bool jetGlowInFront = false;

    [Header("Missile Launcher (ตัวปล่อยบนหลังไหล่)")]
    public Sprite launcherLeft;       // canvas เท่ารูปลำตัว, pivot Center
    public Sprite launcherRight;
    public List<GlowPoint> launcherLeftPoints = new List<GlowPoint>();    // สแกนจากจุดม่วงแดง
    public List<GlowPoint> launcherRightPoints = new List<GlowPoint>();
    public bool launcherLeftInFront = false;    // true = อยู่เหนือลำตัว
    public bool launcherRightInFront = false;

    public ArmPoseSet[] poses;
}

[CreateAssetMenu(menuName = "Mech/ArmSet")]
public class MechArmSet : ScriptableObject
{
    public DirectionArmData[] directions;
}