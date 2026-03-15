using System;
using UnityEngine;

[Serializable]
public class Item
{
    public enum ItemType
    {
        Sword,
        HealthPill,
        AnotherPill,
        Coin,
        Medkit,
    }

    public ItemType itemType;
    public int amount;

    public Sprite GetSprite()
    {
        switch (itemType)
        {
            default:
            case ItemType.Sword: 
                return ItemAssets.Instance.swordSprite;
            case ItemType.HealthPill: 
                return ItemAssets.Instance.healthPillSprite;
            case ItemType.AnotherPill: 
                return ItemAssets.Instance.anotherPillSprite;
            case ItemType.Coin: 
                return ItemAssets.Instance.coinSprite;
            case ItemType.Medkit: 
                return ItemAssets.Instance.medkitSprite;
        }
    }

    public float GetUIScale()
    {
        switch (itemType)
        {
            default:
            case ItemType.Sword:
                return 1f;
            case ItemType.HealthPill:
                return 0.75f;
            case ItemType.AnotherPill:
                return 0.75f;
            case ItemType.Coin:
                return 0.65f;
            case ItemType.Medkit:
                return 0.8f;
        }
    }

    public Color GetColor()
    {
        switch (itemType)
        {
            default:
            case ItemType.Sword:
                return new Color(1, 1, 1);
            case ItemType.HealthPill:
                return new Color(1, 0, 0);
            case ItemType.AnotherPill:
                return new Color(0, 0, 1);
            case ItemType.Coin:
                return new Color(1, 1, 0);
            case ItemType.Medkit:
                return new Color(1, 0, 0.75f);
        }
    }

    public bool IsStackable()
    {
        switch (itemType)
        {
            default:
            case ItemType.Coin:
            case ItemType.HealthPill:
            case ItemType.AnotherPill:
                return true;
            case ItemType.Sword:
            case ItemType.Medkit:
                return false;
        }
    }

}
