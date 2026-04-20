using UnityEngine;

public class ItemWorldSpawner : MonoBehaviour
{
    /// <summary>Legacy multiplier retained for compatibility constants only.</summary>
    public const float RoomPickupWorldScale = 4f;

    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int amount = 1;
    [SerializeField] private string spawnedObjectName;

    public ItemDefinition ItemDefinition => itemDefinition;

    private Transform spawnParent;

    public void SetSpawnParent(Transform parent)
    {
        spawnParent = parent;
    }

    private void Start()
    {
        if (itemDefinition == null)
        {
            Debug.LogWarning("ItemWorldSpawner: itemDefinition is null", this);
            return;
        }

        if (spawnParent == null)
        {
            spawnParent = ResolveFallbackSpawnParent();
        }

        Vector3 spawnScale = GetWorldSpawnScale(itemDefinition, transform);

        Item item = new Item
        {
            definition = itemDefinition,
            amount = amount,
            worldScale = spawnScale
        };

        item.InitializeRuntimeDataIfNeeded();

        ItemWorld spawned = ItemWorld.SpawnItemWorld(
            transform.position,
            transform.rotation,
            spawnScale,
            item
        );

        if (spawned != null)
        {
            Room room = GetComponentInParent<Room>();
            if (room != null)
            {
                spawned.SetRoom(room);
            }

            if (!string.IsNullOrWhiteSpace(spawnedObjectName))
                spawned.name = spawnedObjectName;
        }

        if (spawned != null)
        {
            ApplyDefinitionWorldSettings(spawned.gameObject, itemDefinition);
            CopyColliderToSpawned(spawned.gameObject);

            if (spawnParent != null)
            {
                spawned.transform.SetParent(spawnParent, true);
            }

            Room room = GetComponentInParent<Room>();
            room?.RefreshRendererRegistry();
        }

        Destroy(gameObject);
    }
    private void CopyColliderToSpawned(GameObject target)
    {
        if (target == null) return;

        Collider2D sourceCollider = GetComponent<Collider2D>();
        if (sourceCollider == null) return;

        Collider2D existing = target.GetComponent<Collider2D>();
        if (existing != null)
        {
            Destroy(existing);
        }

        if (sourceCollider is BoxCollider2D sourceBox)
        {
            BoxCollider2D box = target.AddComponent<BoxCollider2D>();
            box.isTrigger = itemDefinition == null || !itemDefinition.IsLoot()
                ? sourceBox.isTrigger
                : false;
            box.offset = sourceBox.offset;
            box.size = itemDefinition != null && itemDefinition.IsLoot()
                ? new Vector2(Mathf.Max(0.1f, sourceBox.size.x * 0.42f), Mathf.Max(0.1f, sourceBox.size.y * 0.34f))
                : sourceBox.size;
            box.edgeRadius = sourceBox.edgeRadius;
        }
        else if (sourceCollider is CircleCollider2D sourceCircle)
        {
            CircleCollider2D circle = target.AddComponent<CircleCollider2D>();
            circle.isTrigger = itemDefinition == null || !itemDefinition.IsLoot()
                ? sourceCircle.isTrigger
                : false;
            circle.offset = sourceCircle.offset;
            circle.radius = itemDefinition != null && itemDefinition.IsLoot()
                ? Mathf.Max(0.08f, sourceCircle.radius * 0.38f)
                : sourceCircle.radius;
        }
    }

    private void ApplyDefinitionWorldSettings(GameObject target, ItemDefinition definition)
    {
        ApplyWorldSpawnSettings(target, definition);
    }

    /// <summary>
    /// Unified world pickup scale resolver:
    /// - Hard source of truth is prefab/source transform scale.
    /// - ItemDefinition worldDropScale fallback is intentionally disabled.
    /// </summary>
    public static Vector3 GetWorldSpawnScale(ItemDefinition definition, Transform sourceTransform = null)
    {
        if (sourceTransform != null && sourceTransform.lossyScale.sqrMagnitude > 1e-8f)
            return sourceTransform.lossyScale;

        if (definition != null && definition.worldDropScale.sqrMagnitude > 1e-8f)
            return definition.worldDropScale;

        return Vector3.one;
    }

    /// <summary>Tag/layer setup for spawned <see cref="ItemWorld"/> (also used by room decoration pickups).</summary>
    public static void ApplyWorldSpawnSettings(GameObject target, ItemDefinition definition)
    {
        if (target == null || definition == null)
        {
            return;
        }

        target.tag = string.IsNullOrEmpty(definition.worldTag) ? "Untagged" : definition.worldTag;
        SetLayerRecursively(target, definition.worldLayer);
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    private Transform ResolveFallbackSpawnParent()
    {
        Room room = GetComponentInParent<Room>();
        if (room == null)
            return null;

        RoomSpawnPrefabDefinition def = GetComponent<RoomSpawnPrefabDefinition>();
        if (def == null)
            def = GetComponentInChildren<RoomSpawnPrefabDefinition>(true);

        if (def != null)
        {
            switch (def.spawnCategory)
            {
                case RoomSpawnCategory.Decoration:
                    return room.transform.Find("SpawnedObjects") ?? room.transform;

                case RoomSpawnCategory.Weapon:
                    return room.transform.Find("SpawnedItems") ?? room.transform;

                case RoomSpawnCategory.Item:
                    return room.transform.Find("SpawnedLoots")
                        ?? room.transform.Find("SpawnedLoot")
                        ?? room.transform;

                default:
                    return room.transform.Find("SpawnedLoots")
                        ?? room.transform.Find("SpawnedItems")
                        ?? room.transform.Find("SpawnedObjects")
                        ?? room.transform;
            }
        }

        // If no RoomSpawnPrefabDefinition is found, infer from ItemDefinition.
        if (itemDefinition != null)
        {
            if (itemDefinition.IsLoot())
            {
                return room.transform.Find("SpawnedLoots")
                    ?? room.transform.Find("SpawnedLoot")
                    ?? room.transform;
            }

            if (itemDefinition.itemCategory == ItemCategory.Normal &&
                itemDefinition.equipTag == EquipmentEnum.EquipTag.Weapon)
            {
                return room.transform.Find("SpawnedItems") ?? room.transform;
            }
        }

        return room.transform.Find("SpawnedLoots")
            ?? room.transform.Find("SpawnedItems")
            ?? room.transform.Find("SpawnedObjects")
            ?? room.transform;
    }
}
