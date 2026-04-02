using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ObjectiveTextPanel : MonoBehaviour
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
                    text.text = $"{entry.itemDefinition.itemName} {entry.collectedAmount}/{entry.requiredAmount}";
                }
            }

            Transform checkTransform = row.transform.Find("Checkmark");
            if (checkTransform != null)
            {
                checkTransform.gameObject.SetActive(entry.IsComplete());
            }
        }
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

        valueText.text =
            $"Value: {RunObjectiveManager.Instance.CurrentCollectedValue} / {RunObjectiveManager.Instance.RequiredGoalValue}";
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