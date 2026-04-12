using System.Collections.Generic;
using UnityEngine;

public class EnemyRespawnManager : MonoBehaviour
{
    public static EnemyRespawnManager Instance { get; private set; }

    [Header("Respawn Rule")]
    [SerializeField] private int visitsBeforeRespawn = 3;

    private class RoomEnemyState
    {
        public int initialEnemyCount;
        public int targetEnemyCount;
        public bool shouldRespawn;
        public readonly List<GameObject> aliveEnemies = new List<GameObject>();
    }

    private readonly Dictionary<GameObject, RoomEnemyState> roomStates = new Dictionary<GameObject, RoomEnemyState>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterRoom(GameObject room)
    {
        if (room == null)
            return;

        if (!roomStates.ContainsKey(room))
        {
            roomStates.Add(room, new RoomEnemyState());
        }
    }

    public void SetTargetEnemyCount(GameObject room, int count)
    {
        if (room == null)
            return;

        RegisterRoom(room);

        RoomEnemyState state = roomStates[room];
        int safeCount = Mathf.Max(0, count);

        if (state.initialEnemyCount <= 0)
        {
            state.initialEnemyCount = safeCount;
        }

        state.targetEnemyCount = safeCount;
    }

    public void RegisterSpawnedEnemy(GameObject room, GameObject enemy)
    {
        if (room == null || enemy == null)
            return;

        RegisterRoom(room);

        RoomEnemyState state = roomStates[room];
        if (!state.aliveEnemies.Contains(enemy))
        {
            state.aliveEnemies.Add(enemy);
        }
    }

    public void NotifyEnemyDied(GameObject room, GameObject enemy)
    {
        if (room == null || enemy == null)
            return;

        if (!roomStates.TryGetValue(room, out RoomEnemyState state))
            return;

        state.aliveEnemies.Remove(enemy);
        state.aliveEnemies.RemoveAll(e => e == null);
    }

    public void NotifyRoomEntered(GameObject room)
    {
        if (room == null)
            return;

        RegisterRoom(room);

        foreach (KeyValuePair<GameObject, RoomEnemyState> pair in roomStates)
        {
            GameObject otherRoom = pair.Key;
            RoomEnemyState otherState = pair.Value;

            if (otherRoom == room)
                continue;

            int aliveCount = CountAliveEnemies(otherRoom);
            int missingCount = otherState.targetEnemyCount - aliveCount;

            if (missingCount <= 0)
                continue;

            int distinctRoomsAfter = RoomVisitTracker.Instance != null
                ? RoomVisitTracker.Instance.CountDistinctRoomsAfter(otherRoom.GetInstanceID().ToString())
                : 0;


            if (distinctRoomsAfter >= visitsBeforeRespawn)
            {
                otherState.shouldRespawn = true;
            }
        }
    }

    public bool ShouldRespawnRoom(GameObject room)
    {
        if (room == null)
            return false;

        if (!roomStates.TryGetValue(room, out RoomEnemyState state))
            return false;

        return state.shouldRespawn;
    }

    public void MarkRoomRespawnHandled(GameObject room)
    {
        if (room == null)
            return;

        if (!roomStates.TryGetValue(room, out RoomEnemyState state))
            return;

        state.shouldRespawn = false;
    }

    public void ClearDeadReferences(GameObject room)
    {
        if (room == null)
            return;

        if (!roomStates.TryGetValue(room, out RoomEnemyState state))
            return;

        state.aliveEnemies.RemoveAll(e => e == null);
    }

    public int GetAliveEnemyCount(GameObject room)
    {
        if (room == null)
            return 0;

        if (!roomStates.TryGetValue(room, out RoomEnemyState state))
            return 0;

        state.aliveEnemies.RemoveAll(e => e == null);
        return state.aliveEnemies.Count;
    }

    public int GetTargetEnemyCount(GameObject room)
    {
        if (room == null)
            return 0;

        if (!roomStates.TryGetValue(room, out RoomEnemyState state))
            return 0;

        return state.targetEnemyCount;
    }

    private int CountAliveEnemies(GameObject room)
    {
        if (room == null)
            return 0;

        if (!roomStates.TryGetValue(room, out RoomEnemyState state))
            return 0;

        state.aliveEnemies.RemoveAll(e => e == null);
        return state.aliveEnemies.Count;
    }
    public int GetInitialEnemyCount(GameObject room)
    {
        if (room == null)
            return 0;

        if (!roomStates.TryGetValue(room, out RoomEnemyState state))
            return 0;

        return state.initialEnemyCount;
    }

    public void SetRespawnTargetEnemyCount(GameObject room, int count)
    {
        if (room == null)
            return;

        RegisterRoom(room);
        roomStates[room].targetEnemyCount = Mathf.Max(0, count);
    }
}