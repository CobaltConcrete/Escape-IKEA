using System.Collections;
using UnityEngine;

public class PlayerInventoryInteraction : MonoBehaviour
{
    private Inventory inventory;
    [SerializeField] private UI_Inventory uiInventory;
    private SpriteRenderer playerSpriteRenderer;
    private PlayerMovement playerMovement;

    private Coroutine flashCoroutine;
    private Color baseColor;

    [SerializeField] private Dialogue playerDialogue;
    private bool firstItemFound;
    private void Awake()
    {
        inventory = new Inventory(UseItem);
        playerMovement = GetComponent<PlayerMovement>();

        playerSpriteRenderer = transform.Find("PlayerSprite").GetComponent<SpriteRenderer>();
        baseColor = playerSpriteRenderer.color;
        firstItemFound = false;
    }
    private void Start()
    {
        uiInventory.SetPlayer(this);
        uiInventory.SetInventory(inventory);
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    internal void UseItem(Item item)
    {
        switch (item.itemType)
        {
            case Item.ItemType.HealthPill:
                FlashGreen();
                playerMovement.BoostSpeedFor10Seconds();
                inventory.RemoveOneItem(item);
                break;

            case Item.ItemType.SpeedPill:
                FlashBlue();
                inventory.RemoveOneItem(item);
                break;

            case Item.ItemType.Medkit:
                FlashPink();
                inventory.RemoveOneItem(item);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        ItemWorld itemWorld = collider.GetComponent<ItemWorld>();
        if (itemWorld != null && itemWorld.CanBePickedUp())
        {
            if (!firstItemFound){
                firstItemFound = true;
                playerDialogue.ShowDialogue();

            }
            inventory.AddItem(itemWorld.GetItem());
            itemWorld.DestroySelf();
        }
    }

    private void StartFlash(Color targetColor)
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            playerSpriteRenderer.color = baseColor; // force return to original color
        }

        flashCoroutine = StartCoroutine(FlashSmooth(targetColor));
    }

    private IEnumerator FlashSmooth(Color flashColor)
    {
        float flashInTime = 0.10f;
        float flashOutTime = 0.45f;

        float timer = 0f;

        while (timer < flashInTime)
        {
            timer += Time.deltaTime;
            float t = timer / flashInTime;
            t = 1f - Mathf.Pow(1f - t, 3f);
            playerSpriteRenderer.color = Color.Lerp(baseColor, flashColor, t);
            yield return null;
        }

        playerSpriteRenderer.color = flashColor;

        timer = 0f;

        while (timer < flashOutTime)
        {
            timer += Time.deltaTime;
            float t = timer / flashOutTime;
            t = t * t;
            playerSpriteRenderer.color = Color.Lerp(flashColor, baseColor, t);
            yield return null;
        }

        playerSpriteRenderer.color = baseColor;
        flashCoroutine = null;
    }

    public void FlashGreen()
    {
        StartFlash(new Color(0.4f, 1f, 0.4f));
    }

    public void FlashBlue()
    {
        StartFlash(new Color(0.4f, 0.4f, 1f));
    }

    public void FlashPink()
    {
        StartFlash(new Color(1f, 0.4f, 0.8f));
    }
}