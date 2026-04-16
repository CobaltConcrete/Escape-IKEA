using UnityEngine;

public class ItemWorldSpawner : MonoBehaviour
{
    /// <summary>Legacy multiplier retained for compatibility constants only.</summary>
    public const float RoomPickupWorldScale = 4f;

    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int amount = 1;

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

        // Prefab transform scale is the authoritative world size for this spawner path.
        // This allows designers to tune pickup size directly in prefab Transform.
        Vector3 spawnScale = GetWorldSpawnScale(itemDefinition, transform);

        Item item = new Item
        {
            definition = itemDefinition,
            amount = amount,
            worldScale = spawnScale
        };

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
        }

        if (spawned != null)
        {
            ApplyDefinitionWorldSettings(spawned.gameObject, itemDefinition);

            if (spawnParent != null)
            {
                spawned.transform.SetParent(spawnParent, true);
            }

            Room room = GetComponentInParent<Room>();
            room?.RefreshRendererRegistry();
        }

        Destroy(gameObject);
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
}