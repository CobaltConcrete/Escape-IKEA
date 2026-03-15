using UnityEngine;

public class PlayerInventoryInteraction : MonoBehaviour {
    private Inventory inventory;
    [SerializeField] private UI_Inventory uiInventory;

    void Awake()
    {
        inventory = new Inventory();
        uiInventory.SetInventory(inventory);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        ItemWorld itemWorld = collider.GetComponent<ItemWorld>();
        if(itemWorld != null)
        {
            // Touching Item
            inventory.AddItem(itemWorld.GetItem());
            itemWorld.DestroySelf();
        }
    }
}
