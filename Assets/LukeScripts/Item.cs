using System;
using UnityEngine;

[Serializable]
public class Item
{
    public ItemDefinition definition;
    public int amount;

    public Item Clone()
    {
        return new Item
        {
            definition = this.definition,
            amount = this.amount
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
}