using System;
using System.Collections.Generic;
using UnityEngine;

public class LootSpawnManager : MonoBehaviour
{
    public static LootSpawnManager Instance { get; private set; }

    private readonly List<LootSpawnArea> allSpawnAreas = new List<LootSpawnArea>();

    private enum LootSpawnPurpose
    {
        Required,
        Bonus
    }

    [Header("Spawn Validation")]
    [SerializeField] private LayerMask blockedLayerMask;
    [SerializeField] private float spawnCheckRadius = 0.25f;
    [SerializeField] private int maxPointAttemptsPerSpawn = 12;

    [Header("Required Loot Guarantee")]
    [SerializeField] private int extraRequiredSpawnBuffer = 2;
    [SerializeField] private int maxAttemptsPerRequiredGroup = 120;

    /// <summary>Extra copies spawned beyond shopping-list required amount (same key).</summary>
    public int ExtraRequiredSpawnBuffer => extraRequiredSpawnBuffer;

    /// <summary>
    /// Distinct loot areas that can accept required spawns for this definition (cafeteria excluded; matches required-spawn rules).
    /// At most one pickup of the same definition is placed per room, so required + buffer must not exceed this count.
    /// </summary>
    public int CountAreasThatCanHoldRequiredLoot(ItemDefinition def)
    {
        if (def == null || def.allowedRoomTypes == null || def.allowedRoomTypes.Count == 0)
            return 0;

        RefreshSpawnAreas();

        int n = 0;
        foreach (LootSpawnArea area in allSpawnAreas)
        {
            if (area == null)
                continue;
            if (area.RoomType == RoomType.Cafeteria)
                continue;
            if (!def.allowedRoomTypes.Contains(area.RoomType))
                continue;
            n++;
        }

        return n;
    }

    /// <summary>
    /// Required-count buffer to apply in shopping-list sizing for this definition.
    /// Catalog-delegated single-room loot (e.g. sports decorations) uses 0 buffer.
    /// </summary>
    public int GetRequiredBufferForDefinition(ItemDefinition def)
    {
        if (def == null)
            return extraRequiredSpawnBuffer;

        string key = def.GetShoppingListKey();
        return IsSingleRoomCatalogDelegatedLoot(def, key) ? 0 : extraRequiredSpawnBuffer;
    }

    /// <summary>Loot areas registered after <see cref="RefreshSpawnAreas"/> (zero before rooms are instantiated).</summary>
    public int LootSpawnAreaCount => allSpawnAreas.Count;

    [Header("Bonus Loot")]
    [SerializeField] private int bonusValueBuffer = 1500;
    [SerializeField] private int maxBonusSpawnAttempts = 50;

