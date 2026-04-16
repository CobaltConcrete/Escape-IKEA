using UnityEngine;

/// <summary>
/// World weapon pickup (e.g. sports bat) that is not shopping-list loot and does not use <see cref="ItemWorld"/>.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WeaponWorldPickup : MonoBehaviour, IInteractable
{
    private const float GenericWeaponColliderScale = 0.55f;
    private const float BatColliderScale = 0.8f;

    [SerializeField] private ItemDefinition weaponDefinition;
    [SerializeField] private int amount = 1;
    [SerializeField] private bool roomVisible = true;

    public ItemDefinition WeaponDefinition => weaponDefinition;

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null)
            c.isTrigger = true;
    }

    private void Awake()
    {
        EnsureWeaponCollider2D();

        ShrinkBlockingColliderFootprint();
    }

    public void Interact(PlayerInventoryInteraction player)
    {
        if (player == null || weaponDefinition == null)
            return;
        if (!IsBatPickup())
            return;

        player.PickupWeaponFromWorld(weaponDefinition, amount, gameObject);
    }

    public string GetInteractionText()
    {
        if (weaponDefinition == null)
            return "";

        if (IsBatPickup())
            return "[F] Pick up the bat to defend yourself";

        return "";
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }

    public void SetRoomVisible(bool visible)
    {
        roomVisible = visible;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
        }

        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null)
                cols[i].enabled = visible;
        }
    }

    private void ShrinkBlockingColliderFootprint()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null)
            return;

        bool isBat = weaponDefinition != null &&
            string.Equals(weaponDefinition.itemName, BatWeapon.ItemName, System.StringComparison.OrdinalIgnoreCase);
        float scale = isBat ? BatColliderScale : GenericWeaponColliderScale;

        if (c is BoxCollider2D box)
        {
            box.size *= scale;
            return;
        }

        if (c is CircleCollider2D circle)
        {
            circle.radius *= scale;
            return;
        }

        CapsuleCollider2D capsule = c as CapsuleCollider2D;
        if (capsule != null)
            capsule.size *= scale;
    }

    private void EnsureWeaponCollider2D()
    {
        Collider[] legacy3d = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < legacy3d.Length; i++)
        {
            if (legacy3d[i] != null)
                Destroy(legacy3d[i]);
        }

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider2D>();

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null && sr.sprite != null)
        {
            Vector2 size = sr.sprite.bounds.size;
            box.size = size.x > 0.01f && size.y > 0.01f ? size : new Vector2(1f, 0.4f);
            box.offset = sr.transform.localPosition;
        }
        else
        {
            box.size = new Vector2(1f, 0.4f);
        }

        // Trigger so scaled bat never walls off the room; interaction uses overlap checks.
        box.isTrigger = true;
    }

    private bool IsBatPickup()
    {
        if (weaponDefinition != null)
        {
            string n = weaponDefinition.itemName;
            if (!string.IsNullOrWhiteSpace(n))
            {
                if (string.Equals(n, BatWeapon.ItemName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
                if (n.IndexOf("bat", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        RoomSpawnPrefabDefinition roomDef = GetComponent<RoomSpawnPrefabDefinition>();
        if (roomDef != null && roomDef.spawnCategory == RoomSpawnCategory.Weapon)
            return true;

        return gameObject.name.IndexOf("bat", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
