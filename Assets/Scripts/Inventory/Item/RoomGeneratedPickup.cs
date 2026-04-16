using UnityEngine;

[DisallowMultipleComponent]
public class RoomGeneratedPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private RoomSpawnPrefabDefinition metadata;
    [SerializeField] private string fallbackDisplayName = "Item";
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] [Range(0.1f, 1f)] private float colliderRadiusScale = 0.55f;

    private void Awake()
    {
        if (metadata == null)
            metadata = GetComponent<RoomSpawnPrefabDefinition>();

        if (metadata == null)
            metadata = GetComponentInChildren<RoomSpawnPrefabDefinition>(true);

        if (metadata == null)
        {
            Debug.LogWarning($"RoomGeneratedPickup on {name} could not find RoomSpawnPrefabDefinition.", this);
            return;
        }

        EnsureSolidLootColliders();
    }

    public void Interact(PlayerInventoryInteraction player)
    {
        if (player == null)
            return;

        if (!CanPickup())
            return;

        ItemDefinition matchedDefinition = FindMatchingLootDefinition();
        if (matchedDefinition == null)
        {
            Debug.LogWarning($"RoomGeneratedPickup on {name} could not find matching loot ItemDefinition.", this);
            return;
        }

        player.PickupLootDefinitionFromWorld(matchedDefinition, 1, gameObject);
    }

    public string GetInteractionText()
    {
        if (!CanPickup())
            return string.Empty;
        string name = metadata != null ? metadata.GetResolvedDisplayName() : fallbackDisplayName;
        return $"[F] Pick up {name}";
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }

    public void SetRoomVisible(bool visible)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = visible;
        }
    }

    private bool CanPickup()
    {
        if (metadata == null)
            return false;

        return !string.IsNullOrWhiteSpace(metadata.shoppingListKey);
    }
    private ItemDefinition FindMatchingLootDefinition()
    {
        if (metadata == null)
            return null;

        string targetKey = NormalizeKey(metadata.shoppingListKey);
        if (string.IsNullOrEmpty(targetKey))
            return null;

        RunObjectiveManager rom = RunObjectiveManager.Instance;
        if (rom == null)
            return null;

        var allDefs = rom.GetAllItemDefinitions();
        if (allDefs == null)
            return null;

        for (int i = 0; i < allDefs.Count; i++)
        {
            ItemDefinition def = allDefs[i];
            if (def == null)
                continue;
            if (!def.IsLoot())
                continue;

            if (NormalizeKey(def.GetShoppingListKey()) == targetKey)
                return def;
        }

        return null;
    }

    private string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
    }

    //private void EnsureSolidLootColliders()
    //{
    //    if (metadata == null || string.IsNullOrWhiteSpace(metadata.shoppingListKey))
    //        return;

    //    bool useTriggerCollider = ShouldUseTriggerCollider();

    //    // Prefab auth can be messy during migration; remove any 3D colliders and
    //    // ensure there is always one reliable 2D collider for interaction + blocking.
    //    Collider[] legacy3d = GetComponentsInChildren<Collider>(true);
    //    for (int i = 0; i < legacy3d.Length; i++)
    //    {
    //        if (legacy3d[i] != null)
    //            Destroy(legacy3d[i]);
    //    }

    //    BoxCollider2D rootBox = GetComponent<BoxCollider2D>();
    //    if (rootBox == null)
    //    {
    //        rootBox = gameObject.AddComponent<BoxCollider2D>();
    //    }

    //    SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);

    //    if (sr != null && sr.sprite != null)
    //    {
    //        Vector2 spriteSize = sr.sprite.bounds.size;

    //        if (spriteSize.x > 0.01f && spriteSize.y > 0.01f)
    //        {
    //            rootBox.size = spriteSize * colliderRadiusScale;
    //            rootBox.offset = (Vector2)sr.transform.localPosition;
    //        }
    //        else
    //        {
    //            rootBox.size = new Vector2(0.9f, 0.9f) * colliderRadiusScale;
    //            rootBox.offset = Vector2.zero;
    //        }
    //    }
    //    else
    //    {
    //        rootBox.size = new Vector2(0.9f, 0.9f) * colliderRadiusScale;
    //        rootBox.offset = Vector2.zero;
    //    }

    //    rootBox.isTrigger = useTriggerCollider;

    //    Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
    //    for (int i = 0; i < colliders.Length; i++)
    //    {
    //        Collider2D c = colliders[i];
    //        if (c == null)
    //            continue;

    //        c.isTrigger = useTriggerCollider;
    //    }
    //}
    private void EnsureSolidLootColliders()
    {
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.shoppingListKey))
            return;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);

        if (colliders == null || colliders.Length == 0)
        {
            Debug.LogWarning($"[Pickup] {name} has no 2D collider.", this);
        }

        // 不改 isTrigger
        // 不改 size
        // 不改 offset
        // 不删 collider
    }

    private bool ShouldUseTriggerCollider()
    {
        if (metadata == null)
            return false;

        return string.Equals(metadata.shoppingListKey, "Table", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(metadata.pickupDisplayName, "Table", System.StringComparison.OrdinalIgnoreCase);
    }
}
