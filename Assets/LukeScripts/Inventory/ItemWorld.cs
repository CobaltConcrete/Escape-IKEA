using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using CodeMonkey.Utils;

public class ItemWorld : MonoBehaviour, IInteractable
{
    public static ItemWorld SpawnItemWorld(Vector3 position, Item item)
    {
        return SpawnItemWorld(position, Quaternion.identity, Vector3.one, item);
    }

    public static ItemWorld SpawnItemWorld(Vector3 position, Quaternion rotation, Vector3 scale, Item item)
    {
        ItemAssets itemAssets = ItemAssets.GetInstance();

        if (itemAssets == null)
        {
            Debug.LogError("No ItemAssets found in scene!");
            return null;
        }

        if (itemAssets.pfItemWorld == null)
        {
            Debug.LogError("pfItemWorld is not assigned on ItemAssets!");
            return null;
        }

        Transform spawnedTransform = Instantiate(itemAssets.pfItemWorld, position, rotation);
        spawnedTransform.localScale = scale;

        ItemWorld itemWorld = spawnedTransform.GetComponent<ItemWorld>();
        if (itemWorld == null)
        {
            Debug.LogError("pfItemWorld prefab is missing ItemWorld component!");
            return null;
        }

        itemWorld.SetItem(item);
        return itemWorld;
    }
    //public static ItemWorld SpawnItemWorld(Vector3 position, Item item)
    //{
    //    ItemAssets itemAssets = ItemAssets.GetInstance();

    //    if (itemAssets == null)
    //    {
    //        Debug.LogError("No ItemAssets found in scene!");
    //        return null;
    //    }

    //    if (itemAssets.pfItemWorld == null)
    //    {
    //        Debug.LogError("pfItemWorld is not assigned on ItemAssets!");
    //        return null;
    //    }

    //    Transform spawnedTransform = Instantiate(itemAssets.pfItemWorld, position, Quaternion.identity);

    //    ItemWorld itemWorld = spawnedTransform.GetComponent<ItemWorld>();
    //    if (itemWorld == null)
    //    {
    //        Debug.LogError("pfItemWorld prefab is missing ItemWorld component!");
    //        return null;
    //    }

    //    itemWorld.SetItem(item);
    //    return itemWorld;
    //}

    private Item item;
    private SpriteRenderer spriteRenderer;
    private Light2D light2D;
    private TextMeshPro textMeshPro;
    private float canBePickedUpTimer;
    private Vector3 defaultScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        light2D = GetComponent<Light2D>();

        Transform amountTransform = transform.Find("Amount");
        if (amountTransform != null)
        {
            textMeshPro = amountTransform.GetComponent<TextMeshPro>();
        }

        defaultScale = transform.localScale;
    }

    private void Update()
    {
        if (canBePickedUpTimer > 0f)
        {
            canBePickedUpTimer -= Time.deltaTime;
        }
    }

    public void SetCanBePickedUpTimer(float time)
    {
        canBePickedUpTimer = time;
    }

    public bool CanBePickedUp()
    {
        return canBePickedUpTimer <= 0f;
    }

    public void SetItem(Item item)
    {
        this.item = item;

        if (item == null || item.definition == null)
        {
            Debug.LogError("Item or ItemDefinition is null!", this);
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer missing on ItemWorld prefab!", this);
            return;
        }

        spriteRenderer.sprite = item.GetSprite();

        if (light2D != null)
        {
            if (item.IsLoot())
            {
                // Loot
                light2D.enabled = false;
            }
            else
            {
                // Item
                light2D.enabled = true;
                light2D.color = item.GetColor();
                light2D.intensity = 1f;
                light2D.pointLightOuterRadius = 1f;
            }
        }

        if (textMeshPro != null)
        {
            if (item.amount > 1)
            {
                textMeshPro.SetText(item.amount.ToString());
            }
            else
            {
                textMeshPro.SetText("");
            }
        }
    }

    public Item GetItem()
    {
        return item;
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    internal static ItemWorld DropItem(Vector3 dropPosition, Item item)
    {
        Vector3 randomDir = UtilsClass.GetRandomDir();

        ItemWorld itemWorld = SpawnItemWorld(dropPosition + randomDir * 1.1f, Quaternion.identity, item.worldScale, item);

        if (itemWorld == null) return null;

        itemWorld.SetCanBePickedUpTimer(0.25f);

        Rigidbody2D rb = itemWorld.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(randomDir * 3.5f, ForceMode2D.Impulse);
        }

        return itemWorld;
    }

    public void Interact(PlayerInventoryInteraction player)
    {
        if (player == null) return;
        if (!CanBePickedUp()) return;
        if (item == null || item.definition == null) return;

        if (item.IsLoot())
        {
            player.PickupLoot(this);
        }
    }

    public string GetInteractionText()
    {
        if (item == null || item.definition == null) return "";

        if (item.IsLoot())
        {
            return "[F] Pick up " + item.definition.itemName;
        }

        return "";
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }


}