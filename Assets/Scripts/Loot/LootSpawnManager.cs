using System.Collections.Generic;
using UnityEngine;

public class LootSpawnManager : MonoBehaviour
{
    public static LootSpawnManager Instance { get; private set; }

    [Header("Spawn Validation")]
    [SerializeField] private LayerMask blockedLayerMask;
    [SerializeField] private float spawnCheckRadius = 0.25f;
    [SerializeField] private int maxPointAttemptsPerSpawn = 12;

    [Header("Bonus Loot")]
    [SerializeField] private int bonusValueBuffer = 500;
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
            if (area != null)
            {
                area.ResetSpawnCount();
                allSpawnAreas.Add(area);
            }
        }
    }

    public void ClearSpawnCounts()
    {
        foreach (LootSpawnArea area in allSpawnAreas)
        {
            if (area != null)
            {
                area.ResetSpawnCount();
            }
        }
    }

    public void GenerateLootForObjective(IReadOnlyList<ShoppingListEntry> shoppingList, int requiredGoalValue, List<ItemDefinition> allLootDefinitions)
    {
        RefreshSpawnAreas();
        ClearSpawnCounts();

        if (shoppingList == null || shoppingList.Count == 0)
        {
            Debug.LogWarning("LootSpawnManager: shopping list is empty.");
            return;
        }

        int guaranteedValue = 0;

        foreach (ShoppingListEntry entry in shoppingList)
        {
            if (entry == null || entry.itemDefinition == null) continue;

            for (int i = 0; i < entry.requiredAmount; i++)
            {
                bool success = SpawnOneLoot(entry.itemDefinition);

                if (!success)
                {
                    Debug.LogError($"LootSpawnManager: Failed to spawn required loot {entry.itemDefinition.itemName}.");
                }
                else
                {
                    guaranteedValue += entry.itemDefinition.lootValue;
                }
            }
        }

        int targetTotalValue = requiredGoalValue + bonusValueBuffer;
        int currentPotentialValue = guaranteedValue;
        int attempts = 0;

        List<ItemDefinition> bonusPool = BuildBonusPool(allLootDefinitions);

        while (currentPotentialValue < targetTotalValue && attempts < maxBonusSpawnAttempts)
        {
            attempts++;

            ItemDefinition bonusLoot = GetWeightedRandomBonusLoot(bonusPool);
            if (bonusLoot == null)
            {
                break;
            }

            bool success = SpawnOneLoot(bonusLoot);
            if (success)
            {
                currentPotentialValue += bonusLoot.lootValue;
            }
        }

        Debug.Log($"LootSpawnManager: Spawn generation complete. Potential value = {currentPotentialValue}, target = {targetTotalValue}");
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
            {
                break;
            }

            bool success = SpawnOneLoot(bonusLoot);
            if (success)
            {
                spawnedCount++;
            }
        }

        Debug.Log($"LootSpawnManager: Spawned additional bonus loot = {spawnedCount}/{extraSpawnCount}");
    }

    private List<ItemDefinition> BuildBonusPool(List<ItemDefinition> allLootDefinitions)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();

        if (allLootDefinitions == null) return result;

        foreach (ItemDefinition itemDef in allLootDefinitions)
        {
            if (itemDef == null) continue;
            if (!itemDef.IsLoot()) continue;
            if (itemDef.lootValue <= 0) continue;
            if (itemDef.allowedRoomTypes == null || itemDef.allowedRoomTypes.Count == 0) continue;

            result.Add(itemDef);
        }

        return result;
    }

    private ItemDefinition GetWeightedRandomBonusLoot(List<ItemDefinition> bonusPool)
    {
        if (bonusPool == null || bonusPool.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        foreach (ItemDefinition itemDef in bonusPool)
        {
            totalWeight += Mathf.Max(1, itemDef.bonusSpawnWeight);
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        int current = 0;

        foreach (ItemDefinition itemDef in bonusPool)
        {
            current += Mathf.Max(1, itemDef.bonusSpawnWeight);

            if (roll < current)
            {
                return itemDef;
            }
        }

        return bonusPool[bonusPool.Count - 1];
    }

    private bool SpawnOneLoot(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null) return false;
        if (itemDefinition.lootWorldPrefab == null) return false;
        if (itemDefinition.allowedRoomTypes == null || itemDefinition.allowedRoomTypes.Count == 0) return false;

        List<LootSpawnArea> validAreas = GetValidAreasForLoot(itemDefinition);

        if (validAreas.Count == 0)
        {
            Debug.LogWarning($"LootSpawnManager: No valid spawn area found for {itemDefinition.itemName}.");
            return false;
        }

        // 打乱顺序，避免老刷第一个
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
            if (area == null) continue;
            if (!area.CanSpawn()) continue;

            if (itemDefinition.allowedRoomTypes.Contains(area.RoomType))
            {
                result.Add(area);
            }
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
            {
                continue;
            }

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
        GameObject obj = Instantiate(itemDefinition.lootWorldPrefab, position, Quaternion.identity);

        if (area != null && area.SpawnParent != null)
        {
            obj.transform.SetParent(area.SpawnParent, true);
        }

        ItemWorld itemWorld = obj.GetComponent<ItemWorld>();
        if (itemWorld != null)
        {
            Item item = new Item
            {
                definition = itemDefinition,
                amount = 1,
                worldScale = Vector3.one
            };

            itemWorld.SetItem(item);
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}