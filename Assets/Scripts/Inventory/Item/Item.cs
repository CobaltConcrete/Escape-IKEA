using System;
using UnityEngine;

[Serializable]
public class Item
{
    public ItemDefinition definition;
    public int amount;
    public Vector3 worldScale = Vector3.one;

    [Header("Armor Runtime Data")]
    public float currentArmorDurability = -1f;

    public Item Clone()
    {
        return new Item
        {
            definition = this.definition,
            amount = this.amount,
            worldScale = this.worldScale,
            currentArmorDurability = this.currentArmorDurability
        };
    }

    public void InitializeRuntimeDataIfNeeded()
    {
        if (definition == null)
            return;

        if (IsArmor() && currentArmorDurability < 0f)
        {
            currentArmorDurability = definition.armorMaxDurability;
        }
    }

    public Sprite GetSprite()
    {
        return definition != null ? definition.icon : null;
    }

    public float GetUIScale()
    {
        return definition != null ? definition.uiScale : 1f;
    }

    public Color GetColor()
    {
        return definition != null ? definition.glowColor : Color.white;
    }

    public bool IsStackable()
    {
        return definition != null && definition.stackable;
    }

    public bool IsLoot()
    {
        return definition != null && definition.itemCategory == ItemCategory.Loot;
    }

    public bool IsUsable()
    {
        return definition != null
               && definition.itemCategory == ItemCategory.Normal
               && definition.useEffect != ItemUseEffect.None;
    }

    public bool IsArmor()
    {
        return definition != null
               && definition.itemCategory == ItemCategory.Normal
               && definition.equipTag == EquipmentEnum.EquipTag.Armor;
    }

    public float GetArmorCurrentDurability()
    {
        InitializeRuntimeDataIfNeeded();
        return currentArmorDurability;
    }

    public float GetArmorMaxDurability()
    {
        if (definition == null) return 0f;
        return definition.armorMaxDurability;
    }

    public float GetArmorDamageReduction()
    {
        if (!IsArmor()) return 0f;
        return Mathf.Max(0f, definition.armorDamageReduction);
    }

    public ArmorSpecialAbility GetArmorSpecialAbility()
    {
        if (!IsArmor()) return ArmorSpecialAbility.None;
        return definition.armorSpecialAbility;
    }

    public bool DamageArmor(float durabilityLoss)
    {
        if (!IsArmor())
            return false;

        InitializeRuntimeDataIfNeeded();

        currentArmorDurability -= Mathf.Max(0f, durabilityLoss);
        return currentArmorDurability <= 0f;
    }
}