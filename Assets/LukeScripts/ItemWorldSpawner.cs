using UnityEngine;

public class ItemWorldSpawner : MonoBehaviour
{
    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int amount = 1;

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
            amount = amount
        };

        ItemWorld.SpawnItemWorld(transform.position, item);
        Destroy(gameObject);
    }
}