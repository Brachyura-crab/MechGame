using System.Collections.Generic;
using UnityEngine;

public class GlowController : MonoBehaviour
{
    [SerializeField] Sprite quadSprite;      // sprite สี่เหลี่ยม/วงกลมเปล่าเป็นฐาน (ภาพขาวล้วนพอ)
    [SerializeField] Material glowMaterial;  // Mat_Glow
    [SerializeField] float pointSize = 0.15f;

    [SerializeField] Transform torsoParent;      // พ่อของจุดเรืองบนลำตัว (BodySprite)
    [SerializeField] Transform rightArmParent;   // พ่อของจุดเรืองบนแขนขวา (RightArmPivot)

    readonly List<SpriteRenderer> torsoGlows = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> armGlows = new List<SpriteRenderer>();
    MaterialPropertyBlock mpb;

    void Awake() => mpb = new MaterialPropertyBlock();

    public void ApplyDirection(DirectionArmData dir, ArmPieceData rightArmPiece)
    {
        SyncPoints(torsoGlows, dir != null ? dir.torsoGlowPoints : null, torsoParent);
        SyncPoints(armGlows, rightArmPiece != null ? rightArmPiece.glowPoints : null, rightArmParent);
    }

    void SyncPoints(List<SpriteRenderer> pool, List<GlowPoint> points, Transform parent)
    {
        int count = points != null ? points.Count : 0;

        while (pool.Count < count) pool.Add(CreateGlowObject(parent));
        for (int i = 0; i < pool.Count; i++) pool[i].gameObject.SetActive(i < count);

        for (int i = 0; i < count; i++)
        {
            pool[i].transform.SetParent(parent, false);
            pool[i].transform.localPosition = points[i].localPos;
        }
    }

    SpriteRenderer CreateGlowObject(Transform parent)
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * pointSize;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = quadSprite;
        sr.material = glowMaterial;
        return sr;
    }

    // ปรับสี/ความสว่างเฉพาะจุด (ต่อยอดอนาคต) โดยไม่สร้าง material instance ใหม่
    public void SetColor(SpriteRenderer glow, Color color, float intensity)
    {
        mpb.SetColor("_Color", color);
        mpb.SetFloat("_Intensity", intensity);
        glow.SetPropertyBlock(mpb);
    }

    public void SetSortingOrder(int order)
    {
        foreach (var g in torsoGlows) g.sortingOrder = order + 3;
        foreach (var g in armGlows) g.sortingOrder = order + 3;
    }
}