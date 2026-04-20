using UnityEngine;

[DisallowMultipleComponent]
public class RoomGeneratedPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private RoomSpawnPrefabDefinition metadata;
    [SerializeField] private ItemWorldSpawner worldSpawner;
    [SerializeField] private string fallbackDisplayName = "Item";
    [SerializeField] private bool destroyOnPickup = true;

    private void Awake()
    {
        if (metadata == null)
            metadata = GetComponent<RoomSpawnPrefabDefinition>();

        if (metadata == null)
            metadata = GetComponentInChildren<RoomSpawnPrefabDefinition>(true);

        if (worldSpawner == null)
            worldSpawner = GetComponent<ItemWorldSpawner>();

        if (worldSpawner == null)
            worldSpawner = GetComponentInChildren<ItemWorldSpawner>(true);

        if (metadata == null)
        {
            Debug.LogWarning($"RoomGeneratedPickup on {name} could not find RoomSpawnPrefabDefinition.", this);
        }

        if (worldSpawner == null)
        {
            Debug.LogWarning($"RoomGeneratedPickup on {name} could not find ItemWorldSpawner.", this);
        }
    }

    public void Interact(PlayerInventoryInteraction player)
    {
        if (player == null)
            return;

        if (!CanPickup())
            return;

        ItemDefinition itemDef = GetItemDefinition();
        if (itemDef == null)
        {
            Debug.LogWarning($"RoomGeneratedPickup on {name} has no ItemDefinition from ItemWorldSpawner.", this);
            return;
        }

        player.PickupLootDefinitionFromWorld(itemDef, 1, gameObject);

        if (destroyOnPickup)
            Destroy(gameObject);
    }

    public string GetInteractionText()
    {
        if (!CanPickup())
            return string.Empty;

        ItemDefinition itemDef = GetItemDefinition();
        string displayName = itemDef != null && !string.IsNullOrWhiteSpace(itemDef.itemName)
            ? itemDef.itemName
            : fallbackDisplayName;

        return $"[F] Pick up {displayName}";
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }

    public void SetRoomVisible(bool visible)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = visible;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in colliders)
        {
            if (c != null)
                c.enabled = visible;
        }
    }

    private ItemDefinition GetItemDefinition()
    {
        if (worldSpawner == null)
            return null;

        return worldSpawner.ItemDefinition;
    }

    private bool CanPickup()
    {
        if (metadata == null)
            return false;

        if (!metadata.isPickable)
            return false;

        if (metadata.spawnCategory != RoomSpawnCategory.Item)
            return false;

        ItemDefinition itemDef = GetItemDefinition();
        if (itemDef == null)
            return false;

        return itemDef.IsLoot();
    }
}