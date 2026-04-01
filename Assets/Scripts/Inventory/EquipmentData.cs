using UnityEngine;
using static EquipmentEnum;

public class EquipmentData : MonoBehaviour
{
    private Item equippedWeapon;
    private Item equippedArmor;
    private Item equippedUtility;

    public Item GetEquippedItem(EquipTag equipTag)
    {
        switch (equipTag)
        {
            case EquipTag.Weapon:
                return equippedWeapon;
            case EquipTag.Armor:
                return equippedArmor;
            case EquipTag.Utility:
                return equippedUtility;
            default:
                return null;
        }
    }

    public Item EquipItem(Item item)
    {
        if (item == null) return null;

        EquipTag equipTag = ItemEquipClassifier.GetEquipTag(item);
        Item oldItem = null;

        switch (equipTag)
        {
            case EquipTag.Weapon:
                oldItem = equippedWeapon;
                equippedWeapon = item;
                break;

            case EquipTag.Armor:
                oldItem = equippedArmor;
                equippedArmor = item;
                break;

            case EquipTag.Utility:
                oldItem = equippedUtility;
                equippedUtility = item;
                break;
        }

        return oldItem;
    }

    public Item UnequipItem(EquipTag equipTag)
    {
        Item removedItem = null;

        switch (equipTag)
        {
            case EquipTag.Weapon:
                removedItem = equippedWeapon;
                equippedWeapon = null;
                break;

            case EquipTag.Armor:
                removedItem = equippedArmor;
                equippedArmor = null;
                break;

            case EquipTag.Utility:
                removedItem = equippedUtility;
                equippedUtility = null;
                break;
        }

        return removedItem;
    }

    public void ClearSlot(EquipTag equipTag)
    {
        switch (equipTag)
        {
            case EquipTag.Weapon:
                equippedWeapon = null;
                break;
            case EquipTag.Armor:
                equippedArmor = null;
                break;
            case EquipTag.Utility:
                equippedUtility = null;
                break;
        }
    }
}