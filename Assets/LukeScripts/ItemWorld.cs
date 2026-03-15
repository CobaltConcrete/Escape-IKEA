using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ItemWorld : MonoBehaviour
{
    public static ItemWorld SpawnItemWorld(Vector3 position, Item item)
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

        Transform spawnedTransform = Instantiate(itemAssets.pfItemWorld, position, Quaternion.identity);

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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        light2D = GetComponent<Light2D>();
    }

    public void SetItem(Item item)
    {
        this.item = item;

        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer missing on ItemWorld prefab!", this);
            return;
        }

        spriteRenderer.sprite = item.GetSprite();

        if (light2D != null)
        {
            light2D.color = item.GetColor();
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
}