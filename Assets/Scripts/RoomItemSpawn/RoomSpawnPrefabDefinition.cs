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
    public RoomType roomType = RoomType.None;
    public RoomSpawnCategory spawnCategory = RoomSpawnCategory.Decoration;

    [Header("Shopping List Metadata")]
    public string shoppingListKey;
    public string pickupDisplayName;
    [Min(0)] public int lootValue = 0;
    public bool canAppearInShoppingList = false;
    [Min(1)] public int minRequiredAmount = 1;
    [Min(1)] public int maxRequiredAmount = 1;
    [Min(1)] public int spawnWeight = 1;

    [Header("Pickup Handoff")]
    public bool isPickable = false;
    public ItemDefinition linkedItemDefinition;

    public string GetResolvedDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(pickupDisplayName))
            return pickupDisplayName;
        return string.IsNullOrWhiteSpace(shoppingListKey) ? gameObject.name : shoppingListKey;
    }
}
