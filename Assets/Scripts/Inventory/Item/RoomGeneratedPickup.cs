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
        EnsureSolidLootColliders();
    }

    public void Interact(PlayerInventoryInteraction player)
    {
        if (!CanPickup())
            return;

        if (RunObjectiveManager.Instance != null)
        {
            RunObjectiveManager.Instance.RegisterCollectedByKey(
                metadata.shoppingListKey,
                1,
                metadata.lootValue);
        }

        if (destroyOnPickup)
            Destroy(gameObject);
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
        // Some prefabs may still be mis-tagged while data migration is in progress.
        // If it has a shopping-list key and that key is needed, allow pickup prompt/collection.
        if (metadata.spawnCategory != RoomSpawnCategory.Item && string.IsNullOrWhiteSpace(metadata.shoppingListKey))
            return false;
        if (string.IsNullOrWhiteSpace(metadata.shoppingListKey))
            return false;
        return RunObjectiveManager.Instance != null &&
               RunObjectiveManager.Instance.NeedsMoreOfShoppingListKey(metadata.shoppingListKey);
    }

    private void EnsureSolidLootColliders()
    {
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.shoppingListKey))
            return;

        // Prefab auth can be messy during migration; remove any 3D colliders and
        // ensure there is always one reliable 2D collider for interaction + blocking.
        Collider[] legacy3d = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < legacy3d.Length; i++)
        {
            if (legacy3d[i] != null)
                Destroy(legacy3d[i]);
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        BoxCollider2D rootBox = GetComponent<BoxCollider2D>();
        if (rootBox == null)
        {
            rootBox = gameObject.AddComponent<BoxCollider2D>();
        }

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null && sr.sprite != null)
        {
            Vector2 size = sr.sprite.bounds.size;
            if (size.x > 0.01f && size.y > 0.01f)
            {
                rootBox.size = size * colliderRadiusScale;
                rootBox.offset = sr.transform.localPosition;
            }
            else
            {
                rootBox.size = new Vector2(0.9f, 0.9f) * colliderRadiusScale;
            }
        }
        else
        {
            rootBox.size = new Vector2(0.9f, 0.9f) * colliderRadiusScale;
        }
        rootBox.isTrigger = false;

        colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D c = colliders[i];
            if (c == null)
                continue;

            c.isTrigger = false;
        }
    }
}
