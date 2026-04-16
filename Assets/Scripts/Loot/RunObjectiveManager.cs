using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RunObjectiveManager : MonoBehaviour
{
    public static RunObjectiveManager Instance { get; private set; }

    private Inventory inventory;

    public event Action OnObjectiveProgressChanged;

    [Header("All Item Definitions")]
    [Tooltip("Pool the run draws shopping-list lines from: each entry is a Loot ItemDefinition (pickup rules, allowed rooms, shoppingListKey). Room props stay as catalog sprites unless a decoration row uses the same ItemDefinition with Pickup only when on shopping list.")]
    [SerializeField] private List<ItemDefinition> allItemDefinitions = new List<ItemDefinition>();

    [Header("Shopping List Settings")]
    [Tooltip("Number of distinct shopping-list lines (unique loot keys) per run.")]
    [SerializeField] private int minListEntries = 12;
    [SerializeField] private int maxListEntries = 15;

    [Header("Goal Value Settings")]
    [SerializeField] private float minGoalMultiplier = 1.2f;
    [SerializeField] private float maxGoalMultiplier = 1.5f;

    [Header("Boss Unlock Requirements")]
    [SerializeField] private bool requireGoalValueToUnlockBoss = false;

    private readonly List<ShoppingListEntry> currentShoppingList = new List<ShoppingListEntry>();
    private int requiredGoalValue;
    private int currentCollectedValue;
    private bool hasShownBossUnlockedNotice = false;

    public IReadOnlyList<ShoppingListEntry> CurrentShoppingList => currentShoppingList;
    public int RequiredGoalValue => requiredGoalValue;
    public int CurrentCollectedValue => currentCollectedValue;
    public bool RequireGoalValueToUnlockBoss => requireGoalValueToUnlockBoss;

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

            // Sync with inventory status
            RecalculateObjectiveProgressFromInventory(this.inventory);
        }
        else
        {
            // Update UI if inventory is cleared
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

    /// <summary>Builds shopping list + value goal only. Call before the map is generated so room decor can branch on the list.</summary>
    public void GenerateShoppingListAndGoals()
    {
        currentShoppingList.Clear();
        currentCollectedValue = 0;
        hasShownBossUnlockedNotice = false;

        List<ItemDefinition> eligibleLootPool = GetEligibleLootPool();

        if (eligibleLootPool.Count == 0)
        {
            int rawLen = allItemDefinitions != null ? allItemDefinitions.Count : 0;
            int nullRefs = 0;
            if (allItemDefinitions != null)
            {
                for (int i = 0; i < allItemDefinitions.Count; i++)
                {
                    if (allItemDefinitions[i] == null)
                        nullRefs++;
                }
            }

            Debug.LogWarning(
                $"RunObjectiveManager: No eligible loot items (shopping list empty). " +
                $"Serialized list length={rawLen}, null references={nullRefs}. " +
                $"Assign non-null ItemDefinition assets on RunObjectiveManager and ensure itemCategory is Loot, lootValue > 0, and canAppearInShoppingList is enabled.");
            OnObjectiveProgressChanged?.Invoke();
            return;
        }

        int spawnBuffer = LootSpawnManager.Instance != null
            ? LootSpawnManager.Instance.ExtraRequiredSpawnBuffer
            : 2;

        List<ItemDefinition> generatablePool = BuildGeneratableLootPool(eligibleLootPool, spawnBuffer);
        if (generatablePool.Count == 0)
        {
            Debug.LogWarning(
                "RunObjectiveManager: No loot types passed spawn-capacity checks; falling back to full eligible pool (spawn may fail for some lines).");
            generatablePool = new List<ItemDefinition>(eligibleLootPool);
        }

        int desiredListSize = UnityEngine.Random.Range(minListEntries, maxListEntries + 1);
        int targetEntries = Mathf.Min(desiredListSize, generatablePool.Count);

        if (targetEntries < minListEntries)
        {
            Debug.LogWarning(
                $"RunObjectiveManager: Map only supports {generatablePool.Count} distinct winnable loot lines; list size will be {targetEntries} (wanted at least {minListEntries}). Add more room types or loot definitions.");
        }

        List<ItemDefinition> prioritized = BuildRoomCoveragePriorityList(generatablePool);
        if (prioritized.Count > targetEntries)
        {
            Debug.LogWarning(
                $"RunObjectiveManager: Room coverage needs {prioritized.Count} entries, expanding list size from {targetEntries}.");
            targetEntries = prioritized.Count;
        }

        for (int i = 0; i < prioritized.Count && currentShoppingList.Count < targetEntries; i++)
        {
            ItemDefinition chosenLoot = prioritized[i];
            int areas;
            if (LootSpawnManager.Instance != null)
            {
                LootSpawnManager.Instance.RefreshSpawnAreas();
                if (LootSpawnManager.Instance.LootSpawnAreaCount > 0)
                    areas = LootSpawnManager.Instance.CountAreasThatCanHoldRequiredLoot(chosenLoot);
                else
                    areas = 32;
            }
            else
            {
                areas = 32;
            }

            int effectiveBuffer = LootSpawnManager.Instance != null
                ? LootSpawnManager.Instance.GetRequiredBufferForDefinition(chosenLoot)
                : spawnBuffer;
            int maxByMap = Mathf.Max(0, areas - effectiveBuffer);
            if (maxByMap < 1)
            {
                Debug.LogWarning(
                    $"RunObjectiveManager: Skipping '{chosenLoot.itemName}' — not enough loot areas ({areas}) for required buffer ({effectiveBuffer}).");
                continue;
            }

            int minAmount = Mathf.Max(1, chosenLoot.minRequiredAmount);
            int maxAmount = Mathf.Max(minAmount, chosenLoot.maxRequiredAmount);
            maxAmount = Mathf.Min(maxAmount, maxByMap);
            minAmount = Mathf.Min(minAmount, maxAmount);

            int requiredAmount = UnityEngine.Random.Range(minAmount, maxAmount + 1);
            requiredAmount = Mathf.Min(requiredAmount, areas);

            ShoppingListEntry entry = new ShoppingListEntry
            {
                itemDefinition = chosenLoot,
                requiredAmount = requiredAmount,
                collectedAmount = 0
            };

            currentShoppingList.Add(entry);
        }

        if (currentShoppingList.Count < targetEntries)
        {
            Debug.LogWarning(
                $"RunObjectiveManager: Built {currentShoppingList.Count} shopping-list lines instead of {targetEntries} (ran out of spawn-feasible loot types).");
        }

        if (currentShoppingList.Count == 0)
        {
            Debug.LogWarning("RunObjectiveManager: Shopping list has zero entries after generation.");
            OnObjectiveProgressChanged?.Invoke();
            return;
        }

        GenerateGoalValue();
        OnObjectiveProgressChanged?.Invoke();
    }

    /// <summary>Spawns world loot for <see cref="CurrentShoppingList"/> after rooms exist. Call from <see cref="MapManager"/> after <see cref="MapManager"/> builds the map.</summary>
    public void SpawnLootForCurrentObjective()
    {
        if (currentShoppingList.Count == 0)
            return;

        if (LootSpawnManager.Instance != null)
        {
            LootSpawnManager.Instance.GenerateLootForObjective(
                currentShoppingList,
                requiredGoalValue,
                allItemDefinitions
            );
        }

        if (inventory != null)
        {
            RecalculateObjectiveProgressFromInventory(inventory);
        }
        else
        {
            OnObjectiveProgressChanged?.Invoke();
        }
    }

    /// <summary>Full run setup: shopping list + goals, then world loot (same as calling the two split methods in order).</summary>
    public void GenerateNewRunObjective()
    {
        GenerateShoppingListAndGoals();
        SpawnLootForCurrentObjective();
    }

    public bool ContainsShoppingListKey(string shoppingListKey)
    {
        if (string.IsNullOrEmpty(shoppingListKey))
            return false;

        for (int i = 0; i < currentShoppingList.Count; i++)
        {
            ShoppingListEntry e = currentShoppingList[i];
            if (e == null || e.itemDefinition == null)
                continue;
            if (string.Equals(e.itemDefinition.GetShoppingListKey(), shoppingListKey, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private List<ItemDefinition> GetEligibleLootPool()
    {
        List<ItemDefinition> result = new List<ItemDefinition>();
        HashSet<string> seenKeys = new HashSet<string>();

        if (allItemDefinitions == null)
        {
            return result;
        }

        foreach (ItemDefinition itemDef in allItemDefinitions)
        {
            if (itemDef == null)
                continue;
            if (!itemDef.IsLoot())
                continue;
            if (!itemDef.canAppearInShoppingList)
                continue;
            if (itemDef.lootValue <= 0)
                continue;

            string key = itemDef.GetShoppingListKey();

            if (seenKeys.Contains(key))
            {
                continue;
            }

            seenKeys.Add(key);
            result.Add(itemDef);
        }

        return result;
    }

    /// <summary>
    /// Loot that can meet <see cref="LootSpawnManager.ExtraRequiredSpawnBuffer"/> on top of at least <see cref="ItemDefinition.minRequiredAmount"/> given one pickup per room per definition.
    /// </summary>
    private static List<ItemDefinition> BuildGeneratableLootPool(List<ItemDefinition> eligible, int spawnBuffer)
    {
        List<ItemDefinition> generatable = new List<ItemDefinition>();
        if (eligible == null)
            return generatable;

        if (LootSpawnManager.Instance == null)
        {
            generatable.AddRange(eligible);
            return generatable;
        }

        LootSpawnManager.Instance.RefreshSpawnAreas();
        if (LootSpawnManager.Instance.LootSpawnAreaCount == 0)
        {
            generatable.AddRange(eligible);
            return generatable;
        }

        for (int i = 0; i < eligible.Count; i++)
        {
            ItemDefinition def = eligible[i];
            if (def == null)
                continue;

            int areas = LootSpawnManager.Instance.CountAreasThatCanHoldRequiredLoot(def);
            int effectiveBuffer = LootSpawnManager.Instance.GetRequiredBufferForDefinition(def);
            if (areas >= def.minRequiredAmount + effectiveBuffer)
                generatable.Add(def);
        }

        return generatable;
    }

    private static void ShuffleItemList(List<ItemDefinition> list)
    {
        if (list == null || list.Count < 2)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            ItemDefinition tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    /// <summary>
    /// Greedy room-coverage seed: picks loot definitions so each room type has at least one shopping-list line when possible.
    /// Remaining definitions are shuffled and appended.
    /// </summary>
    private static List<ItemDefinition> BuildRoomCoveragePriorityList(List<ItemDefinition> generatablePool)
    {
        List<ItemDefinition> ordered = new List<ItemDefinition>();
        if (generatablePool == null || generatablePool.Count == 0)
            return ordered;

        HashSet<RoomType> uncovered = new HashSet<RoomType>();
        foreach (RoomType rt in Enum.GetValues(typeof(RoomType)))
        {
            if (rt != RoomType.None)
                uncovered.Add(rt);
        }

        HashSet<ItemDefinition> remaining = new HashSet<ItemDefinition>(generatablePool.Where(d => d != null));
        while (uncovered.Count > 0 && remaining.Count > 0)
        {
            ItemDefinition best = null;
            int bestCover = 0;

            foreach (ItemDefinition def in remaining)
            {
                if (def == null || def.allowedRoomTypes == null || def.allowedRoomTypes.Count == 0)
                    continue;

                int cover = 0;
                for (int i = 0; i < def.allowedRoomTypes.Count; i++)
                {
                    if (uncovered.Contains(def.allowedRoomTypes[i]))
                        cover++;
                }

                if (cover > bestCover)
                {
                    bestCover = cover;
                    best = def;
                }
                else if (cover == bestCover && cover > 0 && UnityEngine.Random.value < 0.5f)
                {
                    // Break ties randomly so one key (e.g. Baseball) does not dominate every run.
                    best = def;
                }
            }

            if (best == null || bestCover == 0)
                break;

            ordered.Add(best);
            remaining.Remove(best);

            for (int i = 0; i < best.allowedRoomTypes.Count; i++)
                uncovered.Remove(best.allowedRoomTypes[i]);
        }

        if (uncovered.Count > 0)
        {
            string missing = string.Join(", ", uncovered);
            Debug.LogWarning($"RunObjectiveManager: Could not guarantee coverage for room types: {missing}");
        }

        List<ItemDefinition> tail = new List<ItemDefinition>(remaining);
        ShuffleItemList(tail);
        ordered.AddRange(tail);
        return ordered;
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
                    if (entry.itemDefinition != null &&
                        entry.itemDefinition.GetShoppingListKey() == item.definition.GetShoppingListKey())
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

    public bool IsGoalValueComplete()
    {
        return currentCollectedValue >= requiredGoalValue;
    }

    public bool IsObjectiveComplete()
    {
        if (requireGoalValueToUnlockBoss)
        {
            return IsShoppingListComplete() && IsGoalValueComplete();
        }

        return IsShoppingListComplete();
    }

    public void SetRequireGoalValueToUnlockBoss(bool requireGoalValue)
    {
        requireGoalValueToUnlockBoss = requireGoalValue;

        OnObjectiveProgressChanged?.Invoke();
        TryShowBossUnlockedNotice();
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