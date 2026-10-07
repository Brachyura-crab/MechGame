using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabGroup : MonoBehaviour
{
    [Serializable]
    public class Tab 
        { 
            public Button button;
            public GameObject page;
            public TMP_Text label;
            public GameObject selectedBox;
            public string title;
        }

    [SerializeField] Tab[] tabs;
    [SerializeField] int startIndex = 0;
    [SerializeField] TMP_Text titleText;
    int current;

    void OnEnable() => UiThemeSource.Changed += Refresh;
    void OnDisable() => UiThemeSource.Changed -= Refresh;
    void Refresh() => Select(current);

    void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => Select(index));
        }
        Select(startIndex);
    }

    public void Select(int index)
    {
        if (tabs.Length == 0) return;
        current = Mathf.Clamp(index, 0, tabs.Length - 1);
        Color normal = UiThemeSource.Role(ThemeRole.InkMid);
        Color selected = UiThemeSource.Role(ThemeRole.OnInk);
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].page != null) tabs[i].page.SetActive(i == current);
            bool on = i == current;
            if (tabs[i].selectedBox != null) tabs[i].selectedBox.SetActive(on);
            if (tabs[i].label != null) tabs[i].label.color = on ? selected : normal;
        }
        if (titleText != null) titleText.text = tabs[current].title;
    }
}