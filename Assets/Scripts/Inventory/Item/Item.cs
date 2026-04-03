using System;
using UnityEngine;

[Serializable]
public class Item
{
    public ItemDefinition definition;
    public int amount;
    public Vector3 worldScale = Vector3.one;

    public Item Clone()
    {
        return new Item
        {
            definition = this.definition,
            amount = this.amount,
            worldScale = this.worldScale
        };
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
}