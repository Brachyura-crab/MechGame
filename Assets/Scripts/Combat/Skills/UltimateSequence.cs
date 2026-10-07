using System;
using System.Collections;
using UnityEngine;

public class UltimateSequence : MonoBehaviour
{
    [SerializeField] StatusData castLock;          // บล็อก Move+Attack+Skills
    [SerializeField] GameObject animationPrefab;   // prefab animation ที่วาดเอง
    [SerializeField] Vector2 spawnOffset;
    [SerializeField] float animDuration = 0f;      // 0 = ใช้ความยาว clip ใน Animator ของ prefab อัตโนมัติ
    [SerializeField] bool hideMech = false;        // ติ๊กถ้า prefab วาดตัวหุ่นมาเองทั้งตัว
    [SerializeField] float faceTimeout = 0.6f;
    [SerializeField] float faceAngle = -90f;       // Down

    StatusController status;
    PlayerSpriteDirection facing;
    PlayerMove move;
    Health health;
    GameObject fx;

    public bool IsPlaying { get; private set; }

    void Awake()
    {
        status = GetComponent<StatusController>();
        facing = GetComponent<PlayerSpriteDirection>();
        move = GetComponent<PlayerMove>();
        health = GetComponent<Health>();
    }

    public void Play(Action<bool> done) => StartCoroutine(Run(done));

    IEnumerator Run(Action<bool> done)
    {
        IsPlaying = true;
        if (castLock != null) status.Apply(castLock, gameObject, 30f);
        move.Frozen = true;
        facing.ForcedAngle = faceAngle;

        float t = 0f;
        while (!facing.IsFacing(faceAngle) && t < faceTimeout)
        {
            if (health != null && health.IsDead) { End(done, false); yield break; }
            t += Time.deltaTime;
            yield return null;
        }

        facing.HideMech = hideMech;
        float len = 0f;
        if (animationPrefab != null)
        {
            fx = Instantiate(animationPrefab, transform);
            fx.transform.localPosition = spawnOffset;
            len = animDuration > 0f ? animDuration : ClipLength(fx);
        }

        float e = 0f;
        while (e < len)
        {
            if (health != null && health.IsDead) { End(done, false); yield break; }
            e += Time.deltaTime;
            yield return null;
        }

        End(done, true);
    }

    static float ClipLength(GameObject go)
    {
        Animator a = go.GetComponentInChildren<Animator>();
        if (a == null || a.runtimeAnimatorController == null) return 1f;
        float max = 0f;
        foreach (AnimationClip c in a.runtimeAnimatorController.animationClips)
            max = Mathf.Max(max, c.length);
        return max > 0f ? max : 1f;
    }

    void End(Action<bool> done, bool completed)
    {
        IsPlaying = false;
        if (fx != null) Destroy(fx);
        facing.HideMech = false;
        facing.ForcedAngle = null;
        move.Frozen = false;
        if (castLock != null) status.Remove(castLock);
        done?.Invoke(completed);
    }
}