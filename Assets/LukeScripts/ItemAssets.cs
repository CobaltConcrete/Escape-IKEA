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

    private void Awake()
    {
        Instance = this;
    }

    public Transform pfItemWorld;

    public Sprite swordSprite;
    public Sprite healthPillSprite;
    public Sprite anotherPillSprite;
    public Sprite coinSprite;
    public Sprite medkitSprite;
}