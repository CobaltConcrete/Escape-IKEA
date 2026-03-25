using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnableRoomItem
{
    public GameObject itemPrefab;

    [Range(0f, 1f)]
    public float spawnWeight = 1f;
}

public class ItemSpawnManager : MonoBehaviour
{
    public static ItemSpawnManager Instance { get; private set; }

    [Header("Room Spawn Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float roomSpawnChance = 0.35f;

    [SerializeField] private List<SpawnableRoomItem> spawnableItems;

    [SerializeField] private string spawnedItemName = "SpawnedRoomItem";

    private class RoomItemState
    {
        public bool shouldSpawn;
        public Vector3 localPosition;
        public GameObject selectedPrefab;
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
        if (roomInstance == null)
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

        if (Random.value > roomSpawnChance)
        {
            state.shouldSpawn = false;
            return state;
        }

        RoomSpawnArea spawnArea = roomInstance.GetComponentInChildren<RoomSpawnArea>();
        if (spawnArea == null)
        {
            state.shouldSpawn = false;
            return state;
        }

        GameObject selectedPrefab = PickRandomSpawnablePrefab();
        if (selectedPrefab == null)
        {
            state.shouldSpawn = false;
            return state;
        }

        if (!spawnArea.TryGetRandomLocalPoint(roomInstance.transform, out Vector3 localPoint))
        {
            state.shouldSpawn = false;
            return state;
        }

        state.shouldSpawn = true;
        state.selectedPrefab = selectedPrefab;
        state.localPosition = localPoint;

        return state;
    }

    private GameObject PickRandomSpawnablePrefab()
    {
        if (spawnableItems == null || spawnableItems.Count == 0)
            return null;

        List<SpawnableRoomItem> validItems = new List<SpawnableRoomItem>();

        foreach (SpawnableRoomItem item in spawnableItems)
        {
            if (item != null && item.itemPrefab != null && item.spawnWeight > 0f)
            {
                validItems.Add(item);
            }
        }

        if (validItems.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (SpawnableRoomItem item in validItems)
        {
            totalWeight += item.spawnWeight;
        }

        float roll = Random.value * totalWeight;
        float current = 0f;

        foreach (SpawnableRoomItem item in validItems)
        {
            current += item.spawnWeight;
            if (roll <= current)
            {
                return item.itemPrefab;
            }
        }

        return validItems[validItems.Count - 1].itemPrefab;
    }

    private void EnsureRoomItemExists(GameObject roomInstance, RoomItemState state)
    {
        Transform existing = roomInstance.transform.Find(spawnedItemName);

        if (!state.shouldSpawn || state.selectedPrefab == null)
        {
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
            return;
        }

        if (existing != null)
            return;

        GameObject spawnedItem = Instantiate(state.selectedPrefab, roomInstance.transform);
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