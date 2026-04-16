using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Snaps catalog placement to the largest interior trigger bounds so props hug walls/corners across room prefabs.
/// </summary>
public enum RoomDecorInteriorAnchor
{
    None = 0,
    InteriorTopLeft = 1,
    InteriorTopRight = 2,
    InteriorMiddleRight = 3,
    InteriorBottomLeft = 4,
    InteriorBottomRight = 5,
    InteriorTopCenter = 6,
    InteriorMiddleLeft = 7,
    InteriorMiddleCenter = 8,
    InteriorBottomCenter = 9
}

[CreateAssetMenu(menuName = "Rooms/Room Decoration Catalog", fileName = "RoomDecorationCatalog")]
public class RoomDecorationCatalog : ScriptableObject
{
    [Serializable]
    public class DecorationEntry
    {
        [Tooltip("Only rooms of this type receive this row (must match the room's LootSpawnArea room type).")]
        public RoomType roomType;

        [Tooltip("Static art when this row is decor only, or when Catalog Pickup is set to Pickup only when on shopping list and this run does not list that item.")]
        public Sprite sprite;

        [Tooltip("When Interior Anchor is None: local position in room space. Otherwise: offset added after snapping to that anchor (meters).")]
        public Vector3 localPosition;
        public Vector3 localScale = Vector3.one;
        public string sortingLayerName = "Default";
        public int sortingOrder;

        [Tooltip("Uses the room's largest trigger collider AABB in local space; localPosition is an extra offset from that anchor.")]
        public RoomDecorInteriorAnchor interiorAnchor = RoomDecorInteriorAnchor.None;

        [Tooltip("Inset from walls when using an interior anchor (0 uses placer default).")]
        [Min(0f)] public float interiorAnchorInset;

        [Tooltip("When set, spawns this world item/loot here (F to pick up) instead of a plain decoration. Skipped if the room already has this definition under SpawnedItems / loot spawn parents.")]
        public ItemDefinition catalogPickup;

        [Tooltip("If true: spawn catalogPickup only when that item's shopping-list key is on the current run; otherwise use Sprite as non-interactive decor. If false: legacy behavior (always spawn pickup when catalogPickup is set).")]
        public bool pickupOnlyWhenOnShoppingList = true;

        [Tooltip("When > 0: random offset in room local XY after anchor resolve (e.g. floor clutter).")]
        [Min(0f)] public float randomLocalOffsetRadius;
    }

    public List<DecorationEntry> entries = new List<DecorationEntry>();

    /// <summary>Shopping-list keys that have a list-gated catalog pickup row (used to coordinate LootSpawnManager).</summary>
    public void CollectListGatedShoppingListKeys(HashSet<string> keys)
    {
        if (entries == null || keys == null)
            return;

        foreach (DecorationEntry e in entries)
        {
            if (e?.catalogPickup == null || !e.pickupOnlyWhenOnShoppingList)
                continue;

            keys.Add(e.catalogPickup.GetShoppingListKey());
        }
    }

    public bool HasListGatedCatalogPickupForKey(string shoppingListKey)
    {
        if (string.IsNullOrEmpty(shoppingListKey) || entries == null)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            DecorationEntry e = entries[i];
            if (e?.catalogPickup == null || !e.pickupOnlyWhenOnShoppingList)
                continue;

            if (string.Equals(e.catalogPickup.GetShoppingListKey(), shoppingListKey, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>List-gated catalog pickup row for this shopping-list key in a specific room type (coordinates LootSpawnManager with RoomDecorationPlacer).</summary>
    public bool HasListGatedCatalogPickupForRoom(string shoppingListKey, RoomType roomType)
    {
        if (string.IsNullOrEmpty(shoppingListKey) || entries == null)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            DecorationEntry e = entries[i];
            if (e?.catalogPickup == null || !e.pickupOnlyWhenOnShoppingList)
                continue;
            if (e.roomType != roomType)
                continue;

            if (string.Equals(e.catalogPickup.GetShoppingListKey(), shoppingListKey, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
