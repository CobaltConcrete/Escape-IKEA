using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnManager : MonoBehaviour
{
    public static ItemSpawnManager Instance { get; private set; }

    [Header("Item Spawn Settings")]
    [SerializeField] private GameObject itemPrefab;

    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.35f;

    [SerializeField] private string spawnedItemName = "SpawnedRoomItem";

    private class RoomItemState
    {
        public bool shouldSpawn;
        public Vector3 localPosition;
    }

    private readonly Dictionary<string, RoomItemState> roomItemStates = new Dictionary<string, RoomItemState>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterRoom(GameObject roomInstance, int cellX, int cellY, int cellWidth, int cellHeight)
    {
        if (roomInstance == null || itemPrefab == null)
            return;

        string roomKey = MakeRoomKey(cellX, cellY, cellWidth, cellHeight);

        if (!roomItemStates.TryGetValue(roomKey, out RoomItemState state))
        {
            state = CreateRoomItemState(roomInstance);
            roomItemStates.Add(roomKey, state);
        }

        EnsureRoomItemExists(roomInstance, state);
    }

    private RoomItemState CreateRoomItemState(GameObject roomInstance)
    {
        RoomItemState state = new RoomItemState();

        state.shouldSpawn = Random.value <= spawnChance;

        if (!state.shouldSpawn)
            return state;

        RoomSpawnArea spawnArea = roomInstance.GetComponentInChildren<RoomSpawnArea>();

        if (spawnArea == null)
        {
            state.shouldSpawn = false;
            return state;
        }

        if (!spawnArea.TryGetRandomLocalPoint(roomInstance.transform, out Vector3 localPoint))
        {
            state.shouldSpawn = false;
            return state;
        }

        state.localPosition = localPoint;
        return state;
    }

    private void EnsureRoomItemExists(GameObject roomInstance, RoomItemState state)
    {
        Transform existing = roomInstance.transform.Find(spawnedItemName);

        if (!state.shouldSpawn)
        {
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
            return;
        }

        if (existing != null)
            return;

        GameObject spawnedItem = Instantiate(itemPrefab, roomInstance.transform);
        spawnedItem.name = spawnedItemName;
        spawnedItem.transform.localPosition = state.localPosition;
        spawnedItem.transform.localRotation = Quaternion.identity;
        spawnedItem.transform.localScale = Vector3.one;
    }

    private string MakeRoomKey(int x, int y, int width, int height)
    {
        return $"{x}_{y}_{width}_{height}";
    }
}