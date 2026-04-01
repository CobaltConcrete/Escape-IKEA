using UnityEngine;

public class ItemWorldSpawner : MonoBehaviour
{
    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int amount = 1;

    private Transform spawnParent;

    public void SetSpawnParent(Transform parent)
    {
        spawnParent = parent;
    }

    private void Start()
    {
        if (itemDefinition == null)
        {
            Debug.LogWarning("ItemWorldSpawner: itemDefinition is null", this);
            return;
        }

        Item item = new Item
        {
            definition = itemDefinition,
            amount = amount,
            worldScale = transform.lossyScale
        };

        ItemWorld spawned = ItemWorld.SpawnItemWorld(
            transform.position,
            transform.rotation,
            transform.lossyScale,
            item
        );

        if (spawned != null && spawnParent != null)
        {
            spawned.transform.SetParent(spawnParent, true);
        }

        Destroy(gameObject);
    }
}