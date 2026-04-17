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

    public Transform pfItemWorld;

    private void Awake()
    {
        Instance = this;
    }

    public Transform GetPfItemWorld()
    {
        return pfItemWorld;
    }
}