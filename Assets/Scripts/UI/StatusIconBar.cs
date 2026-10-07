using System.Collections.Generic;
using UnityEngine;

public class StatusIconBar : MonoBehaviour
{
    [SerializeField] StatusController status;
    [SerializeField] float iconSize = 0.4f;
    [SerializeField] float spacing = 0.5f;
    [SerializeField] int sortingOrder = 110;

    readonly List<SpriteRenderer> icons = new List<SpriteRenderer>();
    bool started;

    void OnEnable()
    {
        status.Changed += Refresh;
        if (started) Refresh();
    }

    void OnDisable() => status.Changed -= Refresh;

    void Start()
    {
        started = true;
        Refresh();
    }

    void Refresh()
    {
        IReadOnlyList<StatusController.ActiveStatus> list = status.Active;
        int shown = 0;

        for (int i = 0; i < list.Count; i++)
        {
            Sprite sp = list[i].data.icon;
            if (sp == null) continue;   // ไม่มีไอคอนก็ไม่แสดง

            if (shown >= icons.Count) icons.Add(CreateIcon());
            SpriteRenderer sr = icons[shown];
            sr.sprite = sp;
            float size = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
            sr.transform.localScale = Vector3.one * (iconSize / size);
            sr.gameObject.SetActive(true);
            shown++;
        }

        for (int i = shown; i < icons.Count; i++) icons[i].gameObject.SetActive(false);

        // เรียงกึ่งกลางเป็นแถว
        for (int i = 0; i < shown; i++)
            icons[i].transform.localPosition = new Vector3((i - (shown - 1) * 0.5f) * spacing, 0f, 0f);
    }

    SpriteRenderer CreateIcon()
    {
        var go = new GameObject("StatusIcon");
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        return sr;
    }
}