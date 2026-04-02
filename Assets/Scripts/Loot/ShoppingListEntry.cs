using System;

[Serializable]
public class ShoppingListEntry
{
    public ItemDefinition itemDefinition;
    public int requiredAmount;
    public int collectedAmount;

    public bool IsComplete()
    {
        return collectedAmount >= requiredAmount;
    }

    public int GetRequiredValue()
    {
        if (itemDefinition == null)
        {
            return 0;
        }

        return itemDefinition.lootValue * requiredAmount;
    }
}