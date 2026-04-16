using UnityEngine;

/// <summary>
/// Two-step bed interaction:
/// 1) reveal hidden plush
/// 2) pick plush if it is on shopping list.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BedroomBedContainerPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition plushDefinition;
    [SerializeField] private SpriteRenderer plushRenderer;
    [SerializeField] private Collider2D plushCollider;
    [SerializeField] private bool plushRevealed;

    public void Configure(ItemDefinition plushDef, SpriteRenderer plushSr, Collider2D plushCol)
    {
        plushDefinition = plushDef;
        plushRenderer = plushSr;
        plushCollider = plushCol;
        plushRevealed = false;

        if (plushRenderer != null)
            plushRenderer.enabled = false;
        if (plushCollider != null)
            plushCollider.enabled = false;
    }

    private bool CanCollectPlush()
    {
        if (plushDefinition == null || !plushDefinition.IsLoot())
            return false;

        RunObjectiveManager rom = RunObjectiveManager.Instance;
        if (rom == null)
            return true;

        return rom.ContainsShoppingListKey(plushDefinition.GetShoppingListKey());
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
        plushRenderer = null;
        plushCollider = null;
    }

    public string GetInteractionText()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && !sr.enabled)
            return "";

        if (!plushRevealed)
            return "[F] Search bed";

        if (!CanCollectPlush())
            return "";
        if (plushRenderer == null)
            return "";

        string name = plushDefinition != null ? plushDefinition.itemName : "plush";
        return "[F] Pick up " + name;
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }
}

