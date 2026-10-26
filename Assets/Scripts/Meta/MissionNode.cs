using UnityEngine;
using EnemyV2;

[CreateAssetMenu(menuName = "Game/Mission Node")]
public class MissionNode : ScriptableObject
{
    public string id = "m01";                 // คีย์ในเซฟ ห้ามซ้ำ
    public string displayName = "OUTPOST";
    [TextArea] public string brief;
    public string sceneName = "Game";         // ฉากที่ต้องโหลด

    [Header("แผนที่ (พิกัด 0-1 ในกรอบแผนที่)")]
    public Vector2 mapPos = new Vector2(.5f, .5f);
    public MissionNode[] connectsTo;          // เส้นเชื่อมไปโหนดถัดไป
    public MissionNode requires;              // ต้องผ่านด่านนี้ก่อน (ว่าง = เปิดตั้งแต่แรก)

    [Header("กติกา")]
    public MissionData mission;               // ของเดิม (EnemyV2)
    public int gatesToClose = 3;
    public float timeLimit = 600f;
    [Range(1, 5)] public int threat = 1;      // ความยาก 1-5

    [Header("รางวัล")]
    public int basePoints = 20;               // แต้มอัปเกรดฐานเมื่อชนะ
    public int scorePerBonusPoint = 500;      // ทุกกี่คะแนนได้โบนัส +1
    public int maxBonusPoints = 30;
}