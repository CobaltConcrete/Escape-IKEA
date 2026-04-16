using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds <see cref="ItemWorld"/> / <see cref="ItemWorldSpawner"/> only under room pickup/loot roots
/// (not whole-scene scans), so random room items and loot are not blocked by unrelated hierarchy.
/// </summary>
public static class RoomItemWorldQuery
{
    private const string SpawnedItemsName = "SpawnedItems";

    public static HashSet<ItemDefinition> CollectDefinitionsInPickupScopes(GameObject roomRoot)
    {
        HashSet<ItemDefinition> set = new HashSet<ItemDefinition>();
        if (roomRoot == null)
            return set;

        Transform spawnedItems = roomRoot.transform.Find(SpawnedItemsName);
        if (spawnedItems != null)
        {
            AddItemWorldDefinitionsUnder(spawnedItems, set);
            AddSpawnerDefinitionsUnder(spawnedItems, set);
        }

        LootSpawnArea[] areas = roomRoot.GetComponentsInChildren<LootSpawnArea>(true);
        for (int i = 0; i < areas.Length; i++)
        {
            LootSpawnArea area = areas[i];
            if (area == null)
                continue;
            Transform spawnParent = area.SpawnParent;
            if (spawnParent == null)
                continue;
            AddItemWorldDefinitionsUnder(spawnParent, set);
            AddSpawnerDefinitionsUnder(spawnParent, set);
        }

        return set;
    }

    public static bool RoomHasDefinitionInPickupScopes(GameObject roomRoot, ItemDefinition itemDefinition)
    {
        if (roomRoot == null || itemDefinition == null)
            return false;

        Transform spawnedItems = roomRoot.transform.Find(SpawnedItemsName);
        if (spawnedItems != null)
        {
            if (ContainsDefinitionUnder(spawnedItems, itemDefinition))
                return true;
        }

        LootSpawnArea[] areas = roomRoot.GetComponentsInChildren<LootSpawnArea>(true);
        for (int i = 0; i < areas.Length; i++)
        {
            Transform spawnParent = areas[i] != null ? areas[i].SpawnParent : null;
            if (spawnParent == null)
                continue;
            if (ContainsDefinitionUnder(spawnParent, itemDefinition))
                return true;
        }

        return false;
    }

    public static bool RoomHasShoppingListKeyInPickupScopes(GameObject roomRoot, string shoppingListKey)
    {
        if (roomRoot == null || string.IsNullOrEmpty(shoppingListKey))
            return false;

        Transform spawnedItems = roomRoot.transform.Find(SpawnedItemsName);
        if (spawnedItems != null)
        {
            if (ContainsShoppingListKeyUnder(spawnedItems, shoppingListKey))
                return true;
        }

        LootSpawnArea[] areas = roomRoot.GetComponentsInChildren<LootSpawnArea>(true);
        for (int i = 0; i < areas.Length; i++)
        {
            Transform spawnParent = areas[i] != null ? areas[i].SpawnParent : null;
            if (spawnParent == null)
                continue;
            if (ContainsShoppingListKeyUnder(spawnParent, shoppingListKey))
                return true;
        }

        return false;
    }

    private static void AddItemWorldDefinitionsUnder(Transform root, HashSet<ItemDefinition> set)
    {
        ItemWorld[] worlds = root.GetComponentsInChildren<ItemWorld>(true);
        for (int i = 0; i < worlds.Length; i++)
        {
            ItemWorld iw = worlds[i];
            if (iw == null)
                continue;
            Item it = iw.GetItem();
            if (it != null && it.definition != null)
                set.Add(it.definition);
        }
    }

    private static void AddSpawnerDefinitionsUnder(Transform root, HashSet<ItemDefinition> set)
    {
        ItemWorldSpawner[] spawners = root.GetComponentsInChildren<ItemWorldSpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            ItemWorldSpawner sp = spawners[i];
            if (sp == null)
                continue;
            ItemDefinition def = sp.ItemDefinition;
            if (def != null)
                set.Add(def);
        }
    }

    private static bool ContainsDefinitionUnder(Transform root, ItemDefinition itemDefinition)
    {
        ItemWorld[] worlds = root.GetComponentsInChildren<ItemWorld>(true);
        for (int i = 0; i < worlds.Length; i++)
        {
            ItemWorld iw = worlds[i];
            if (iw == null)
                continue;
            Item it = iw.GetItem();
            if (it != null && it.definition == itemDefinition)
                return true;
        }

        ItemWorldSpawner[] spawners = root.GetComponentsInChildren<ItemWorldSpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            ItemWorldSpawner sp = spawners[i];
            if (sp != null && sp.ItemDefinition == itemDefinition)
                return true;
        }

        return false;
    }

    private static bool ContainsShoppingListKeyUnder(Transform root, string shoppingListKey)
    {
        ItemWorld[] worlds = root.GetComponentsInChildren<ItemWorld>(true);
        for (int i = 0; i < worlds.Length; i++)
        {
            ItemWorld iw = worlds[i];
            if (iw == null)
                continue;
            Item it = iw.GetItem();
            if (it == null || it.definition == null)
                continue;
            if (string.Equals(it.definition.GetShoppingListKey(), shoppingListKey, System.StringComparison.Ordinal))
                return true;
        }

        ItemWorldSpawner[] spawners = root.GetComponentsInChildren<ItemWorldSpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            ItemWorldSpawner sp = spawners[i];
            if (sp == null || sp.ItemDefinition == null)
                continue;
            if (string.Equals(sp.ItemDefinition.GetShoppingListKey(), shoppingListKey, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
