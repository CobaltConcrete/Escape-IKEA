using UnityEngine;

/// <summary>
/// World weapon pickup (e.g. sports bat) that is not shopping-list loot and does not use <see cref="ItemWorld"/>.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WeaponWorldPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition weaponDefinition;
    [SerializeField] private int amount = 1;
    [SerializeField] private bool roomVisible = true;

    public ItemDefinition WeaponDefinition => weaponDefinition;

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        c.isTrigger = false;
    }

    private void Awake()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null && c.isTrigger)
            c.isTrigger = false;
    }

    public void Interact(PlayerInventoryInteraction player)
    {
        if (player == null || weaponDefinition == null)
            return;

        player.PickupWeaponFromWorld(weaponDefinition, amount, gameObject);
    }

    public string GetInteractionText()
    {
        if (weaponDefinition == null)
            return "";

        if (string.Equals(weaponDefinition.itemName, BatWeapon.ItemName, System.StringComparison.OrdinalIgnoreCase))
            return "[F] Pick up the bat to defend yourself";

        return "[F] Pick up " + weaponDefinition.itemName;
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
}
