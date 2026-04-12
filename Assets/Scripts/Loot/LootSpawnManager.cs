using System.Collections.Generic;
using UnityEngine;

public class LootSpawnManager : MonoBehaviour
{
    public static LootSpawnManager Instance { get; private set; }

    [Header("Spawn Validation")]
    [SerializeField] private LayerMask blockedLayerMask;
    [SerializeField] private float spawnCheckRadius = 0.25f;
    [SerializeField] private int maxPointAttemptsPerSpawn = 12;

    [Header("Required Loot Guarantee")]
    [SerializeField] private int extraRequiredSpawnBuffer = 2;
    [SerializeField] private int maxAttemptsPerRequiredGroup = 30;

    [Header("Bonus Loot")]
    [SerializeField] private int bonusValueBuffer = 1500;
    [SerializeField] private int maxBonusSpawnAttempts = 50;

    private readonly List<LootSpawnArea> allSpawnAreas = new List<LootSpawnArea>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RefreshSpawnAreas();
    }

    [ContextMenu("Refresh Spawn Areas")]
    public void RefreshSpawnAreas()
    {
        allSpawnAreas.Clear();

        LootSpawnArea[] areas = FindObjectsByType<LootSpawnArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LootSpawnArea area in areas)
        {
            if (area == null)
                continue;

            if (area.RoomType == RoomType.Cafeteria)
                continue;

            area.ResetSpawnCount();
            allSpawnAreas.Add(area);
        }
    }

    public void ClearSpawnCounts()
    {
        foreach (LootSpawnArea area in allSpawnAreas)
        {
            if (area != null)
                area.ResetSpawnCount();
        }
    }

    public void GenerateLootForObjective(IReadOnlyList<ShoppingListEntry> shoppingList, int requiredGoalValue, List<ItemDefinition> allLootDefinitions)
    {
        RefreshSpawnAreas();
        ClearSpawnCounts();

        if (shoppingList == null || shoppingList.Count == 0)
        {
            return;
        }

        if (allLootDefinitions == null || allLootDefinitions.Count == 0)
        {
            return;
        }

        Dictionary<string, int> spawnedCountByKey = new Dictionary<string, int>();

        int guaranteedValue = 0;

        foreach (ShoppingListEntry entry in shoppingList)
        {
            if (entry == null || entry.itemDefinition == null)
                continue;

            string key = entry.itemDefinition.GetShoppingListKey();
            int targetCount = entry.requiredAmount + extraRequiredSpawnBuffer;

            List<ItemDefinition> groupPool = GetDefinitionsByShoppingListKey(key, allLootDefinitions);

            if (groupPool.Count == 0)
            {
                Debug.LogError($"LootSpawnManager: No loot definitions found for shoppingListKey = {key}");
                continue;
            }

            int currentCount = GetSpawnedCountForKey(spawnedCountByKey, key);
            int attempts = 0;

            while (currentCount < targetCount && attempts < maxAttemptsPerRequiredGroup)
            {
                attempts++;

                ItemDefinition chosenVariant = GetRandomDefinitionFromGroup(groupPool);
                if (chosenVariant == null)
                    break;

                bool success = SpawnOneLoot(chosenVariant);
                if (success)
                {
                    currentCount++;
                    spawnedCountByKey[key] = currentCount;
                    guaranteedValue += chosenVariant.lootValue;
                }
            }

            if (currentCount < targetCount)
            {
                Debug.LogError(
                    $"LootSpawnManager: Failed to guarantee enough loot for key {key}. Spawned={currentCount}, Target={targetCount}"
                );
            }
        }

        int targetTotalValue = requiredGoalValue + bonusValueBuffer;
        int currentPotentialValue = guaranteedValue;
        int attemptsForBonus = 0;

        List<ItemDefinition> bonusPool = BuildBonusPool(allLootDefinitions);

        while (currentPotentialValue < targetTotalValue && attemptsForBonus < maxBonusSpawnAttempts)
        {
            attemptsForBonus++;

            ItemDefinition bonusLoot = GetWeightedRandomBonusLoot(bonusPool);
            if (bonusLoot == null)
                break;

            bool success = SpawnOneLoot(bonusLoot);
            if (success)
                currentPotentialValue += bonusLoot.lootValue;
        }

        Debug.Log($"LootSpawnManager: Spawn generation complete. Potential value = {currentPotentialValue}, target = {targetTotalValue}");

        foreach (ShoppingListEntry entry in shoppingList)
        {
            if (entry == null || entry.itemDefinition == null)
                continue;

            string key = entry.itemDefinition.GetShoppingListKey();
            int minimumRequired = entry.requiredAmount + extraRequiredSpawnBuffer;
            int spawned = GetSpawnedCountForKey(spawnedCountByKey, key);

            if (spawned < minimumRequired)
            {
                Debug.LogError(
                    $"LootSpawnManager: Final validation failed for key {key}. Spawned={spawned}, MinimumRequired={minimumRequired}"
                );
            }
        }
    }

    public void SpawnAdditionalBonusLoot(int extraSpawnCount, List<ItemDefinition> allLootDefinitions)
    {
        RefreshSpawnAreas();

        if (allLootDefinitions == null || allLootDefinitions.Count == 0)
        {
            Debug.LogWarning("LootSpawnManager: No loot definitions available for bonus refresh.");
            return;
        }

        List<ItemDefinition> bonusPool = BuildBonusPool(allLootDefinitions);

        if (bonusPool.Count == 0)
        {
            Debug.LogWarning("LootSpawnManager: Bonus pool is empty.");
            return;
        }

        int spawnedCount = 0;
        int attempts = 0;
        int maxAttempts = extraSpawnCount * 10;

        while (spawnedCount < extraSpawnCount && attempts < maxAttempts)
        {
            attempts++;

            ItemDefinition bonusLoot = GetWeightedRandomBonusLoot(bonusPool);
            if (bonusLoot == null)
                break;

            bool success = SpawnOneLoot(bonusLoot);
            if (success)
                spawnedCount++;
        }

        Debug.Log($"LootSpawnManager: Spawned additional bonus loot = {spawnedCount}/{extraSpawnCount}");
    }

    private List<ItemDefinition> BuildBonusPool(List<ItemDefinition> allLootDefinitions)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();

        if (allLootDefinitions == null)
            return result;

        foreach (ItemDefinition itemDef in allLootDefinitions)
        {
            if (itemDef == null)
                continue;
            if (!itemDef.IsLoot())
                continue;
            if (itemDef.lootValue <= 0)
                continue;
            if (itemDef.allowedRoomTypes == null || itemDef.allowedRoomTypes.Count == 0)
                continue;

            result.Add(itemDef);
        }

        return result;
    }

    private ItemDefinition GetWeightedRandomBonusLoot(List<ItemDefinition> bonusPool)
    {
        if (bonusPool == null || bonusPool.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (ItemDefinition itemDef in bonusPool)
            totalWeight += Mathf.Max(1, itemDef.bonusSpawnWeight);

        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (ItemDefinition itemDef in bonusPool)
        {
            current += Mathf.Max(1, itemDef.bonusSpawnWeight);

            if (roll < current)
                return itemDef;
        }

        return bonusPool[bonusPool.Count - 1];
    }

    private List<ItemDefinition> GetDefinitionsByShoppingListKey(string key, List<ItemDefinition> allLootDefinitions)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();

        if (string.IsNullOrWhiteSpace(key) || allLootDefinitions == null)
            return result;

        foreach (ItemDefinition def in allLootDefinitions)
        {
            if (def == null)
                continue;
            if (!def.IsLoot())
                continue;
            if (def.lootValue <= 0)
                continue;
            if (def.allowedRoomTypes == null || def.allowedRoomTypes.Count == 0)
                continue;

            if (def.GetShoppingListKey() == key)
                result.Add(def);
        }

        return result;
    }

    private ItemDefinition GetRandomDefinitionFromGroup(List<ItemDefinition> groupPool)
    {
        if (groupPool == null || groupPool.Count == 0)
            return null;

        int index = Random.Range(0, groupPool.Count);
        return groupPool[index];
    }

    private int GetSpawnedCountForKey(Dictionary<string, int> spawnedCountByKey, string key)
    {
        if (spawnedCountByKey == null || string.IsNullOrWhiteSpace(key))
            return 0;

        if (spawnedCountByKey.TryGetValue(key, out int count))
            return count;

        return 0;
    }

    private bool SpawnOneLoot(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
            return false;
        if (itemDefinition.allowedRoomTypes == null || itemDefinition.allowedRoomTypes.Count == 0)
            return false;

        List<LootSpawnArea> validAreas = GetValidAreasForLoot(itemDefinition);

        if (validAreas.Count == 0)
        {
            Debug.LogWarning($"LootSpawnManager: No valid spawn area found for {itemDefinition.itemName}.");
            return false;
        }

        Shuffle(validAreas);

        foreach (LootSpawnArea area in validAreas)
        {
            if (TrySpawnInArea(itemDefinition, area))
            {
                area.RegisterSpawn();
                return true;
            }
        }

        Debug.LogWarning($"LootSpawnManager: Could not find valid point for {itemDefinition.itemName} in any valid area.");
        return false;
    }

    private List<LootSpawnArea> GetValidAreasForLoot(ItemDefinition itemDefinition)
    {
        List<LootSpawnArea> result = new List<LootSpawnArea>();

        foreach (LootSpawnArea area in allSpawnAreas)
        {
            if (area == null)
                continue;
            if (!area.CanSpawn())
                continue;
            if (area.RoomType == RoomType.Cafeteria)
                continue;

            if (itemDefinition.allowedRoomTypes.Contains(area.RoomType))
                result.Add(area);
        }

        return result;
    }

    private bool TrySpawnInArea(ItemDefinition itemDefinition, LootSpawnArea area)
    {
        Vector2 footprint = itemDefinition.spawnFootprint;

        for (int i = 0; i < maxPointAttemptsPerSpawn; i++)
        {
            Vector2 point = area.GetRandomPoint(footprint);

            if (!IsSpawnPointValid(point, footprint))
                continue;

            SpawnLootObject(itemDefinition, point, area);
            return true;
        }

        return false;
    }

    private bool IsSpawnPointValid(Vector2 point, Vector2 footprint)
    {
        Collider2D hit = Physics2D.OverlapBox(point, footprint, 0f, blockedLayerMask);
        return hit == null;
    }

    private void SpawnLootObject(ItemDefinition itemDefinition, Vector2 position, LootSpawnArea area)
    {
        Transform parent = null;

        if (area != null && area.SpawnParent != null)
            obj.transform.SetParent(area.SpawnParent, true);

        Item item = new Item
        {
            definition = itemDefinition,
            amount = 1,
            worldScale = itemDefinition.worldDropScale
        };

        ItemWorld itemWorld = ItemWorld.SpawnItemWorld(
            position,
            Quaternion.identity,
            item.worldScale,
            item
        );

        if (itemWorld == null) return;

        // parent
        if (parent != null)
        {
            itemWorld.transform.SetParent(parent, true);
        }

        // room
        Room room = area != null ? area.GetComponentInParent<Room>() : null;
        if (room != null)
        {
            itemWorld.SetRoom(room);
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
