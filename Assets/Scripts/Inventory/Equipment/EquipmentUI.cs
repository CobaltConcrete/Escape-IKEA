using UnityEngine;
using static EquipmentEnum;

public class EquipmentUI : MonoBehaviour
{
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private EquipmentSlotUI weaponSlotUI;
    [SerializeField] private EquipmentSlotUI armorSlotUI;
    [SerializeField] private EquipmentSlotUI utilitySlotUI;

    private void Start()
    {
        RefreshAllSlots();
    }

    public void ClearAllSlots()
    {
        weaponSlotUI.ClearSlot();
        armorSlotUI.ClearSlot();
        utilitySlotUI.ClearSlot();
    }

    public Item EquipItem(Item item)
    {
        if (item == null) return null;

        Item oldItem = equipmentData.EquipItem(item);
        RefreshAllSlots();
        return oldItem;
    }

    public Item UnequipItem(EquipTag equipTag)
    {
        Item removedItem = equipmentData.UnequipItem(equipTag);
        RefreshAllSlots();
        return removedItem;
    }

    public void ClearSlot(EquipTag equipTag)
    {
        equipmentData.ClearSlot(equipTag);
        RefreshAllSlots();
    }

    public void RefreshSlot(EquipTag equipTag)
    {
        RefreshAllSlots();
    }

    public void RefreshAllSlots()
    {
        Item equippedWeapon = equipmentData.GetEquippedItem(EquipTag.Weapon);
        Item equippedArmor = equipmentData.GetEquippedItem(EquipTag.Armor);
        Item equippedUtility = equipmentData.GetEquippedItem(EquipTag.Utility);

        if (equippedWeapon != null)
            weaponSlotUI.SetItem(equippedWeapon);
        else
            weaponSlotUI.ClearSlot();

        if (equippedArmor != null)
            armorSlotUI.SetItem(equippedArmor);
        else
            armorSlotUI.ClearSlot();

        if (equippedUtility != null)
            utilitySlotUI.SetItem(equippedUtility);
        else
            utilitySlotUI.ClearSlot();
    }

    public Item GetEquippedItem(EquipTag equipTag)
    {
        return equipmentData.GetEquippedItem(equipTag);
    }
}