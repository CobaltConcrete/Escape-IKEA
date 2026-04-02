using System.Collections.Generic;
using UnityEngine;
using static EquipmentEnum;

public enum ItemCategory
{
    Normal,
    Loot
}
public enum WorldColliderType
{
    None,
    Box,
    Circle
}

[CreateAssetMenu(menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;
    public bool stackable;
    public float uiScale = 1f;
    public Color glowColor = Color.white;

    [Header("Category")]
    public ItemCategory itemCategory = ItemCategory.Normal;

    [Header("Normal Item Settings")]
    public EquipTag equipTag = EquipTag.None;
    public ItemUseEffect useEffect = ItemUseEffect.None;
    public float effectValue = 0f;
    public float effectDuration = 0f;

    [Header("Loot Settings")]
    public int lootValue = 0;
    public bool canAppearInShoppingList = true;
    public int minRequiredAmount = 1;
    public int maxRequiredAmount = 3;
    public GameObject lootWorldPrefab;

    [Header("Loot Spawn Settings")]
    public List<RoomType> allowedRoomTypes = new List<RoomType>();
    public int bonusSpawnWeight = 1;

    [Header("World Display Settings")]
    public Vector3 worldDropScale = Vector3.one;
    public Vector2 spawnFootprint = new Vector2(1f, 1f);

    [Header("World Collider Settings")]
    public WorldColliderType worldColliderType = WorldColliderType.None;

    // Box
    public Vector2 boxOffset = Vector2.zero;
    public Vector2 boxSize = Vector2.one;
    public float boxEdgeRadius = 0f;

    // Circle
    public Vector2 circleOffset = Vector2.zero;
    public float circleRadius = 0.5f;

    [Header("World Object Settings")]
    public string worldTag = "Untagged";
    public int worldLayer = 0;

    public bool IsLoot()
    {
        return itemCategory == ItemCategory.Loot;
    }

    public bool IsNormalItem()
    {
        return itemCategory == ItemCategory.Normal;
    }
}