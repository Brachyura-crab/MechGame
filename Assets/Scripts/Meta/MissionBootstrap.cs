using System;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class MissionBootstrap : MonoBehaviour
{
    [Header("ใช้ทดสอบใน Editor เมื่อกด Play ฉากนี้ตรงๆ (ว่าง = ไม่ใช้)")]
    [SerializeField] string devMechId;
    [SerializeField] string devMissionId;

    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public
                         | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    void Awake()
    {
        var cfg = GameConfig.Instance;
        if (GameSession.SelectedMech == null && cfg != null && !string.IsNullOrEmpty(devMechId))
            GameSession.SelectedMech = cfg.mechs != null ? cfg.mechs.Get(devMechId) : null;
        if (GameSession.SelectedMission == null && cfg != null && !string.IsNullOrEmpty(devMissionId))
            GameSession.SelectedMission = cfg.missions != null ? cfg.missions.Get(devMissionId) : null;

        var entry = GameSession.SelectedMech;
        if (entry == null || entry.playerPrefab == null) return;

        var tag = FindAnyObjectByType<PlayerTag>(FindObjectsInactive.Include);
        GameObject old = tag != null ? tag.gameObject : null;

        Vector3 pos = old != null ? old.transform.position : Vector3.zero;
        Quaternion rot = old != null ? old.transform.rotation : Quaternion.identity;

        if (old != null) old.SetActive(false);               // กัน Awake ของตัวเก่าทำงาน
        GameObject fresh = Instantiate(entry.playerPrefab, pos, rot);
        fresh.name = entry.playerPrefab.name;

        if (old != null)
        {
            int n = Rebind(old.transform, fresh.transform);
            Destroy(old);
            Debug.Log("MissionBootstrap: spawn " + fresh.name + ", ผูก reference ใหม่ " + n + " จุด");
        }
    }

    // เดินทุก MonoBehaviour ใน scene (รวม struct/class ที่ serialize ซ้อน เช่น Cinemachine Target)
    // ถ้า field ไหนชี้เข้า Player เก่า ให้เปลี่ยนไปชี้ตัวใหม่ในตำแหน่งลูกเดียวกัน
    static int Rebind(Transform oldRoot, Transform newRoot)
    {
        int count = 0;
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        foreach (var mb in all)
        {
            if (mb == null || mb.transform.IsChildOf(oldRoot)) continue;
            count += RebindObject(mb, mb, oldRoot, newRoot, 0);
        }
        return count;
    }

    // obj = MonoBehaviour หรือ class ซ้อน (แก้ในที่เดิม) หรือ struct ที่ถูก box (ผู้เรียกต้องเขียนกลับ)
    static int RebindObject(object obj, MonoBehaviour owner, Transform oldRoot, Transform newRoot, int depth)
    {
        int count = 0;
        for (Type t = obj.GetType();
             t != null && t != typeof(MonoBehaviour) && t != typeof(object) && t != typeof(ValueType);
             t = t.BaseType)
        {
            foreach (var f in t.GetFields(F))
            {
                if (f.IsStatic) continue;
                Type ft = f.FieldType;

                if (typeof(Component).IsAssignableFrom(ft) || ft == typeof(GameObject))
                {
                    var cur = f.GetValue(obj) as UnityEngine.Object;
                    if (cur == null) continue;

                    Transform tr = cur is GameObject go ? go.transform : ((Component)cur).transform;
                    if (!tr.IsChildOf(oldRoot)) continue;

                    UnityEngine.Object mapped = Map(cur, tr, oldRoot, newRoot);
                    if (mapped != null) { f.SetValue(obj, mapped); count++; }
                    else Debug.LogWarning("MissionBootstrap: ผูกใหม่ไม่ได้ " + owner.GetType().Name + "." + f.Name
                        + " (โครงลูกของ prefab ไม่ตรงกับ Player เดิม)", owner);
                }
                else if (depth < 3 && IsNestedSerializable(ft))
                {
                    object inner = f.GetValue(obj);
                    if (inner == null) continue;
                    int c = RebindObject(inner, owner, oldRoot, newRoot, depth + 1);
                    if (c > 0)
                    {
                        if (ft.IsValueType) f.SetValue(obj, inner);   // struct เป็น copy ต้องเขียนกลับ
                        count += c;
                    }
                }
            }
        }
        return count;
    }

    static bool IsNestedSerializable(Type ft)
    {
        if (ft.IsPrimitive || ft.IsEnum || ft == typeof(string) || ft.IsArray) return false;
        if (typeof(UnityEngine.Object).IsAssignableFrom(ft)) return false;
        string ns = ft.Namespace ?? "";
        if (ns.StartsWith("System") || ns.StartsWith("UnityEngine")) return false;
        return ft.IsValueType || ft.IsDefined(typeof(SerializableAttribute), false);
    }

    static UnityEngine.Object Map(UnityEngine.Object cur, Transform tr, Transform oldRoot, Transform newRoot)
    {
        var path = new System.Collections.Generic.List<int>();
        for (Transform c = tr; c != oldRoot; c = c.parent) path.Add(c.GetSiblingIndex());

        Transform target = newRoot;
        for (int i = path.Count - 1; i >= 0; i--)
        {
            if (path[i] >= target.childCount) return null;
            target = target.GetChild(path[i]);
        }

        if (cur is GameObject) return target.gameObject;

        var oldComp = (Component)cur;
        var newComps = target.GetComponents(cur.GetType());
        if (newComps.Length == 0) return null;

        // Weapon มีสองตัวบน object เดียวกัน: เลือกตัวที่ติ๊ก Is Right Arm ตรงกัน
        if (oldComp is Weapon ow)
        {
            foreach (var c in newComps)
                if (c is Weapon nw && nw.IsRightArm == ow.IsRightArm) return nw;
        }

        // component ชนิดเดียวกันหลายตัว: ใช้ลำดับที่ตรงกัน
        var oldComps = oldComp.GetComponents(cur.GetType());
        int idx = System.Array.IndexOf(oldComps, oldComp);
        if (idx >= 0 && idx < newComps.Length) return newComps[idx];
        return newComps[0];
    }
}