using System.Collections.Generic;
using UnityEngine;

public static class BulletPool
{
    static readonly Dictionary<Bullet, Stack<Bullet>> pools = new Dictionary<Bullet, Stack<Bullet>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => pools.Clear();

    public static Bullet Get(Bullet prefab)
    {
        if (!pools.TryGetValue(prefab, out Stack<Bullet> stack))
        {
            stack = new Stack<Bullet>();
            pools[prefab] = stack;
        }

        while (stack.Count > 0)
        {
            Bullet b = stack.Pop();
            if (b != null)
            {
                b.gameObject.SetActive(true);
                return b;
            }
        }

        Bullet inst = Object.Instantiate(prefab);
        inst.poolKey = prefab;
        return inst;
    }

    public static void Release(Bullet b)
    {
        if (b.poolKey == null)
        {
            Object.Destroy(b.gameObject);
            return;
        }
        b.gameObject.SetActive(false);
        pools[b.poolKey].Push(b);
    }
}