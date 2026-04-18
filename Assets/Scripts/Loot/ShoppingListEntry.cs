using System;
using UnityEngine;

[Serializable]
public class ShoppingListEntry
{
    public ItemDefinition itemDefinition;
    public string shoppingListKey;
    public string displayName;
    public int unitValue;
    public int requiredAmount;
    public int collectedAmount;

    public bool IsComplete()
    {
        return collectedAmount >= requiredAmount;
    }

    public int GetRequiredValue()
    {
        int v = itemDefinition != null ? itemDefinition.lootValue : unitValue;
        return Mathf.Max(0, v) * requiredAmount;
    }
    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;
        if (!string.IsNullOrWhiteSpace(shoppingListKey))
            return shoppingListKey;
        if (itemDefinition == null)
            return "";
        return itemDefinition.GetShoppingListKey();
    }

    public string GetShoppingListKey()
    {
        if (!string.IsNullOrWhiteSpace(shoppingListKey))
            return shoppingListKey;
        return itemDefinition != null ? itemDefinition.GetShoppingListKey() : "";
    }
}