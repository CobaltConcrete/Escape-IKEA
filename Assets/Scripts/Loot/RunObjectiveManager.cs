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
    [Header("Prefab-First Objective Source")]
    [SerializeField] private RoomPrefabSpawnCatalog prefabSpawnCatalog;

    [Header("Shopping List Settings")]
    [Tooltip("Number of distinct shopping-list lines (unique loot keys) per run. Capped at maxListEntries even when room-coverage wants more.")]
    [SerializeField] private int minListEntries = 7;
    [SerializeField] private int maxListEntries = 7;

    [Header("Goal Value Settings")]
    [SerializeField] private float minGoalMultiplier = 1.2f;
    [SerializeField] private float maxGoalMultiplier = 1.5f;

    [Header("Boss Unlock Requirements")]
    [SerializeField] private bool requireGoalValueToUnlockBoss = false;

    private readonly List<ShoppingListEntry> currentShoppingList = new List<ShoppingListEntry>();
    private int requiredGoalValue;
    private int currentCollectedValue;
    private bool hasShownBossUnlockedNotice = false;

    private bool lootCapacityGenerated = false;
    private int generatedLootExtraSlots = 0;

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

            ApplyLootCapacityFromShoppingList();

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
        lootCapacityGenerated = false;
        generatedLootExtraSlots = 0;

        if (TryGenerateShoppingListFromPrefabMetadata())
        {
            ClampCurrentShoppingListToGeneratedPickupCounts();
            GenerateGoalValue();
            ApplyLootCapacityFromShoppingList();
            OnObjectiveProgressChanged?.Invoke();
            return;
        }

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

        int minCap = Mathf.Max(1, Mathf.Min(minListEntries, maxListEntries));
        int maxCap = Mathf.Max(minCap, maxListEntries);

        int desiredListSize = UnityEngine.Random.Range(minCap, maxCap + 1);
        int targetEntries = Mathf.Min(desiredListSize, generatablePool.Count, maxCap);

        if (targetEntries < minCap)
        {
            Debug.LogWarning(
                $"RunObjectiveManager: Map only supports {generatablePool.Count} distinct winnable loot lines; list size will be {targetEntries} (wanted at least {minCap}). Add more room types or loot definitions.");
        }
        List<ItemDefinition> prioritized = BuildRoomCoveragePriorityList(generatablePool);
        int coverageCap = Mathf.Min(prioritized.Count, generatablePool.Count, maxCap);
        if (coverageCap > targetEntries)
        {
            if (prioritized.Count > maxCap)
            {
                Debug.LogWarning(
                    $"RunObjectiveManager: Room-coverage ordering includes {prioritized.Count} loot types; capping shopping list at maxListEntries ({maxCap}).");
            }

            targetEntries = coverageCap;
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

        ClampCurrentShoppingListToGeneratedPickupCounts();
        if (currentShoppingList.Count == 0)
        {
            Debug.LogWarning("RunObjectiveManager: Shopping list has zero entries after map-capacity clamping.");
            OnObjectiveProgressChanged?.Invoke();
            return;
        }

        GenerateGoalValue();
        ApplyLootCapacityFromShoppingList();
        OnObjectiveProgressChanged?.Invoke();
    }

    /// <summary>Spawns world loot for <see cref="CurrentShoppingList"/> after rooms exist. Call from <see cref="MapManager"/> after <see cref="MapManager"/> builds the map.</summary>
    public void SpawnLootForCurrentObjective()
    {
        if (currentShoppingList.Count == 0)
            return;

        if (RoomPrefabObjectiveSpawner.Instance != null &&
            RoomPrefabObjectiveSpawner.Instance.TrySpawnFromObjective(currentShoppingList))
        {
            OnObjectiveProgressChanged?.Invoke();
            return;
        }

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
        string targetKey = NormalizeShoppingListKey(shoppingListKey);
        if (string.IsNullOrEmpty(targetKey))
            return false;

        for (int i = 0; i < currentShoppingList.Count; i++)
        {
            ShoppingListEntry e = currentShoppingList[i];
            if (e == null)
                continue;
            if (string.Equals(NormalizeShoppingListKey(e.GetShoppingListKey()), targetKey, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// True only when this key is on the shopping list and its required count is not yet met.
    /// </summary>
    public bool NeedsMoreOfShoppingListKey(string shoppingListKey)
    {
        string targetKey = NormalizeShoppingListKey(shoppingListKey);
        if (string.IsNullOrEmpty(targetKey))
            return false;

        for (int i = 0; i < currentShoppingList.Count; i++)
        {
            ShoppingListEntry e = currentShoppingList[i];
            if (e == null)
                continue;
            if (!string.Equals(NormalizeShoppingListKey(e.GetShoppingListKey()), targetKey, StringComparison.Ordinal))
                continue;

            return e.collectedAmount < e.requiredAmount;
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
    private void ApplyLootCapacityFromShoppingList()
    {
        if (inventory == null)
            return;

        int requiredLootTotal = 0;

        foreach (ShoppingListEntry entry in currentShoppingList)
        {
            if (entry == null)
                continue;

            requiredLootTotal += Mathf.Max(0, entry.requiredAmount);
        }

        bool firstTime = false;

        if (!lootCapacityGenerated)
        {
            generatedLootExtraSlots = UnityEngine.Random.Range(2, 5);
            lootCapacityGenerated = true;
            firstTime = true;
        }

        int capacity = requiredLootTotal + generatedLootExtraSlots;
        inventory.SetLootCapacity(capacity);

        if (firstTime)
        {
            Debug.Log($"Loot capacity set to {capacity} = required {requiredLootTotal} + extra {generatedLootExtraSlots}");
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

                string itemKey = NormalizeShoppingListKey(item.definition.GetShoppingListKey());
                foreach (ShoppingListEntry entry in currentShoppingList)
                {
                    if (string.IsNullOrEmpty(itemKey))
                        continue;
                    if (string.Equals(NormalizeShoppingListKey(entry.GetShoppingListKey()), itemKey, StringComparison.Ordinal))
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

    public void RegisterCollectedByKey(string shoppingListKey, int amount, int unitValue)
    {
        string targetKey = NormalizeShoppingListKey(shoppingListKey);
        if (string.IsNullOrWhiteSpace(targetKey) || amount <= 0)
            return;

        bool changed = false;
        for (int i = 0; i < currentShoppingList.Count; i++)
        {
            ShoppingListEntry entry = currentShoppingList[i];
            if (entry == null)
                continue;
            if (!string.Equals(NormalizeShoppingListKey(entry.GetShoppingListKey()), targetKey, StringComparison.Ordinal))
                continue;
            if (entry.collectedAmount >= entry.requiredAmount)
                break;

            entry.collectedAmount += amount;
            if (entry.collectedAmount > entry.requiredAmount)
                entry.collectedAmount = entry.requiredAmount;
            changed = true;

            int valuePerItem = entry.itemDefinition != null ? entry.itemDefinition.lootValue : entry.unitValue;
            if (valuePerItem <= 0)
                valuePerItem = Mathf.Max(0, unitValue);
            currentCollectedValue += Mathf.Max(0, valuePerItem * amount);
            break;
        }

        if (changed)
        {
            OnObjectiveProgressChanged?.Invoke();
            TryShowBossUnlockedNotice();
        }
    }

    //private bool TryGenerateShoppingListFromPrefabMetadata()
    //{
    //    if (prefabSpawnCatalog == null)
    //        return false;

    //    Dictionary<string, ShoppingListEntry> uniqueByKey = new Dictionary<string, ShoppingListEntry>(StringComparer.Ordinal);
    //    foreach (RoomPrefabSpawnCatalog.RoomPool pool in prefabSpawnCatalog.Pools)
    //    {
    //        if (pool == null || pool.prefabs == null)
    //            continue;
    //        for (int i = 0; i < pool.prefabs.Count; i++)
    //        {
    //            GameObject prefab = pool.prefabs[i];
    //            if (prefab == null)
    //                continue;
    //            RoomSpawnPrefabDefinition def = prefab.GetComponent<RoomSpawnPrefabDefinition>();
    //            if (def == null)
    //            {
    //                continue;
    //            }
    //            if (def.spawnCategory != RoomSpawnCategory.Item || !def.canAppearInShoppingList)
    //                continue;
    //            string normalizedKey = NormalizeShoppingListKey(def.shoppingListKey);
    //            if (string.IsNullOrWhiteSpace(normalizedKey) || def.lootValue <= 0)
    //                continue;
    //            if (!uniqueByKey.ContainsKey(normalizedKey))
    //            {
    //                uniqueByKey[normalizedKey] = new ShoppingListEntry
    //                {
    //                    shoppingListKey = normalizedKey,
    //                    displayName = def.GetResolvedDisplayName(),
    //                    unitValue = def.lootValue,
    //                    roomType = def.roomType,
    //                    requiredAmount = 1,
    //                    collectedAmount = 0
    //                };
    //            }

    //        }
    //    }

    //    if (uniqueByKey.Count == 0)
    //        return false;

    //    List<ShoppingListEntry> poolDefs = new List<ShoppingListEntry>(uniqueByKey.Values);
    //    for (int i = poolDefs.Count - 1; i > 0; i--)
    //    {
    //        int j = UnityEngine.Random.Range(0, i + 1);
    //        (poolDefs[i], poolDefs[j]) = (poolDefs[j], poolDefs[i]);
    //    }

    //    // Design rule (for now): exactly 7 unique shopping-list lines.
    //    const int targetUniqueEntries = 7;

    //    int targetEntries = Mathf.Min(targetUniqueEntries, poolDefs.Count);

    //    for (int i = 0; i < targetEntries; i++)
    //    {
    //        ShoppingListEntry seed = poolDefs[i];
    //        seed.requiredAmount = Mathf.Max(1, seed.requiredAmount);
    //        currentShoppingList.Add(seed);
    //    }

    //    return currentShoppingList.Count > 0;
    //}
    private bool TryGenerateShoppingListFromPrefabMetadata()
    {
        if (prefabSpawnCatalog == null)
            return false;

        Dictionary<string, ShoppingListEntry> uniqueByKey =
            new Dictionary<string, ShoppingListEntry>(StringComparer.Ordinal);

        foreach (RoomPrefabSpawnCatalog.RoomPool pool in prefabSpawnCatalog.Pools)
        {
            if (pool == null || pool.prefabs == null)
                continue;

            for (int i = 0; i < pool.prefabs.Count; i++)
            {
                GameObject prefab = pool.prefabs[i];
                if (prefab == null)
                    continue;

                RoomSpawnPrefabDefinition def = prefab.GetComponent<RoomSpawnPrefabDefinition>();
                if (def == null)
                    continue;

                if (!def.isPickable)
                    continue;

                if (def.spawnCategory != RoomSpawnCategory.Item)
                    continue;

                ItemWorldSpawner spawner = prefab.GetComponent<ItemWorldSpawner>();
                if (spawner == null)
                    spawner = prefab.GetComponentInChildren<ItemWorldSpawner>(true);

                if (spawner == null)
                    continue;

                ItemDefinition itemDef = spawner.ItemDefinition;
                if (itemDef == null)
                    continue;

                if (!itemDef.IsLoot())
                    continue;

                if (!itemDef.canAppearInShoppingList)
                    continue;

                string normalizedKey = NormalizeShoppingListKey(itemDef.GetShoppingListKey());
                if (string.IsNullOrWhiteSpace(normalizedKey))
                    continue;

                if (itemDef.lootValue <= 0)
                    continue;

                if (!uniqueByKey.ContainsKey(normalizedKey))
                {
                    uniqueByKey[normalizedKey] = new ShoppingListEntry
                    {
                        itemDefinition = itemDef,
                        requiredAmount = Mathf.Max(1, itemDef.minRequiredAmount),
                        collectedAmount = 0
                    };
                }
            }
        }

        if (uniqueByKey.Count == 0)
            return false;

        Dictionary<string, int> availablePickupCounts = CountAvailableShoppingListPickupsInGeneratedMap();
        List<ShoppingListEntry> poolDefs = new List<ShoppingListEntry>(uniqueByKey.Values);
        for (int i = poolDefs.Count - 1; i >= 0; i--)
        {
            ShoppingListEntry seed = poolDefs[i];
            string key = seed?.itemDefinition != null
                ? NormalizeShoppingListKey(seed.itemDefinition.GetShoppingListKey())
                : string.Empty;

            if (string.IsNullOrWhiteSpace(key) ||
                !availablePickupCounts.TryGetValue(key, out int available) ||
                available < 1)
            {
                poolDefs.RemoveAt(i);
            }
        }

        if (poolDefs.Count == 0)
        {
            Debug.LogWarning("RunObjectiveManager: No shopping-list loot has pickups in the generated map.");
            return false;
        }

        for (int i = poolDefs.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            ShoppingListEntry temp = poolDefs[i];
            poolDefs[i] = poolDefs[j];
            poolDefs[j] = temp;
        }

        int minCap = Mathf.Max(1, Mathf.Min(minListEntries, maxListEntries));
        int maxCap = Mathf.Max(minCap, maxListEntries);
        int desiredCount = UnityEngine.Random.Range(minCap, maxCap + 1);
        int targetEntries = Mathf.Min(desiredCount, poolDefs.Count);

        for (int i = 0; i < targetEntries; i++)
        {
            ShoppingListEntry seed = poolDefs[i];
            if (seed == null || seed.itemDefinition == null)
                continue;

            ItemDefinition itemDef = seed.itemDefinition;
            string key = NormalizeShoppingListKey(itemDef.GetShoppingListKey());
            int available = availablePickupCounts.TryGetValue(key, out int count)
                ? count
                : 0;
            if (available < 1)
                continue;

            int minAmount = Mathf.Max(1, itemDef.minRequiredAmount);
            int maxAmount = Mathf.Max(minAmount, itemDef.maxRequiredAmount);
            maxAmount = Mathf.Min(maxAmount, available);
            minAmount = Mathf.Min(minAmount, maxAmount);

            ShoppingListEntry entry = new ShoppingListEntry
            {
                itemDefinition = itemDef,
                requiredAmount = UnityEngine.Random.Range(minAmount, maxAmount + 1),
                collectedAmount = 0
            };

            currentShoppingList.Add(entry);
        }

        return currentShoppingList.Count > 0;
    }

    private static Dictionary<string, int> CountAvailableShoppingListPickupsInGeneratedMap()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
        HashSet<string> countedPickupSlots = new HashSet<string>(StringComparer.Ordinal);

        RoomGeneratedPickup[] generatedPickups = UnityEngine.Object.FindObjectsByType<RoomGeneratedPickup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < generatedPickups.Length; i++)
        {
            RoomGeneratedPickup pickup = generatedPickups[i];
            if (pickup == null)
                continue;

            ItemWorldSpawner spawner = pickup.GetComponent<ItemWorldSpawner>();
            if (spawner == null)
                spawner = pickup.GetComponentInChildren<ItemWorldSpawner>(true);
            if (spawner == null || spawner.ItemDefinition == null)
                continue;

            AddPickupCountAtPosition(counts, countedPickupSlots, spawner.ItemDefinition, 1, pickup.transform.position);
        }

        ItemWorldSpawner[] itemSpawners = UnityEngine.Object.FindObjectsByType<ItemWorldSpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        HashSet<ItemWorldSpawner> countedSpawners = new HashSet<ItemWorldSpawner>();
        for (int i = 0; i < generatedPickups.Length; i++)
        {
            RoomGeneratedPickup pickup = generatedPickups[i];
            if (pickup == null)
                continue;

            ItemWorldSpawner spawner = pickup.GetComponent<ItemWorldSpawner>();
            if (spawner == null)
                spawner = pickup.GetComponentInChildren<ItemWorldSpawner>(true);
            if (spawner != null)
                countedSpawners.Add(spawner);
        }

        for (int i = 0; i < itemSpawners.Length; i++)
        {
            ItemWorldSpawner spawner = itemSpawners[i];
            if (spawner == null || countedSpawners.Contains(spawner))
                continue;

            AddPickupCountAtPosition(counts, countedPickupSlots, spawner.ItemDefinition, 1, spawner.transform.position);
        }

        ItemWorld[] itemWorlds = UnityEngine.Object.FindObjectsByType<ItemWorld>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < itemWorlds.Length; i++)
        {
            ItemWorld itemWorld = itemWorlds[i];
            if (itemWorld == null)
                continue;

            Item item = itemWorld.GetItem();
            if (item == null || item.definition == null)
                continue;

            AddPickupCountAtPosition(counts, countedPickupSlots, item.definition, Mathf.Max(1, item.amount), itemWorld.transform.position);
        }

        return counts;
    }

    private static void AddPickupCountAtPosition(
        Dictionary<string, int> counts,
        HashSet<string> countedPickupSlots,
        ItemDefinition itemDef,
        int amount,
        Vector3 position)
    {
        if (countedPickupSlots == null)
        {
            AddPickupCount(counts, itemDef, amount);
            return;
        }

        string slotKey = MakePickupSlotKey(itemDef, position);
        if (string.IsNullOrWhiteSpace(slotKey) || countedPickupSlots.Contains(slotKey))
            return;

        countedPickupSlots.Add(slotKey);
        AddPickupCount(counts, itemDef, amount);
    }

    private static string MakePickupSlotKey(ItemDefinition itemDef, Vector3 position)
    {
        if (itemDef == null)
            return string.Empty;

        string key = NormalizeShoppingListKey(itemDef.GetShoppingListKey());
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        int x = Mathf.RoundToInt(position.x * 20f);
        int y = Mathf.RoundToInt(position.y * 20f);
        return $"{key}:{x}:{y}";
    }

    private static void AddPickupCount(Dictionary<string, int> counts, ItemDefinition itemDef, int amount)
    {
        if (counts == null || itemDef == null || amount <= 0)
            return;
        if (!itemDef.IsLoot() || !itemDef.canAppearInShoppingList || itemDef.lootValue <= 0)
            return;

        string key = NormalizeShoppingListKey(itemDef.GetShoppingListKey());
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!counts.ContainsKey(key))
            counts[key] = 0;
        counts[key] += amount;
    }

    private void ClampCurrentShoppingListToGeneratedPickupCounts()
    {
        Dictionary<string, int> availablePickupCounts = CountAvailableShoppingListPickupsInGeneratedMap();

        for (int i = currentShoppingList.Count - 1; i >= 0; i--)
        {
            ShoppingListEntry entry = currentShoppingList[i];
            string key = entry?.itemDefinition != null
                ? NormalizeShoppingListKey(entry.itemDefinition.GetShoppingListKey())
                : NormalizeShoppingListKey(entry?.GetShoppingListKey());

            if (string.IsNullOrWhiteSpace(key) ||
                !availablePickupCounts.TryGetValue(key, out int available) ||
                available < 1)
            {
                currentShoppingList.RemoveAt(i);
                continue;
            }

            entry.requiredAmount = Mathf.Min(Mathf.Max(1, entry.requiredAmount), available);
        }
    }
    private static void ShuffleShoppingListEntries(List<ShoppingListEntry> list)
    {
        if (list == null || list.Count < 2)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            ShoppingListEntry temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

    }
    private void OnValidate()
    {
        AutoPopulateAllItemDefinitionsFromPrefabCatalog();
    }

    [ContextMenu("Auto Populate All Item Definitions From Prefab Catalog")]
    private void AutoPopulateAllItemDefinitionsFromPrefabCatalog()
    {
        if (prefabSpawnCatalog == null)
            return;

        HashSet<ItemDefinition> uniqueDefs = new HashSet<ItemDefinition>();

        foreach (RoomPrefabSpawnCatalog.RoomPool pool in prefabSpawnCatalog.Pools)
        {
            if (pool == null || pool.prefabs == null)
                continue;

            for (int i = 0; i < pool.prefabs.Count; i++)
            {
                GameObject prefab = pool.prefabs[i];
                if (prefab == null)
                    continue;

                RoomSpawnPrefabDefinition def = prefab.GetComponent<RoomSpawnPrefabDefinition>();
                if (def == null)
                    continue;

                if (!def.isPickable)
                    continue;

                if (def.spawnCategory != RoomSpawnCategory.Item)
                    continue;

                ItemWorldSpawner spawner = prefab.GetComponent<ItemWorldSpawner>();
                if (spawner == null)
                    spawner = prefab.GetComponentInChildren<ItemWorldSpawner>(true);

                if (spawner == null)
                    continue;

                ItemDefinition itemDef = spawner.ItemDefinition;
                if (itemDef == null)
                    continue;

                uniqueDefs.Add(itemDef);
            }
        }

        allItemDefinitions = uniqueDefs
            .Where(d => d != null)
            .OrderBy(d => d.itemName)
            .ToList();
    }

    private static string NormalizeShoppingListKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
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
