using UnityEngine;

public class ItemWorldSpawner : MonoBehaviour
{
    /// <summary>Applied to room pickup prefabs so world items read clearly at room scale.</summary>
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

        Vector3 spawnScale = itemDefinition.worldDropScale.sqrMagnitude > 1e-8f
            ? itemDefinition.worldDropScale
            : transform.lossyScale;
        spawnScale *= RoomPickupWorldScale;

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