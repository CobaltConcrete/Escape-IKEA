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
        public Vector3 worldPosition;
        public GameObject selectedPrefab;
    }

    private readonly Dictionary<string, RoomItemState> roomItemStates = new Dictionary<string, RoomItemState>();
    private readonly Dictionary<GameObject, string> roomObjectToKey = new Dictionary<GameObject, string>();

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

        if (!roomObjectToKey.ContainsKey(roomInstance))
        {
            roomObjectToKey.Add(roomInstance, roomKey);
        }
        else
        {
            roomObjectToKey[roomInstance] = roomKey;
        }

        if (!roomItemStates.TryGetValue(roomKey, out RoomItemState state))
        {
            state = CreateRoomItemState(roomInstance);
            roomItemStates.Add(roomKey, state);
        }

        EnsureRoomItemExists(roomInstance, state);
    }

    public void RespawnRoom(GameObject roomInstance)
    {
        if (roomInstance == null)
            return;

        if (IsTutorialRoom(roomInstance))
            return;

        if (!roomObjectToKey.TryGetValue(roomInstance, out string roomKey))
        {
            Debug.LogWarning($"ItemSpawnManager: RespawnRoom failed because room key was not registered for {roomInstance.name}");
            return;
        }

        ClearExistingRoomItem(roomInstance);

        RoomItemState newState = CreateRoomItemState(roomInstance);
        roomItemStates[roomKey] = newState;

        EnsureRoomItemExists(roomInstance, newState);
    }

    private RoomItemState CreateRoomItemState(GameObject roomInstance)
    {
        RoomItemState state = new RoomItemState();

        if (IsTutorialRoom(roomInstance))
        {
            state.shouldSpawn = false;
            return state;
        }

        if (IsCafeteriaRoom(roomInstance))
        {
            state.shouldSpawn = false;
            return state;
        }

        if (RoomLootSpawnTypeHelper.TryGetRoomType(roomInstance.transform, out RoomType roomTypeForSpawn) &&
            roomTypeForSpawn == RoomType.SportsRoom)
        {
            state.shouldSpawn = false;
            return state;
        }

        if (UnityEngine.Random.value > roomSpawnChance)
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

        RoomLootSpawnTypeHelper.TryGetRoomType(roomInstance.transform, out RoomType resolvedRoomType);

        GameObject selectedPrefab = PickRandomSpawnablePrefab(resolvedRoomType, roomInstance);
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

        Vector3 worldPoint = roomInstance.transform.TransformPoint(localPoint);

        state.shouldSpawn = true;
        state.selectedPrefab = selectedPrefab;
        state.worldPosition = worldPoint;

        return state;
    }

    private GameObject PickRandomSpawnablePrefab(RoomType resolvedRoomType, GameObject roomInstance)
    {
        if (spawnableItems == null || spawnableItems.Count == 0)
            return null;

        HashSet<ItemDefinition> definitionsAlreadyInRoom =
            RoomItemWorldQuery.CollectDefinitionsInPickupScopes(roomInstance);

        List<SpawnableRoomItem> validItems = new List<SpawnableRoomItem>();

        foreach (SpawnableRoomItem item in spawnableItems)
        {
            if (item == null || item.itemPrefab == null || item.spawnWeight <= 0f)
                continue;

            if (!IsSpawnPrefabAllowedInRoom(item.itemPrefab, resolvedRoomType))
                continue;

            ItemWorldSpawner spawner = item.itemPrefab.GetComponent<ItemWorldSpawner>();
            ItemDefinition def = spawner != null ? spawner.ItemDefinition : null;
            if (def != null && definitionsAlreadyInRoom.Contains(def))
                continue;

            validItems.Add(item);
        }

        if (validItems.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (SpawnableRoomItem item in validItems)
        {
            totalWeight += item.spawnWeight;
        }

        float roll = UnityEngine.Random.value * totalWeight;
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

    private static bool IsSpawnPrefabAllowedInRoom(GameObject prefab, RoomType resolvedRoomType)
    {
        ItemWorldSpawner spawner = prefab != null ? prefab.GetComponent<ItemWorldSpawner>() : null;
        ItemDefinition definition = spawner != null ? spawner.ItemDefinition : null;

        if (definition == null)
            return false;

        if (definition.allowedRoomTypes == null || definition.allowedRoomTypes.Count == 0)
            return false;

        return definition.allowedRoomTypes.Contains(resolvedRoomType);
    }

    private void EnsureRoomItemExists(GameObject roomInstance, RoomItemState state)
    {
        Transform itemParent = roomInstance.transform.Find("SpawnedItems");
        Transform searchRoot = itemParent != null ? itemParent : roomInstance.transform;

        Transform existing = searchRoot.Find(spawnedItemName);

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

        Transform parentToUse = itemParent != null ? itemParent : roomInstance.transform;

        GameObject spawnedSpawner = Instantiate(
            state.selectedPrefab,
            state.worldPosition,
            Quaternion.identity,
            parentToUse
        );

        spawnedSpawner.name = spawnedItemName;

        ItemWorldSpawner spawner = spawnedSpawner.GetComponent<ItemWorldSpawner>();
        if (spawner != null)
        {
            spawner.SetSpawnParent(parentToUse);
        }
    }

    private void ClearExistingRoomItem(GameObject roomInstance)
    {
        if (roomInstance == null)
            return;

        Transform itemParent = roomInstance.transform.Find("SpawnedItems");
        Transform searchRoot = itemParent != null ? itemParent : roomInstance.transform;

        Transform existing = searchRoot.Find(spawnedItemName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
    }

    private string MakeRoomKey(int x, int y, int width, int height)
    {
        return $"{x}_{y}_{width}_{height}";
    }

    private bool IsTutorialRoom(GameObject roomInstance)
    {
        if (roomInstance == null) return false;

        string roomName = roomInstance.name;
        return roomName.Contains("Tutorial");
    }

    private static bool IsCafeteriaRoom(GameObject roomInstance)
    {
        if (roomInstance == null)
            return false;

        if (RoomLootSpawnTypeHelper.TryGetRoomType(roomInstance.transform, out RoomType roomType) &&
            roomType == RoomType.Cafeteria)
        {
            return true;
        }

        return roomInstance.name.IndexOf("Cafeteria", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}