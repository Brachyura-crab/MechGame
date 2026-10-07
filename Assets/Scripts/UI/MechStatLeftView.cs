using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MechStatLeftView : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] MechStats mech;
    [SerializeField] ResourceInventory inventory;
    [SerializeField] StatDisplayTable table;

    [Header("Id band")]
    [SerializeField] Image portrait;
    [SerializeField] TMP_Text shortNameText;
    [SerializeField] TMP_Text fullNameText;
    [SerializeField] TMP_Text codeText;

    [Header("List")]
    [SerializeField] Transform content;
    [SerializeField] InfoRowView rowPrefab;
    [SerializeField] GameObject headingPrefab;
    [SerializeField] string supplyHeading = "SUPPLY";
    [SerializeField] float refreshInterval = 0.2f;

    struct StatRow { public InfoRowView view; public StatDisplayTable.Entry entry; }

    readonly List<StatRow> statRows = new List<StatRow>();
    readonly List<InfoRowView> supplyRows = new List<InfoRowView>();
    bool built;
    float timer;

    void Start()
    {
        MechData d = mech.Data;
        shortNameText.text = d.shortName;
        fullNameText.text = d.displayName;
        codeText.text = d.code;
        portrait.sprite = d.portrait;
        portrait.gameObject.SetActive(d.portrait != null);

        BuildStats();
        AddHeading(supplyHeading);    // แถว supply จะต่อท้ายหัวข้อนี้เสมอ
        built = true;
        Refresh();
    }

    void OnEnable()
    {
        inventory.Changed += OnInventoryChanged;
        UiThemeSource.Changed += Refresh;
        if (built) Refresh();
    }

    void OnDisable()
    {
        inventory.Changed -= OnInventoryChanged;
        UiThemeSource.Changed -= Refresh;
    }

    void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = refreshInterval;
        if (built) Refresh();
    }

    void BuildStats()
    {
        if (table == null || table.entries == null) return;

        string lastGroup = null;
        foreach (StatDisplayTable.Entry e in table.entries)
        {
            if (!e.visible) continue;
            if (e.group != lastGroup)
            {
                AddHeading(e.group);
                lastGroup = e.group;
            }
            InfoRowView row = Instantiate(rowPrefab, content);
            statRows.Add(new StatRow { view = row, entry = e });
        }
    }

    void AddHeading(string text)
    {
        GameObject h = Instantiate(headingPrefab, content);
        h.GetComponentInChildren<TMP_Text>().text = text;
    }

    void Refresh()
    {
        StatBlock s = mech.Stats;
        foreach (StatRow r in statRows)
        {
            float v = s.Get(r.entry.stat);
            float b = s.GetBase(r.entry.stat);
            r.view.Set(r.entry.label, StatDisplayTable.Format(v, r.entry.format));
            r.view.SetValueColor(UiThemeSource.Role(Mathf.Approximately(v, b) ? ThemeRole.InkMid : ThemeRole.InkBright));
        }
        RefreshSupply();
    }

    void OnInventoryChanged(ResourceType t, int n) => RefreshSupply();

    void RefreshSupply()
    {
        var amounts = inventory.Amounts;
        if (amounts.Count != supplyRows.Count) RebuildSupply();

        int i = 0;
        foreach (var kv in amounts)
        {
            InfoRowView row = supplyRows[i++];
            row.Set(kv.Key.displayName, kv.Value + " / " + kv.Key.maxAmount);
            row.SetValueColor(kv.Value > 0 ? UiThemeSource.Role(ThemeRole.InkMid) : UiThemeSource.Critical);
        }
    }

    void RebuildSupply()
    {
        foreach (InfoRowView r in supplyRows)
        {
            r.gameObject.SetActive(false);   // ออกจาก layout ทันที ไม่รอ Destroy
            Destroy(r.gameObject);
        }
        supplyRows.Clear();

        foreach (var kv in inventory.Amounts)
        {
            InfoRowView row = Instantiate(rowPrefab, content);
            row.SetIcon(kv.Key.icon);
            supplyRows.Add(row);
        }
    }
}