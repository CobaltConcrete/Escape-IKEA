using UnityEngine;
using static EquipmentEnum;

public enum ItemCategory
{
    Normal,
    Loot
}

[CreateAssetMenu(menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public bool stackable;
    public float uiScale = 1f;
    public Color glowColor = Color.white;
    public EquipTag equipTag = EquipTag.None;

    [Header("Category")]
    public ItemCategory itemCategory = ItemCategory.Normal;

    [Header("Use Effect")]
    public ItemUseEffect useEffect = ItemUseEffect.None;
    public float effectValue = 0f;
    public float effectDuration = 0f;
}