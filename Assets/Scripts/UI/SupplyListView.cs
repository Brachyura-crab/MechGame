// SupplyListView.cs
using System.Text;
using TMPro;
using UnityEngine;

public class SupplyListView : MonoBehaviour
{
    [SerializeField] ResourceInventory inventory;
    [SerializeField] TMP_Text listText;

    void OnEnable()
    {
        inventory.Changed += OnChanged;
        Refresh();
    }

    void OnDisable() => inventory.Changed -= OnChanged;

    void OnChanged(ResourceType t, int n) => Refresh();

    void Refresh()
    {
        var sb = new StringBuilder();
        foreach (var kv in inventory.Amounts)
            sb.AppendLine(kv.Key.displayName + "   " + kv.Value + " / " + kv.Key.maxAmount);
        listText.text = sb.ToString();
    }
}