using UnityEngine;

public class ItemAssets : MonoBehaviour
{
    public static ItemAssets Instance { get; private set; }

    public static ItemAssets GetInstance()
    {
        if (Instance == null)
        {
            Instance = Object.FindFirstObjectByType<ItemAssets>(FindObjectsInactive.Include);
        }
        return Instance;
    }

    public Transform pfItemWorld_Normal;
    public Transform pfItemWorld_Loot;

    private void Awake()
    {
        Instance = this;
    }

    public Transform GetPfItemWorld(Item item)
    {
        if (item != null && item.IsLoot())
        {
            return pfItemWorld_Loot;
        }
        return pfItemWorld_Normal;
    }
}