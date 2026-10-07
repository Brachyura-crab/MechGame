using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PartDetailView : MonoBehaviour
{
    [SerializeField] ModuleSystem system;
    [SerializeField] BodyPartData defaultPart;
    [SerializeField] TMP_Text titleText;
    [SerializeField] GameObject emptyLabel;
    [SerializeField] ModuleItemView itemPrefab;
    [SerializeField] Transform container;

    readonly List<ModuleData> buffer = new List<ModuleData>();
    readonly List<ModuleItemView> items = new List<ModuleItemView>();
    BodyPartData selected;
    public BodyPartData Selected => selected;

    void Start()
    {
        if (defaultPart != null) Select(defaultPart);
    }

    // ซิลลูเอตฝั่งซ้ายจะเรียกตัวนี้ตอนกดชิ้นส่วน
    public void Select(BodyPartData part)
    {
        selected = part;

        foreach (ModuleItemView it in items)
        {
            it.gameObject.SetActive(false);   // ออกจาก layout ทันที ไม่รอ Destroy
            Destroy(it.gameObject);
        }
        items.Clear();

        system.GetModules(part, buffer);
        foreach (ModuleData m in buffer)
        {
            ModuleItemView it = Instantiate(itemPrefab, container);
            it.Bind(system, m);
            items.Add(it);
        }

        titleText.text = part != null ? part.displayName : "";
        emptyLabel.SetActive(items.Count == 0);
    }
}