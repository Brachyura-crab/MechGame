using System.Collections.Generic;
using UnityEngine;
using EnemyV2;

// swarm ที่ไม่มีเจ้านาย: ตัวซีดจางไปทางขาวตามค่า Follower.Awake01 (1 = สีปกติ, 0 = ซีดสุด)
// ใช้ shader ที่มีค่า _Whiten (M_SwarmSprite) ส่งผ่าน MaterialPropertyBlock จึงไม่สร้าง material เพิ่มและไม่เสีย batching
// ไม่แตะ SpriteRenderer.color (EnemyCorpse ใช้ย้อมศพ) ตายแล้วคืนค่าเป็น 0 เพื่อให้ศพไม่ขาว
[DisallowMultipleComponent]
[RequireComponent(typeof(Follower))]
public class SwarmPale : MonoBehaviour
{
    static readonly int WhitenId = Shader.PropertyToID("_Whiten");

    [SerializeField] SpriteRenderer[] bodySprites;   // ว่าง = หาจากลูกทั้งหมด (ไม่รวม sprite ของ WorldBar)
    [Range(0f, 1f)] [SerializeField] float maxWhiten = 0.7f;   // ความขาวเมื่อไม่มีเจ้านายเต็มที่ (1 = ขาวล้วน)

    Follower follower;
    Health health;
    MaterialPropertyBlock block;
    float applied = -1f;
    bool dead;

    void Awake()
    {
        follower = GetComponent<Follower>();
        TryGetComponent(out health);
        block = new MaterialPropertyBlock();

        if (bodySprites == null || bodySprites.Length == 0)
        {
            var list = new List<SpriteRenderer>();
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.GetComponentInParent<WorldBar>() == null) list.Add(sr);
            bodySprites = list.ToArray();
        }

        // เตือนถ้า sprite ไหนยังใช้ material ที่ไม่มี _Whiten (เช่นลืมเปลี่ยน หรือเป็นชิ้นเรืองแสง)
        foreach (SpriteRenderer sr in bodySprites)
            if (sr != null && sr.sharedMaterial != null && !sr.sharedMaterial.HasProperty(WhitenId))
                Debug.LogWarning("SwarmPale: " + sr.name + " ไม่ได้ใช้ material ที่มี _Whiten (ใช้ M_SwarmSprite)", sr);
    }

    void OnEnable()
    {
        dead = false;
        applied = -1f;                       // เกิดใหม่/ฟื้น: ใช้ค่าปัจจุบันใหม่ใน LateUpdate
        if (health != null) health.Died += OnDied;
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
    }

    void OnDied()
    {
        dead = true;
        Apply(0f);                           // ศพไม่ขาว ให้ EnemyCorpse ย้อมสีศพตามปกติ
    }

    void LateUpdate()
    {
        if (dead || (health != null && health.IsDead)) return;

        float a = follower.Awake01;
        if (Mathf.Approximately(a, applied)) return;
        applied = a;

        Apply((1f - a) * maxWhiten);
    }

    void Apply(float whiten)
    {
        for (int i = 0; i < bodySprites.Length; i++)
        {
            SpriteRenderer sr = bodySprites[i];
            if (sr == null) continue;

            sr.GetPropertyBlock(block);      // เก็บค่าที่ SpriteRenderer ตั้งไว้ (เช่น texture) ไม่ให้หาย
            block.SetFloat(WhitenId, whiten);
            sr.SetPropertyBlock(block);
        }
    }
}