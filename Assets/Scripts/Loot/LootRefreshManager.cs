using UnityEngine;

public class LootRefreshManager : MonoBehaviour
{
    public static LootRefreshManager Instance { get; private set; }

    [Header("Refresh Settings")]
    [SerializeField] private float refreshInterval = 120f;
    [SerializeField] private bool refreshOnlyIfObjectiveIncomplete = true;

    [Header("Bonus Loot Refresh")]
    [SerializeField] private int extraSpawnCountPerRefresh = 3;
    [SerializeField] private int maxRefreshCount = 2;

    private float timer;
    private int currentRefreshCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        timer = refreshInterval;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = refreshInterval;
        TryRefreshLoot();
    }

    private void TryRefreshLoot()
    {
        if (currentRefreshCount >= maxRefreshCount)
        {
            return;
        }

        if (refreshOnlyIfObjectiveIncomplete &&
            RunObjectiveManager.Instance != null &&
            RunObjectiveManager.Instance.IsObjectiveComplete())
        {
            return;
        }

        currentRefreshCount++;

        if (LootSpawnManager.Instance != null && RunObjectiveManager.Instance != null)
        {
            Debug.Log("LootRefreshManager: Spawning additional bonus loot.");

            LootSpawnManager.Instance.SpawnAdditionalBonusLoot(
                extraSpawnCountPerRefresh,
                RunObjectiveManager.Instance.GetAllItemDefinitions()
            );
        }
    }
}