    [Header("Decoration catalog")]
    [Tooltip("Same asset as MapManager uses. When set, bonus loot only rolls shopping-list keys, and single-room list-gated catalog keys skip duplicate LootSpawnManager required spawns.")]
    [SerializeField] private RoomDecorationCatalog decorationCatalog;

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
            return;
        }

        if (allLootDefinitions == null || allLootDefinitions.Count == 0)
        {
            return;
        }

        Dictionary<string, int> spawnedCountByKey = new Dictionary<string, int>();
        Dictionary<string, int> lootValueByKey = new Dictionary<string, int>();

        SeedSpawnedCountsFromPickupsAlreadyInScene(shoppingList, spawnedCountByKey);

        int guaranteedValue = 0;
        foreach (ShoppingListEntry e in shoppingList)
        {
            if (e?.itemDefinition == null)
                continue;

            string k = e.itemDefinition.GetShoppingListKey();
            int c = GetSpawnedCountForKey(spawnedCountByKey, k);
            guaranteedValue += c * e.itemDefinition.lootValue;
        }

        HashSet<string> shoppingListKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (ShoppingListEntry e in shoppingList)
        {
            if (e?.itemDefinition != null)
                shoppingListKeys.Add(e.itemDefinition.GetShoppingListKey());
        }

        // 1. Guarantee shopping list loot counts (required + buffer), except single-room catalog–delegated keys (catalog + seed only).
        foreach (ShoppingListEntry entry in shoppingList)
        {
            if (entry == null || entry.itemDefinition == null) continue;

            string key = entry.itemDefinition.GetShoppingListKey();

            if (IsSingleRoomCatalogDelegatedLoot(entry.itemDefinition, key))
            {
                int seeded = GetSpawnedCountForKey(spawnedCountByKey, key);
                if (seeded >= entry.requiredAmount)
                    continue;
            }

            int targetCount = entry.requiredAmount + extraRequiredSpawnBuffer;
            if (IsSingleRoomCatalogDelegatedLoot(entry.itemDefinition, key))
                targetCount = entry.requiredAmount;

            List<ItemDefinition> groupPool = GetDefinitionsByShoppingListKey(key, allLootDefinitions);

            if (groupPool.Count == 0)
            {
                Debug.LogError($"LootSpawnManager: No loot definitions found for shoppingListKey = {key}");
                continue;
            }

            if (!lootValueByKey.ContainsKey(key))
            {
                lootValueByKey[key] = entry.itemDefinition.lootValue;
            }

            int currentCount = GetSpawnedCountForKey(spawnedCountByKey, key);
            int attempts = 0;

            while (currentCount < targetCount && attempts < maxAttemptsPerRequiredGroup)
            {
                attempts++;

                ItemDefinition chosenVariant = GetRandomDefinitionFromGroup(groupPool);
                if (chosenVariant == null)
                {
                    break;
                }

                bool success = SpawnOneLoot(chosenVariant, LootSpawnPurpose.Required);
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

        // 2. ���׹���֮����ˢ bonus value
        int targetTotalValue = requiredGoalValue + bonusValueBuffer;
        int currentPotentialValue = guaranteedValue;
        int attemptsForBonus = 0;

        List<ItemDefinition> bonusPool = BuildBonusPool(allLootDefinitions, shoppingListKeys);

        while (currentPotentialValue < targetTotalValue && attemptsForBonus < maxBonusSpawnAttempts)
        {
            attemptsForBonus++;

            ItemDefinition bonusLoot = GetWeightedRandomBonusLoot(bonusPool);
            if (bonusLoot == null)
            {
                break;
            }

            bool success = SpawnOneLoot(bonusLoot, LootSpawnPurpose.Bonus);
            if (success)
            {
                currentPotentialValue += bonusLoot.lootValue;
            }
        }

        Debug.Log($"LootSpawnManager: Spawn generation complete. Potential value = {currentPotentialValue}, target = {targetTotalValue}");

        // 3. �����һ�� required У��
        foreach (ShoppingListEntry entry in shoppingList)
        {
            if (entry == null || entry.itemDefinition == null) continue;

            string key = entry.itemDefinition.GetShoppingListKey();
            int minimumRequired = entry.requiredAmount + extraRequiredSpawnBuffer;
            if (IsSingleRoomCatalogDelegatedLoot(entry.itemDefinition, key))
                minimumRequired = entry.requiredAmount;

            int spawned = GetSpawnedCountForKey(spawnedCountByKey, key);

            if (spawned < minimumRequired)
            {
                Debug.LogError(
                    $"LootSpawnManager: Final validation failed for key {key}. Spawned={spawned}, MinimumRequired={minimumRequired}"
                );
            }
        }
    }

    private bool IsSingleRoomCatalogDelegatedLoot(ItemDefinition def, string shoppingListKey)
    {
        if (decorationCatalog == null || def == null || string.IsNullOrEmpty(shoppingListKey))
            return false;

        if (def.allowedRoomTypes == null || def.allowedRoomTypes.Count != 1)
            return false;

        return decorationCatalog.HasListGatedCatalogPickupForRoom(shoppingListKey, def.allowedRoomTypes[0]);
    }

    public void SpawnAdditionalBonusLoot(int extraSpawnCount, List<ItemDefinition> allLootDefinitions)
    {
        RefreshSpawnAreas();

        if (allLootDefinitions == null || allLootDefinitions.Count == 0)
        {
            Debug.LogWarning("LootSpawnManager: No loot definitions available for bonus refresh.");
            return;
        }

        HashSet<string> shoppingListKeys = new HashSet<string>(StringComparer.Ordinal);
        if (RunObjectiveManager.Instance != null)
        {
            foreach (ShoppingListEntry e in RunObjectiveManager.Instance.CurrentShoppingList)
            {
                if (e?.itemDefinition != null)
                    shoppingListKeys.Add(e.itemDefinition.GetShoppingListKey());
            }
        }

        List<ItemDefinition> bonusPool = BuildBonusPool(allLootDefinitions, shoppingListKeys);

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

            bool success = SpawnOneLoot(bonusLoot, LootSpawnPurpose.Bonus);
            if (success)
            {
                spawnedCount++;
            }
        }

        Debug.Log($"LootSpawnManager: Spawned additional bonus loot = {spawnedCount}/{extraSpawnCount}");
    }

    private List<ItemDefinition> BuildBonusPool(List<ItemDefinition> allLootDefinitions, HashSet<string> shoppingListKeys)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();

        if (allLootDefinitions == null) return result;

        foreach (ItemDefinition itemDef in allLootDefinitions)
        {
            if (itemDef == null) continue;
            if (!itemDef.IsLoot()) continue;
            if (itemDef.lootValue <= 0) continue;
            if (itemDef.allowedRoomTypes == null || itemDef.allowedRoomTypes.Count == 0) continue;

            if (shoppingListKeys != null && shoppingListKeys.Count > 0 &&
                !shoppingListKeys.Contains(itemDef.GetShoppingListKey()))
                continue;

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

        int roll = UnityEngine.Random.Range(0, totalWeight);
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

    private List<ItemDefinition> GetDefinitionsByShoppingListKey(string key, List<ItemDefinition> allLootDefinitions)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();

        if (string.IsNullOrWhiteSpace(key) || allLootDefinitions == null)
        {
            return result;
        }

        foreach (ItemDefinition def in allLootDefinitions)
        {
            if (def == null) continue;
            if (!def.IsLoot()) continue;
            if (def.lootValue <= 0) continue;
            if (def.allowedRoomTypes == null || def.allowedRoomTypes.Count == 0) continue;

            if (def.GetShoppingListKey() == key)
            {
                result.Add(def);
            }
        }

        return result;
    }

    private ItemDefinition GetRandomDefinitionFromGroup(List<ItemDefinition> groupPool)
    {
        if (groupPool == null || groupPool.Count == 0)
        {
            return null;
        }

        int index = UnityEngine.Random.Range(0, groupPool.Count);
        return groupPool[index];
    }

    private static void SeedSpawnedCountsFromPickupsAlreadyInScene(
        IReadOnlyList<ShoppingListEntry> shoppingList,
        Dictionary<string, int> spawnedCountByKey)
    {
        if (shoppingList == null || spawnedCountByKey == null)
            return;

        Dictionary<string, int> byKey = new Dictionary<string, int>();
        ItemWorld[] worlds = UnityEngine.Object.FindObjectsByType<ItemWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < worlds.Length; i++)
        {
            ItemWorld iw = worlds[i];
            if (iw == null)
                continue;
            Item it = iw.GetItem();
            if (it == null || it.definition == null || !it.definition.IsLoot())
                continue;
            string key = it.definition.GetShoppingListKey();
            int amt = Mathf.Max(1, it.amount);
            if (!byKey.ContainsKey(key))
                byKey[key] = 0;
            byKey[key] += amt;
        }

        for (int i = 0; i < shoppingList.Count; i++)
        {
            ShoppingListEntry e = shoppingList[i];
            if (e == null || e.itemDefinition == null)
                continue;
            string k = e.itemDefinition.GetShoppingListKey();
            if (byKey.TryGetValue(k, out int n))
                spawnedCountByKey[k] = n;
        }
    }

    private int GetSpawnedCountForKey(Dictionary<string, int> spawnedCountByKey, string key)
    {
        if (spawnedCountByKey == null || string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        if (spawnedCountByKey.TryGetValue(key, out int count))
        {
            return count;
        }

        return 0;
    }

    private bool SpawnOneLoot(ItemDefinition itemDefinition, LootSpawnPurpose purpose)
    {
        if (itemDefinition == null) return false;
        if (itemDefinition.allowedRoomTypes == null || itemDefinition.allowedRoomTypes.Count == 0) return false;

        List<LootSpawnArea> validAreas = GetValidAreasForLoot(itemDefinition, purpose);

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

    private List<LootSpawnArea> GetValidAreasForLoot(ItemDefinition itemDefinition, LootSpawnPurpose purpose)
    {
        List<LootSpawnArea> result = new List<LootSpawnArea>();

        foreach (LootSpawnArea area in allSpawnAreas)
        {
            if (area == null) continue;
            if (!area.CanSpawn()) continue;
            if (area.RoomType == RoomType.Cafeteria)
                continue;
            if (purpose == LootSpawnPurpose.Bonus && area.RoomType == RoomType.SportsRoom)
                continue;

            if (itemDefinition.allowedRoomTypes.Contains(area.RoomType))
            {
                result.Add(area);
            }
        }

        return result;
    }

    private bool TrySpawnInArea(ItemDefinition itemDefinition, LootSpawnArea area)
    {
        Room room = area != null ? area.GetComponentInParent<Room>() : null;
        if (room != null &&
            RoomItemWorldQuery.RoomHasDefinitionInPickupScopes(room.gameObject, itemDefinition))
            return false;

        Vector2 footprint = itemDefinition.spawnFootprint;

        for (int i = 0; i < maxPointAttemptsPerSpawn; i++)
        {
            Vector2 point;
            if (area.RoomType == RoomType.SportsRoom && room != null)
                point = GetSportsAnchoredLootPoint(room.transform, i, maxPointAttemptsPerSpawn);
            else
                point = area.GetRandomPoint(footprint);

            if (!IsSpawnPointValid(point, footprint))
            {
                continue;
            }

            SpawnLootObject(itemDefinition, point, area);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Required shopping-list loot in the sports room uses the same wall anchors as decorations so props are not random mid-floor.
    /// </summary>
    private static Vector2 GetSportsAnchoredLootPoint(Transform roomRoot, int attemptIndex, int maxAttempts)
    {
        // Fixed center-right spot to keep sports layout deterministic and clean.
        Vector3 world = RoomDecorationPlacer.GetAnchoredWorldPosition(
            roomRoot,
            RoomDecorInteriorAnchor.InteriorBottomRight,
            new Vector3(-0.2f, 0.2f, 0f));
        return new Vector2(world.x, world.y);
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
        {
            parent = area.SpawnParent;
        }

        Vector3 lootScale = itemDefinition.worldDropScale * ItemWorldSpawner.RoomPickupWorldScale;

        Item item = new Item
        {
            definition = itemDefinition,
            amount = 1,
            worldScale = lootScale
        };

        ItemWorld itemWorld = ItemWorld.SpawnItemWorld(
            position,
            Quaternion.identity,
            lootScale,
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
            room.RefreshRendererRegistry();
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

}