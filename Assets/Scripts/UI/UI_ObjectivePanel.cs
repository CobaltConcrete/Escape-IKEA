using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ObjectivePanel : MonoBehaviour
{
    [Header("Loot List")]
    [SerializeField] private Transform lootContainer;
    [SerializeField] private GameObject lootRowTemplate;

    [Header("Other Text")]
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI bossText;

    private void Start()
    {
        RefreshUI();
        lootRowTemplate.SetActive(false);
    }

    private void OnEnable()
    {
        if (RunObjectiveManager.Instance != null)
        {
            RunObjectiveManager.Instance.OnObjectiveProgressChanged += RefreshUI;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (RunObjectiveManager.Instance != null)
        {
            RunObjectiveManager.Instance.OnObjectiveProgressChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        if (RunObjectiveManager.Instance == null)
        {
            ClearLootRows();

            if (valueText != null) valueText.text = "";
            if (bossText != null) bossText.text = "";

            return;
        }

        RefreshLootRows();
        RefreshValueText();
        RefreshBossText();
    }

    private void RefreshLootRows()
    {
        if (lootContainer == null || lootRowTemplate == null) return;

        ClearLootRows();

        var entries = RunObjectiveManager.Instance.CurrentShoppingList;
        int count = entries != null ? entries.Count : 0;

        // ⭐ 核心：根据数量计算字体和行高
        float fontSize = GetAdaptiveFontSize(count);
        float rowHeight = GetAdaptiveRowHeight(count);

        // ⭐ 行距压缩
        VerticalLayoutGroup layout = lootContainer.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            float t = Mathf.InverseLerp(4f, 10f, count);
            layout.spacing = Mathf.Lerp(8f, 2f, t);
        }

        foreach (ShoppingListEntry entry in entries)
        {
            if (entry == null || entry.itemDefinition == null) continue;

            GameObject row = Instantiate(lootRowTemplate, lootContainer);
            row.SetActive(true);

            // ⭐ 控制每一行高度
            LayoutElement le = row.GetComponent<LayoutElement>();
            if (le == null) le = row.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;
            le.minHeight = rowHeight;

            Transform lootTextTransform = row.transform.Find("LootText");
            if (lootTextTransform != null)
            {
                TextMeshProUGUI text = lootTextTransform.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"{entry.GetDisplayName()} {entry.collectedAmount}/{entry.requiredAmount}";

                    // ⭐ 关键：关闭自动缩放，用我们自己的
                    text.enableAutoSizing = false;
                    text.fontSize = fontSize;

                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            Transform checkTransform = row.transform.Find("Checkmark");
            if (checkTransform != null)
            {
                checkTransform.gameObject.SetActive(entry.IsComplete());
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(lootContainer as RectTransform);
    }

    private void ClearLootRows()
    {
        if (lootContainer == null || lootRowTemplate == null) return;

        foreach (Transform child in lootContainer)
        {
            if (child.gameObject == lootRowTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void RefreshValueText()
    {
        if (valueText == null) return;

        if (RunObjectiveManager.Instance.RequireGoalValueToUnlockBoss)
        {
            valueText.text =
                $"Value: {RunObjectiveManager.Instance.CurrentCollectedValue} / {RunObjectiveManager.Instance.RequiredGoalValue}";
        }
        else
        {
            valueText.text =
                $"Value: {RunObjectiveManager.Instance.CurrentCollectedValue}";
        }
    }

    private void RefreshBossText()
    {
        if (bossText == null) return;

        if (RunObjectiveManager.Instance.IsObjectiveComplete())
        {
            bossText.text = "Boss: <color=#4CAF50><b>Unlocked</b></color>";
        }
        else
        {
            bossText.text = "Boss: <color=#F44336><b>Locked</b></color>";
        }
    }
    private float GetAdaptiveFontSize(int count)
    {
        if (count <= 4) return 24f;
        if (count == 5) return 21f;
        if (count == 6) return 18f;
        if (count == 7) return 16f;
        if (count == 8) return 14f;
        return 12f;
    }

    private float GetAdaptiveRowHeight(int count)
    {
        if (count <= 4) return 40f;
        if (count == 5) return 34f;
        if (count == 6) return 30f;
        if (count == 7) return 26f;
        if (count == 8) return 24f;
        return 22f;
    }
}