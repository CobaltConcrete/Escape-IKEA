using UnityEngine;

public class EnemySpawnController : MonoBehaviour
{
    [System.Serializable]
    public class SpawnGroup
    {
        public string groupName;
        public GameObject prefab;

        [Range(0f, 1f)]
        public float spawnChance = 1f;

        public int minCount = 0;
        public int maxCount = 2;

        public int sortingOrder = 10;
    }

    [Header("Enemy Spawn Area")]
    [SerializeField] private Collider2D enemySpawnAreaCollider;

    [Header("Enemy Wander Area")]
    [SerializeField] private Collider2D enemyWanderAreaCollider;

    [Header("Spawn Parent")]
    [SerializeField] private Transform spawnParent;

    [Header("Spawn Groups")]
    [SerializeField] private SpawnGroup[] spawnGroups;

    [Header("Spawn Settings")]
    [SerializeField] private int maxPositionTriesPerSpawn = 20;
    [SerializeField] private float edgePadding = 0.5f;
    [SerializeField] private float overlapCheckRadius = 0.4f;
    [SerializeField] private float doorAvoidRadius = 1.2f;
    [SerializeField] private LayerMask blockingLayers;

    private bool hasSpawned = false;

    private void Start()
    {
        SpawnRoomContents();
    }

    [ContextMenu("Spawn Room Contents")]
    public void SpawnRoomContents()
    {
        if (hasSpawned) return;
        if (IsTutorialRoom())
        {
            hasSpawned = true;
            return;
        }

        if (enemySpawnAreaCollider == null)
        {
            Debug.LogWarning($"EnemySpawnController on {name}: enemySpawnAreaCollider is not assigned.");
            return;
        }

        Collider2D boundsSource = enemyWanderAreaCollider != null ? enemyWanderAreaCollider : enemySpawnAreaCollider;

        Bounds spawnBounds = enemySpawnAreaCollider.bounds;
        Bounds wanderBounds = boundsSource.bounds;

        Vector2 min = wanderBounds.min;
        Vector2 max = wanderBounds.max;

        foreach (SpawnGroup group in spawnGroups)
        {
            if (group.prefab == null) continue;
            if (IsBossGroup(group) && !IsCafeteriaRoom())
                continue;

            float roll = Random.value;
            if (roll > group.spawnChance) continue;

            int count = Random.Range(group.minCount, group.maxCount + 1);

            for (int i = 0; i < count; i++)
            {
                if (!TryGetSpawnPosition(spawnBounds, out Vector3 spawnPos))
                    continue;

                Transform parentToUse = spawnParent != null ? spawnParent : transform;
                GameObject spawned = Instantiate(group.prefab, spawnPos, Quaternion.identity, parentToUse);

                ApplySortingOrder(spawned, group.sortingOrder);
                AssignMovementBounds(spawned, min, max);
            }
        }

        hasSpawned = true;
    }

    private bool TryGetSpawnPosition(Bounds bounds, out Vector3 position)
    {
        for (int i = 0; i < maxPositionTriesPerSpawn; i++)
        {
            float minX = bounds.min.x + edgePadding;
            float maxX = bounds.max.x - edgePadding;
            float minY = bounds.min.y + edgePadding;
            float maxY = bounds.max.y - edgePadding;

            if (minX > maxX || minY > maxY)
                break;

            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);

            Vector3 testPos = new Vector3(x, y, 0f);

            if (!enemySpawnAreaCollider.OverlapPoint(testPos))
                continue;

            if (Physics2D.OverlapCircle(testPos, overlapCheckRadius, blockingLayers) != null)
                continue;

            if (IsNearDoor(testPos))
                continue;

            position = testPos;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsNearDoor(Vector3 testPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(testPos, doorAvoidRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag("Door"))
                return true;
        }
        return false;
    }

    private bool IsTutorialRoom()
    {
        string n = gameObject.name;
        return n.Contains("SportsRoom") || n.Contains("Tutorial");
    }

    private bool IsCafeteriaRoom()
    {
        string n = gameObject.name;
        return n.Contains("CafeteriaRoom");
    }

    private bool IsBossGroup(SpawnGroup group)
    {
        if (group == null) return false;
        if (!string.IsNullOrEmpty(group.groupName) &&
            group.groupName.Contains("CafeteriaBoss"))
            return true;
        string prefabName = group.prefab != null ? group.prefab.name : "";
        if (prefabName.Contains("CafeteriaBoss")) return true;
        return false;
    }

    private void AssignMovementBounds(GameObject spawnedObject, Vector2 min, Vector2 max)
    {
        EnemyWander[] wanderers = spawnedObject.GetComponentsInChildren<EnemyWander>(true);
        foreach (EnemyWander wander in wanderers)
        {
            wander.SetBounds(min, max);
        }

        Enemy[] enemies = spawnedObject.GetComponentsInChildren<Enemy>(true);
        foreach (Enemy enemy in enemies)
        {
            enemy.SetBounds(min, max);
        }
    }

    private void ApplySortingOrder(GameObject root, int order)
    {
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerName = "Player";
            sr.sortingOrder = order;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (enemySpawnAreaCollider != null)
        {
            Gizmos.color = Color.red;
            Bounds b = enemySpawnAreaCollider.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        if (enemyWanderAreaCollider != null)
        {
            Gizmos.color = Color.green;
            Bounds b = enemyWanderAreaCollider.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}