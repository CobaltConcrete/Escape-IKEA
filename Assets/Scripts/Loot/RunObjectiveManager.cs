using System;
using System.Collections.Generic;
using UnityEngine;

public class RunObjectiveManager : MonoBehaviour
{
    public static RunObjectiveManager Instance { get; private set; }

    private Inventory inventory;

    public event Action OnObjectiveProgressChanged;

    [Header("All Item Definitions")]
    [SerializeField] private List<ItemDefinition> allItemDefinitions = new List<ItemDefinition>();

    [Header("Shopping List Settings")]
    [SerializeField] private int minListEntries = 3;
    [SerializeField] private int maxListEntries = 5;

    [Header("Goal Value Settings")]
    [SerializeField] private float minGoalMultiplier = 1.2f;
    [SerializeField] private float maxGoalMultiplier = 1.5f;

    private readonly List<ShoppingListEntry> currentShoppingList = new List<ShoppingListEntry>();
    private int requiredGoalValue;
    private int currentCollectedValue;
    private bool hasShownBossUnlockedNotice = false;

    public IReadOnlyList<ShoppingListEntry> CurrentShoppingList => currentShoppingList;
    public int RequiredGoalValue => requiredGoalValue;
    public int CurrentCollectedValue => currentCollectedValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnLootListChanged -= OnLootListChanged;
        }
    }

    public void SetInventory(Inventory inventory)
    {
        if (this.inventory != null)
        {
            this.inventory.OnLootListChanged -= OnLootListChanged;
        }

        this.inventory = inventory;

        if (this.inventory != null)
        {
            this.inventory.OnLootListChanged += OnLootListChanged;

            // sync with inventory status
            RecalculateObjectiveProgressFromInventory(this.inventory);
        }
        else
        {
            // update UI if inventory is cleared
            currentCollectedValue = 0;

            foreach (ShoppingListEntry entry in currentShoppingList)
            {
                entry.collectedAmount = 0;
            }

            OnObjectiveProgressChanged?.Invoke();
        }
    }
    public List<ItemDefinition> GetAllItemDefinitions()
    {
        return allItemDefinitions;
    }
    private void OnLootListChanged(object sender, EventArgs e)
    {
        RecalculateObjectiveProgressFromInventory(inventory);
    }

    public void GenerateNewRunObjective()
    {
        currentShoppingList.Clear();
        currentCollectedValue = 0;

        List<ItemDefinition> eligibleLootPool = GetEligibleLootPool();

        if (eligibleLootPool.Count == 0)
        {
            Debug.LogWarning("RunObjectiveManager: No eligible loot items found.");
            OnObjectiveProgressChanged?.Invoke();
            return;
        }

        int entryCount = UnityEngine.Random.Range(minListEntries, maxListEntries + 1);
        entryCount = Mathf.Min(entryCount, eligibleLootPool.Count);

        for (int i = 0; i < entryCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, eligibleLootPool.Count);
            ItemDefinition chosenLoot = eligibleLootPool[randomIndex];
            eligibleLootPool.RemoveAt(randomIndex);

            int minAmount = Mathf.Max(1, chosenLoot.minRequiredAmount);
            int maxAmount = Mathf.Max(minAmount, chosenLoot.maxRequiredAmount);

            ShoppingListEntry entry = new ShoppingListEntry
            {
                itemDefinition = chosenLoot,
                requiredAmount = UnityEngine.Random.Range(minAmount, maxAmount + 1),
                collectedAmount = 0
            };

            currentShoppingList.Add(entry);
        }

        GenerateGoalValue();
        if (LootSpawnManager.Instance != null)
        {
            LootSpawnManager.Instance.GenerateLootForObjective(
                currentShoppingList,
                requiredGoalValue,
                allItemDefinitions
            );
        }

        // if inventory is bound, recalculate
        if (inventory != null)
        {
            RecalculateObjectiveProgressFromInventory(inventory);
        }
        else
        {
            OnObjectiveProgressChanged?.Invoke();
        }

    }

    private List<ItemDefinition> GetEligibleLootPool()
    {
        List<ItemDefinition> result = new List<ItemDefinition>();

        foreach (ItemDefinition itemDef in allItemDefinitions)
        {
            if (itemDef == null) continue;
            if (!itemDef.IsLoot()) continue;
            if (!itemDef.canAppearInShoppingList) continue;
            if (itemDef.lootValue <= 0) continue;

            result.Add(itemDef);
        }

        return result;
    }

    private void GenerateGoalValue()
    {
        int shoppingListTotalValue = 0;

        foreach (ShoppingListEntry entry in currentShoppingList)
        {
            shoppingListTotalValue += entry.GetRequiredValue();
        }

        float multiplier = UnityEngine.Random.Range(minGoalMultiplier, maxGoalMultiplier);
        requiredGoalValue = Mathf.CeilToInt(shoppingListTotalValue * multiplier);

        if (requiredGoalValue <= shoppingListTotalValue)
        {
            requiredGoalValue = shoppingListTotalValue + 1;
        }
    }

    public void RecalculateObjectiveProgressFromInventory(Inventory inventory)
    {
        if (inventory == null)
        {
            return;
        }

        currentCollectedValue = 0;

        foreach (ShoppingListEntry entry in currentShoppingList)
        {
            entry.collectedAmount = 0;
        }

        List<Item> lootItems = inventory.GetLootList();

        if (lootItems != null)
        {
            foreach (Item item in lootItems)
            {
                if (item == null || item.definition == null) continue;
                if (!item.definition.IsLoot()) continue;

                int amount = Mathf.Max(0, item.amount);

                currentCollectedValue += item.definition.lootValue * amount;

                foreach (ShoppingListEntry entry in currentShoppingList)
                {
                    if (entry.itemDefinition == item.definition)
                    {
                        entry.collectedAmount += amount;
                        break;
                    }
                }
            }
        }

        OnObjectiveProgressChanged?.Invoke();
        TryShowBossUnlockedNotice();
    }
    public bool IsShoppingListComplete()
    {
        foreach (ShoppingListEntry entry in currentShoppingList)
        {
            if (!entry.IsComplete())
            {
                return false;
            }
        }

        return true;
    }

    public bool IsObjectiveComplete()
    {
        return IsShoppingListComplete() && currentCollectedValue >= requiredGoalValue;
    }
    private void TryShowBossUnlockedNotice()
    {
        if (hasShownBossUnlockedNotice)
        {
            return;
        }

        if (IsObjectiveComplete())
        {
            hasShownBossUnlockedNotice = true;
            BossRoomNoticeUI.Instance?.ShowMessage("The Locked Room is now Available", 3f);
        }
    }
}