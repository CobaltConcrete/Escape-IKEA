using System.Collections.Generic;
using UnityEngine;

public class RoomEnemyRespawnAnchor : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform enemyParent;
    [SerializeField] private List<GameObject> enemyPrefabs;
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private int respawnExtraCount = 2;
    [SerializeField] private EnemySpawnArea enemySpawnArea;

    private GameObject roomOwner;
    private EnemySpawnController spawnController;

    private void Awake()
    {
        if (enemyParent == null)
        {
            enemyParent = transform;
        }

        if (enemySpawnArea == null)
        {
            enemySpawnArea = GetComponentInChildren<EnemySpawnArea>();
        }

        spawnController = GetComponentInChildren<EnemySpawnController>();

        RoomContentActivation activation = GetComponentInParent<RoomContentActivation>();
        roomOwner = activation != null ? activation.gameObject : gameObject;

        EnemyRespawnManager.Instance?.RegisterRoom(roomOwner);
    }

    private void Start()
    {
        RegisterExistingEnemies();
    }

    public void OnPlayerEnteredRoom()
    {

        EnemyRespawnManager.Instance?.NotifyRoomEntered(roomOwner);

        bool shouldRespawn = EnemyRespawnManager.Instance != null &&
                             EnemyRespawnManager.Instance.ShouldRespawnRoom(roomOwner);


        if (shouldRespawn)
        {
            RefillMissingEnemies();
        }
    }

    public void NotifyEnemyDied(GameObject enemy)
    {
        EnemyRespawnManager.Instance?.NotifyEnemyDied(roomOwner, enemy);
    }

    private void RegisterExistingEnemies()
    {
        List<GameObject> existingEnemies = new List<GameObject>();

        foreach (Transform child in enemyParent)
        {
            if (!child.CompareTag("Enemy") && child.GetComponent<Enemy>() == null)
                continue;

            GameObject enemy = child.gameObject;

            EnemyRoomMember member = enemy.GetComponent<EnemyRoomMember>();
            if (member == null)
            {
                member = enemy.AddComponent<EnemyRoomMember>();
            }

            member.Initialize(this);

            existingEnemies.Add(enemy);
            EnemyRespawnManager.Instance?.RegisterSpawnedEnemy(roomOwner, enemy);
        }

        int targetCount = existingEnemies.Count > 0 ? existingEnemies.Count : spawnCount;
        EnemyRespawnManager.Instance?.SetTargetEnemyCount(roomOwner, targetCount);
    }

    private void RefillMissingEnemies()
    {
        if (EnemyRespawnManager.Instance == null)
            return;

        if (spawnController == null)
        {
            Debug.LogWarning($"RoomEnemyRespawnAnchor on {name}: EnemySpawnController not found.");
            return;
        }

        EnemyRespawnManager.Instance.ClearDeadReferences(roomOwner);

        int aliveCount = EnemyRespawnManager.Instance.GetAliveEnemyCount(roomOwner);

        int initialCount = EnemyRespawnManager.Instance.GetInitialEnemyCount(roomOwner);
        int desiredTargetCount = initialCount + respawnExtraCount;

        EnemyRespawnManager.Instance.SetRespawnTargetEnemyCount(roomOwner, desiredTargetCount);

        int targetCount = EnemyRespawnManager.Instance.GetTargetEnemyCount(roomOwner);
        int missingCount = targetCount - aliveCount;

        if (missingCount <= 0)
        {
            EnemyRespawnManager.Instance.MarkRoomRespawnHandled(roomOwner);
            return;
        }

        spawnController.RefillEnemies(missingCount, this, roomOwner);

        EnemyRespawnManager.Instance.MarkRoomRespawnHandled(roomOwner);
    }

    public Transform GetEnemyParent()
    {
        return enemyParent != null ? enemyParent : transform;
    }

    public EnemySpawnArea GetEnemySpawnArea()
    {
        return enemySpawnArea;
    }

    public List<GameObject> GetEnemyPrefabs()
    {
        return enemyPrefabs;
    }

    public int GetSpawnCount()
    {
        return spawnCount;
    }
}