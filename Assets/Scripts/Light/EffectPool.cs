using System.Collections.Generic;
using UnityEngine;

public class PooledEffect : MonoBehaviour
{
    [HideInInspector] public GameObject key;

    Renderer[] renderers;
    int[] baseOrders;
    float timeLeft;

    public void Begin(float lifetime, int order)
    {
        if (renderers == null)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            baseOrders = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseOrders[i] = renderers[i].sortingOrder;
        }

        // order ที่ตั้งไว้ใน prefab ของแต่ละ sprite (เช่น แสง = +1) ถูกคงไว้เป็นส่วนต่างสัมพัทธ์
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = order + baseOrders[i];

        timeLeft = lifetime;
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) EffectPool.Release(this);
    }
}

public static class EffectPool
{
    static readonly Dictionary<GameObject, Stack<PooledEffect>> pools =
        new Dictionary<GameObject, Stack<PooledEffect>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => pools.Clear();

    static Stack<PooledEffect> StackFor(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Stack<PooledEffect> stack))
        {
            stack = new Stack<PooledEffect>();
            pools[prefab] = stack;
        }
        return stack;
    }

    public static PooledEffect Spawn(GameObject prefab, Vector3 position, Quaternion rotation,
                                     float lifetime, Transform parent = null, int sortingOrder = 0)
    {
        Stack<PooledEffect> stack = StackFor(prefab);

        PooledEffect fx = null;
        while (stack.Count > 0 && fx == null) fx = stack.Pop();   // ข้ามตัวที่ถูกทำลายไปแล้ว

        if (fx == null)
        {
            GameObject go = Object.Instantiate(prefab);
            fx = go.GetComponent<PooledEffect>();
            if (fx == null) fx = go.AddComponent<PooledEffect>();
            fx.key = prefab;
        }

        Transform t = fx.transform;
        t.SetParent(parent, true);
        t.SetPositionAndRotation(position, rotation);
        fx.gameObject.SetActive(true);   // เปิดใหม่ = Animator เริ่มเล่นจากเฟรมแรก
        fx.Begin(lifetime, sortingOrder);
        return fx;
    }

    public static void Release(PooledEffect fx)
    {
        fx.transform.SetParent(null, true);
        fx.gameObject.SetActive(false);
        StackFor(fx.key).Push(fx);
    }
}