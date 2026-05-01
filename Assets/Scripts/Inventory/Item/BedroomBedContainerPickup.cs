using UnityEngine;

/// <summary>
/// Two-step bed interaction: first F takes the plush off the bed (reveals it for pickup), second F collects if on shopping list.
/// After collection the bed sprite switches to the no-plush variant.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BedroomBedContainerPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition plushDefinition;
    [SerializeField] private SpriteRenderer plushRenderer;
    [SerializeField] private Collider2D plushCollider;
    [SerializeField] private SpriteRenderer bedRenderer;
    [SerializeField] private Sprite bedSpriteNoPlush;
    [SerializeField] private bool plushRevealed;

    public void Configure(
        ItemDefinition plushDef,
        SpriteRenderer plushSr,
        Collider2D plushCol,
        SpriteRenderer bedSr,
        Sprite noPlushBedSprite)
    {
        plushDefinition = plushDef;
        plushRenderer = plushSr;
        plushCollider = plushCol;
        bedRenderer = bedSr;
        bedSpriteNoPlush = noPlushBedSprite;
        plushRevealed = false;

        if (plushRenderer != null)
            plushRenderer.enabled = false;
        if (plushCollider != null)
            plushCollider.enabled = false;
    }

    /// <summary>Used by loot seeding: how many of this plush can exist on this bed.</summary>
    public string GetPlushShoppingListKey()
    {
        return plushDefinition != null ? plushDefinition.GetShoppingListKey() : null;
    }

    private bool CanCollectPlush()
    {
        if (plushDefinition == null || !plushDefinition.IsLoot())
            return false;

        RunObjectiveManager rom = RunObjectiveManager.Instance;
        if (rom == null)
            return true;

        return rom.NeedsMoreOfShoppingListKey(plushDefinition.GetShoppingListKey());
    }

    public void Interact(PlayerInventoryInteraction player)
    {
        if (player == null)
            return;

        if (!plushRevealed)
        {
            plushRevealed = true;
            if (plushRenderer != null)
                plushRenderer.enabled = true;
            if (plushCollider != null)
                plushCollider.enabled = true;
            return;
        }

        if (!CanCollectPlush())
            return;

        if (plushRenderer == null)
            return;

        player.PickupLootDefinitionFromWorld(plushDefinition, 1, plushRenderer.gameObject);

        if (bedRenderer != null && bedSpriteNoPlush != null)
            bedRenderer.sprite = bedSpriteNoPlush;

        plushRenderer = null;
        plushCollider = null;
    }

    public string GetInteractionText()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && !sr.enabled)
            return "";

        if (!plushRevealed)
            return "Hold [F] Take plush from bed";

        if (!CanCollectPlush())
            return "";
        if (plushRenderer == null)
            return "";

        string name = plushDefinition != null ? plushDefinition.itemName : "plush";
        return "Hold [F] Pick up " + name;
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }
}
