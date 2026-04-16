using UnityEngine;

/// <summary>
/// Pickup used by living-room container children (e.g. picture on cabinet, cushion on couch).
/// Uses shopping-list gating via loot definition key, then grants loot directly.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LivingRoomContainerPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition lootDefinition;
    [SerializeField] private int amount = 1;
    [SerializeField] private string displayNameOverride = "";
    [SerializeField] private SpriteRenderer couchRendererToSwap;
    [SerializeField] private Sprite couchAfterSprite;

    public void Configure(ItemDefinition definition, int pickupAmount, string displayName)
    {
        lootDefinition = definition;
        amount = Mathf.Max(1, pickupAmount);
        displayNameOverride = displayName ?? "";
    }

    public void ConfigureCouchSwap(SpriteRenderer couchRenderer, Sprite afterSprite)
    {
        couchRendererToSwap = couchRenderer;
        couchAfterSprite = afterSprite;
    }

    private bool CanPickup()
    {
        if (lootDefinition == null || !lootDefinition.IsLoot())
            return false;

        RunObjectiveManager rom = RunObjectiveManager.Instance;
        if (rom == null)
            return true;

        return rom.ContainsShoppingListKey(lootDefinition.GetShoppingListKey());
    }

    public void Interact(PlayerInventoryInteraction player)
    {
        if (player == null || !CanPickup())
            return;

        player.PickupLootDefinitionFromWorld(lootDefinition, amount, gameObject);

        if (couchRendererToSwap != null && couchAfterSprite != null)
            couchRendererToSwap.sprite = couchAfterSprite;
    }

    public string GetInteractionText()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && !sr.enabled)
            return "";

        if (!CanPickup())
            return "";

        string displayName = string.IsNullOrWhiteSpace(displayNameOverride)
            ? (lootDefinition != null ? lootDefinition.itemName : "")
            : displayNameOverride;

        return string.IsNullOrWhiteSpace(displayName) ? "" : "[F] Pick up " + displayName;
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }
}

