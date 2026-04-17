using static EquipmentEnum;

public static class ItemEquipClassifier
{
    public static EquipTag GetEquipTag(Item item)
    {
        if (item == null || item.definition == null)
            return EquipTag.None;

        return item.definition.equipTag;
    }

    public static bool IsEquipable(Item item)
    {
        return GetEquipTag(item) != EquipTag.None;
    }
}