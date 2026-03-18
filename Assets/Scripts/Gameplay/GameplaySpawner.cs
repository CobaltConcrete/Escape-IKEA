using UnityEngine;

public class GameplaySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject meatballPrefab;
    [SerializeField] private Transform spawnParent;

    private static bool s_warnedEnemy;
    private static bool s_warnedMeatball;

    public GameObject SpawnEnemy(Vector3 worldPosition, Transform parent = null)
    {
        if (enemyPrefab == null)
        {
            if (!s_warnedEnemy)
            {
                s_warnedEnemy = true;
                Debug.LogWarning("GameplaySpawner: assign Enemy prefab.");
            }
            return null;
        }
        Transform p = parent != null ? parent : spawnParent;
        var go = Instantiate(enemyPrefab, worldPosition, Quaternion.identity, p);
        GameplayDrawOrder.ApplyEnemy(go);
        return go;
    }

    public GameObject SpawnMeatball(Vector3 worldPosition, Transform parent = null)
    {
        if (meatballPrefab == null)
        {
            if (!s_warnedMeatball)
            {
                s_warnedMeatball = true;
                Debug.LogWarning("GameplaySpawner: assign Meatball prefab.");
            }
            return null;
        }
        Transform p = parent != null ? parent : spawnParent;
        var go = Instantiate(meatballPrefab, worldPosition, Quaternion.identity, p);
        GameplayDrawOrder.ApplyMeatball(go);
        return go;
    }
}

public static class GameplayDrawOrder
{
    private const string LayerName = "Player";

    public static void ApplyEnemy(GameObject root)
    {
        Apply(root, 10);
    }

    public static void ApplyMeatball(GameObject root)
    {
        Apply(root, 8);
    }

    private static void Apply(GameObject root, int order)
    {
        if (root == null) return;
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerName = LayerName;
            sr.sortingOrder = order;
        }
    }
}
