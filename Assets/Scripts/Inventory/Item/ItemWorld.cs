using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TMPro;
using CodeMonkey.Utils;
using static EquipmentEnum;

public class ItemWorld : MonoBehaviour, IInteractable
{
    private const float WorldPickupColliderScale = 0.24f;
    private Room currentRoom;

    [SerializeField] private bool lockRoomOnSpawn = true;

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

    private Item item;
    private SpriteRenderer spriteRenderer;
    private Light2D light2D;
    private TextMeshPro textMeshPro;
    private float canBePickedUpTimer;
    private Vector3 defaultScale;
    private BoxCollider2D boxCollider2D;
    private Rigidbody2D rigidbody2D;
    private Transform contrastPlateTransform;

    private static Sprite s_unitWhiteSprite;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        light2D = GetComponent<Light2D>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        rigidbody2D = GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            // World pickups should block movement but never be pushed by player/enemies.
            rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            rigidbody2D.simulated = true;
            rigidbody2D.freezeRotation = true;
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }

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

    public void SetRoom(Room room)
    {
        if (lockRoomOnSpawn)
        {
            currentRoom = room;
        }
    }

    public Room GetRoom()
    {
        return currentRoom;
    }

    public void SetRoomVisible(bool visible)
    {
        // LukeScene ???????????????
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "LukeScene")
        {
            visible = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        if (contrastPlateTransform != null)
        {
            SpriteRenderer plateSr = contrastPlateTransform.GetComponent<SpriteRenderer>();
            if (plateSr != null)
            {
                plateSr.enabled = visible;
            }
        }

        if (textMeshPro != null)
        {
            textMeshPro.enabled = visible;
        }

        // Loot ?????????????? item ????????????
        if (light2D != null && item != null && !item.IsLoot())
        {
            light2D.enabled = visible;
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
                // Loot ??????
                light2D.enabled = false;
            }
            else
            {
                // ??? Item ????
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

        ApplyColliderFromDefinition();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = "Item";
            // Weapons draw above generic pickups (same layer) so large sprites never read as "under" floor props.
            spriteRenderer.sortingOrder =
                item.definition.equipTag == EquipTag.Weapon ? 18 : 2;
        }

        ConfigureContrastBackdrop();
    }

    private static Sprite GetUnitWhiteSprite()
    {
        if (s_unitWhiteSprite != null)
        {
            return s_unitWhiteSprite;
        }

        Texture2D tex = Texture2D.whiteTexture;
        s_unitWhiteSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return s_unitWhiteSprite;
    }

    private void ConfigureContrastBackdrop()
    {
        if (item?.definition == null || !item.definition.worldContrastBackdrop)
        {
            if (contrastPlateTransform != null)
            {
                Destroy(contrastPlateTransform.gameObject);
                contrastPlateTransform = null;
            }

            return;
        }

        Transform existing = transform.Find("ContrastBackdrop");
        GameObject plate = existing != null ? existing.gameObject : new GameObject("ContrastBackdrop");
        plate.transform.SetParent(transform, false);
        plate.transform.localPosition = Vector3.zero;
        plate.transform.localRotation = Quaternion.identity;

        float w = 0.55f;
        float h = 0.55f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector3 bs = spriteRenderer.sprite.bounds.size;
            w = Mathf.Max(0.08f, bs.x) * 1.45f;
            h = Mathf.Max(0.08f, bs.y) * 1.45f;
        }

        plate.transform.localScale = new Vector3(w, h, 1f);
        contrastPlateTransform = plate.transform;

        SpriteRenderer plateSr = plate.GetComponent<SpriteRenderer>();
        if (plateSr == null)
        {
            plateSr = plate.AddComponent<SpriteRenderer>();
        }

        plateSr.sprite = GetUnitWhiteSprite();
        plateSr.color = new Color(1f, 1f, 1f, 0.94f);
        plateSr.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Item";
        plateSr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : 1;
    }

    private void ApplyColliderFromDefinition()
    {
        if (item == null || item.definition == null)
            return;

        var def = item.definition;

        if (def.worldColliderType == WorldColliderType.None)
        {
            return;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var c in colliders)
        {
            Destroy(c);
        }

        switch (def.worldColliderType)
        {
            case WorldColliderType.Box:
                {
                    BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                    box.isTrigger = false;
                    box.offset = def.boxOffset;
                    box.size = def.boxSize * WorldPickupColliderScale;
                    box.edgeRadius = def.boxEdgeRadius;
                    break;
                }

            case WorldColliderType.Circle:
                {
                    CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
                    circle.isTrigger = false;
                    circle.offset = def.circleOffset;
                    circle.radius = def.circleRadius * WorldPickupColliderScale;
                    break;
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

    internal static ItemWorld DropItem(Vector3 dropPosition, Item item, Transform parent = null, Room room = null)
    {
        Vector3 randomDir = UtilsClass.GetRandomDir();

        ItemWorld itemWorld = SpawnItemWorld(
            dropPosition + randomDir * 1.1f,
            Quaternion.identity,
            item.worldScale,
            item
        );

        if (itemWorld == null) return null;

        if (parent != null)
        {
            itemWorld.transform.SetParent(parent, true);
        }

        if (room != null)
        {
            itemWorld.SetRoom(room);
        }

        Debug.LogWarning(
            $"[DropItem] Spawned {itemWorld.name}, parent={(itemWorld.transform.parent != null ? itemWorld.transform.parent.name : "ROOT")}",
            itemWorld
        );

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

        if (!IsEligibleForWorldPickup())
            return;

        if (item.IsLoot())
        {
            player.PickupLoot(this);
            return;
        }

        player.PickupNormalItemWorld(this);
    }

    public string GetInteractionText()
    {
        if (item == null || item.definition == null) return "";

        if (!IsEligibleForWorldPickup())
            return "";

        if (item.IsLoot())
            return "[F] Pick up " + item.definition.itemName;

        return "[F] Pick up " + item.definition.itemName;
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// World pickups: normal items are always interactable; loot is interactable only when needed by the shopping list.
    /// </summary>
    private bool IsEligibleForWorldPickup()
    {
        if (item == null || item.definition == null)
            return false;

        // Non-loot items (equipment/consumables like speed potion) should be pickable in-world.
        if (!item.IsLoot())
            return true;

        return IsLootOnCurrentShoppingList();
    }

    /// <summary>
    /// Loot may only be picked up when it appears on the current run shopping list (when objectives are active).
    /// </summary>
    private bool IsLootOnCurrentShoppingList()
    {
        if (item == null || item.definition == null || !item.IsLoot())
            return false;

        RunObjectiveManager rom = RunObjectiveManager.Instance;
        if (rom == null)
            return false;

        return rom.NeedsMoreOfShoppingListKey(item.definition.GetShoppingListKey());
    }
}