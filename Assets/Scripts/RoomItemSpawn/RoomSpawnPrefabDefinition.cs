using UnityEngine;

public enum RoomSpawnCategory
{
    Decoration = 0,
    Item = 1,
    Weapon = 2
}

[DisallowMultipleComponent]
public class RoomSpawnPrefabDefinition : MonoBehaviour
{
    [Header("Classification")]
    public RoomSpawnCategory spawnCategory = RoomSpawnCategory.Decoration;

    [Header("Pickup Handoff")]
    public bool isPickable = false;

    public bool IsWeapon()
    {
        return spawnCategory == RoomSpawnCategory.Weapon;
    }

    public bool IsItemSpawn()
    {
        return spawnCategory == RoomSpawnCategory.Item;
    }

    public ItemDefinition GetItemDefinition()
    {
        ItemWorldSpawner spawner = GetComponent<ItemWorldSpawner>();
        if (spawner == null)
            spawner = GetComponentInChildren<ItemWorldSpawner>(true);

        return spawner != null ? spawner.ItemDefinition : null;
    }

    public bool IsLootItem()
    {
        ItemDefinition def = GetItemDefinition();
        return def != null && def.IsLoot();
    }

    public string GetShoppingListKey()
    {
        ItemDefinition def = GetItemDefinition();
        return def != null ? def.GetShoppingListKey() : "";
    }

    public string GetResolvedDisplayName()
    {
        ItemDefinition def = GetItemDefinition();
        if (def != null && !string.IsNullOrWhiteSpace(def.itemName))
            return def.itemName;

        return gameObject.name;
    }

    public bool CanAppearInShoppingList()
    {
        ItemDefinition def = GetItemDefinition();
        return def != null && def.canAppearInShoppingList;
    }

    public bool AllowsRoomType(RoomType queriedRoomType)
    {
        ItemDefinition def = GetItemDefinition();
        if (def == null)
            return true;

        if (def.allowedRoomTypes == null || def.allowedRoomTypes.Count == 0)
            return true;

        return def.allowedRoomTypes.Contains(queriedRoomType);
    }

    private void OnValidate()
    {
        // 自动同步：Item 才可拾取；其他类型默认不可拾取
        isPickable = (spawnCategory == RoomSpawnCategory.Item);

        ItemDefinition def = GetItemDefinition();

        if (spawnCategory == RoomSpawnCategory.Item &&
            def != null &&
            !def.IsLoot())
        {
            Debug.LogWarning(
                $"{name}: spawnCategory is Item, but ItemWorldSpawner.ItemDefinition '{def.itemName}' is not Loot.",
                this);
        }

        if (spawnCategory == RoomSpawnCategory.Weapon &&
            def != null &&
            def.itemCategory != ItemCategory.Normal)
        {
            Debug.LogWarning(
                $"{name}: spawnCategory is Weapon, but ItemWorldSpawner.ItemDefinition '{def.itemName}' is not a Normal item.",
                this);
        }

        if (spawnCategory == RoomSpawnCategory.Item && def == null)
        {
            Debug.LogWarning(
                $"{name}: spawnCategory is Item, but no ItemWorldSpawner with ItemDefinition was found.",
                this);
        }
    }
}