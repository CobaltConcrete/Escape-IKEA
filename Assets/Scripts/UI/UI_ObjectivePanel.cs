using System.Collections;
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

    [Header("Dynamic List Text")]
    [SerializeField] private float listFontMax = 52f;
    [SerializeField] private float listFontMin = 24f;

    private void Start()
    {
        RefreshUI();
        StartCoroutine(CoRefreshAfterObjectiveInit());
    }

    private IEnumerator CoRefreshAfterObjectiveInit()
    {
        yield return null;
        RefreshUI();
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

        foreach (ShoppingListEntry entry in RunObjectiveManager.Instance.CurrentShoppingList)
        {
            if (entry == null || entry.itemDefinition == null) continue;

            GameObject row = Instantiate(lootRowTemplate, lootContainer);
            row.SetActive(true);

            Transform lootTextTransform = row.transform.Find("LootText");
            if (lootTextTransform != null)
            {
                TextMeshProUGUI text = lootTextTransform.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"{entry.GetDisplayName()} {entry.collectedAmount}/{entry.requiredAmount}";
                    ApplyDynamicListTextStyle(text, RunObjectiveManager.Instance.CurrentShoppingList.Count);
                }
            }

            Transform checkTransform = row.transform.Find("Checkmark");
            if (checkTransform != null)
            {
                checkTransform.gameObject.SetActive(entry.IsComplete());
            }
        }
    }

    private void ApplyDynamicListTextStyle(TextMeshProUGUI text, int rowCount)
    {
        if (text == null)
            return;

        float t = Mathf.InverseLerp(6f, 18f, rowCount);
        float targetSize = Mathf.Lerp(listFontMax, listFontMin, t);

        text.enableAutoSizing = true;
        text.fontSizeMax = targetSize;
        text.fontSizeMin = Mathf.Max(14f, targetSize * 0.55f);
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.textWrappingMode = TextWrappingModes.NoWrap;
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
}