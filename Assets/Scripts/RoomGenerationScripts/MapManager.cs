using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RoomPrefab
{
    public GameObject prefab;
    public int cellWidth = 1;
    public int cellHeight = 1;
}

public class MapManager : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] public int maxCellRows = 5;
    [SerializeField] public int maxCellCols = 5;
    [SerializeField] public Vector2 unitCellSize = new Vector2(10f, 10f);

    [Header("Room Prefabs")]
    [SerializeField] private RoomPrefab[] roomPrefabs;
    [SerializeField] private GameObject startingRoomPrefab;
    [SerializeField] private RoomPrefab bossRoom;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Doors")]
    [SerializeField] private GameObject horizontalDoorPrefab;
    [SerializeField] private GameObject verticalDoorPrefab;
    [SerializeField] private GameObject horizontalBoundaryDoorPrefab;
    [SerializeField] private GameObject verticalBoundaryDoorPrefab;

    [Header("Room presentation")]
    [Tooltip("UIArt/fixedtile (64px @ 100 PPU). Tiled in a grid; edge tiles are clipped to the room interior by a Sprite Mask.")]
    [SerializeField] private Sprite roomFloorTileSprite;
    [SerializeField] private RoomDecorationCatalog roomDecorationCatalog;
    [SerializeField] private RoomPrefabSpawnCatalog roomPrefabSpawnCatalog;
    [Tooltip("Spawned only in SportsRoom (not from ItemSpawnManager weighted list).")]
    [SerializeField] private GameObject sportsBatPickupPrefab;

    private bool[,] occupied;
    private GameObject[,] roomGrid;
    private GameObject _startingRoomInstance;

    private int bossX;
    private int bossY;

    private int centerX;
    private int centerY;

    private void Start()
    {
        centerX = maxCellCols / 2;
        centerY = maxCellRows / 2;

        ChooseBossLocation();

        GenerateMap();
        GenerateDoors();

        if (player != null)
        {
            player.position = MapToWorld(centerX, centerY);
        }

        if (_startingRoomInstance != null)
            StartCoroutine(ShowOnlyStartingRoomAfterFirstFrame());

        if (LootSpawnManager.Instance != null)
        {
            LootSpawnManager.Instance.RefreshSpawnAreas();
        }

        if (RunObjectiveManager.Instance != null)
        {
            RunObjectiveManager.Instance.GenerateShoppingListAndGoals();
            RunObjectiveManager.Instance.SpawnLootForCurrentObjective();
        }

        SpawnSportsRoomBatPickups();

        RoomContentActivation.RefreshPlayerRoomsAfterMapSetup();
        StartCoroutine(CoRoomContentActivationAfterFirstFrame());
    }

    private IEnumerator CoRoomContentActivationAfterFirstFrame()
    {
        yield return null;
        RoomContentActivation.RefreshPlayerRoomsAfterMapSetup();
    }

    // ==================== Room Spawning stuff ====================

    void ChooseBossLocation()
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = 0; x <= maxCellCols - bossRoom.cellWidth; x++)
        {
            candidates.Add(new Vector2Int(x, 0));
            candidates.Add(new Vector2Int(x, maxCellRows - bossRoom.cellHeight));
        }

        for (int y = 1; y < maxCellRows - 1; y++)
        {
            candidates.Add(new Vector2Int(0, y));
            candidates.Add(new Vector2Int(maxCellCols - bossRoom.cellWidth, y));
        }

        while (candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            Vector2Int pos = candidates[index];
            candidates.RemoveAt(index);

            bool overlapsSpawn =
                pos.x <= centerX && centerX < pos.x + bossRoom.cellWidth &&
                pos.y <= centerY && centerY < pos.y + bossRoom.cellHeight;

            if (!overlapsSpawn)
            {
                bossX = pos.x;
                bossY = pos.y;
                return;
            }
        }
    }

    void GenerateMap()
    {
        Room.ResetOneShotHintsForNewMap();
        occupied = new bool[maxCellCols, maxCellRows];
        roomGrid = new GameObject[maxCellCols, maxCellRows];

        // Spawn start room
        GameObject startRoomObj = Instantiate(startingRoomPrefab, MapToWorld(centerX, centerY), Quaternion.identity, transform);
        startRoomObj.AddComponent<StartingRoomSafeZone>();
        
        if (ItemSpawnManager.Instance != null)
        {
            ItemSpawnManager.Instance.RegisterRoom(startRoomObj, centerX, centerY, 1, 1);
        }
        
        occupied[centerX, centerY] = true;
        roomGrid[centerX, centerY] = startRoomObj;
        _startingRoomInstance = startRoomObj;
        ApplyRoomPresentation(startRoomObj);

        // Spawn boss room
        PlaceRoom(bossRoom, bossX, bossY);

        // Ensure we have at least one of each room type when possible.
        EnsureAtLeastOneRoomPerType();

        // Spawn other rooms
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int y = 0; y < maxCellRows; y++)
        {
            for (int x = 0; x < maxCellCols; x++)
            {
                // Skip cells already occupied
                if (occupied[x, y]) 
                    continue;

                positions.Add(new Vector2Int(x, y));
            }
        }

        Shuffle(positions);

        foreach (var pos in positions)
        {
            TryPlaceRandomRoom(pos.x, pos.y);
        }
    }

    private void EnsureAtLeastOneRoomPerType()
    {
        if (roomPrefabs == null || roomPrefabs.Length == 0)
            return;

        HashSet<RoomType> present = CollectPresentRoomTypes();

        List<Vector2Int> openPositions = new List<Vector2Int>();
        for (int y = 0; y < maxCellRows; y++)
        {
            for (int x = 0; x < maxCellCols; x++)
            {
                if (!occupied[x, y])
                    openPositions.Add(new Vector2Int(x, y));
            }
        }
        Shuffle(openPositions);

        foreach (RoomType rt in System.Enum.GetValues(typeof(RoomType)))
        {
            if (rt == RoomType.None || present.Contains(rt))
                continue;

            RoomPrefab forcedPrefab = PickRoomPrefabForType(rt);
            if (forcedPrefab == null)
                continue;

            bool placed = false;
            for (int i = 0; i < openPositions.Count; i++)
            {
                Vector2Int p = openPositions[i];
                if (!CanPlace(forcedPrefab, p.x, p.y))
                    continue;

                PlaceRoom(forcedPrefab, p.x, p.y);
                present.Add(rt);
                openPositions.RemoveAt(i);
                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning($"MapManager: Could not force-place room type {rt}; map bounds are full.");
            }
        }
    }

    private HashSet<RoomType> CollectPresentRoomTypes()
    {
        HashSet<RoomType> present = new HashSet<RoomType>();
        HashSet<GameObject> visited = new HashSet<GameObject>();

        for (int y = 0; y < maxCellRows; y++)
        {
            for (int x = 0; x < maxCellCols; x++)
            {
                GameObject go = roomGrid[x, y];
                if (go == null || !visited.Add(go))
                    continue;

                if (RoomLootSpawnTypeHelper.TryGetRoomType(go.transform, out RoomType rt))
                    present.Add(rt);
            }
        }

        return present;
    }

    private RoomPrefab PickRoomPrefabForType(RoomType targetType)
    {
        List<RoomPrefab> matches = new List<RoomPrefab>();
        List<RoomPrefab> candidates = BuildRoomPrefabCandidates();
        for (int i = 0; i < candidates.Count; i++)
        {
            RoomPrefab rp = candidates[i];
            if (rp == null || rp.prefab == null)
                continue;

            if (!RoomLootSpawnTypeHelper.TryGetRoomType(rp.prefab.transform, out RoomType prefabType))
                continue;
            if (prefabType != targetType)
                continue;
            matches.Add(rp);
        }

        if (matches.Count == 0)
            return null;
        return matches[Random.Range(0, matches.Count)];
    }

    void Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void TryPlaceRandomRoom(int x, int y)
    {
        List<RoomPrefab> candidates = new List<RoomPrefab>();

        List<RoomPrefab> allCandidates = BuildRoomPrefabCandidates();
        foreach (var room in allCandidates)
        {
            if (CanPlace(room, x, y))
                candidates.Add(room);
        }

        if (candidates.Count == 0) return;

        PlaceRoom(candidates[Random.Range(0, candidates.Count)], x, y);
    }

    bool CanPlace(RoomPrefab room, int x, int y)
    {
        if (room == null || room.prefab == null)
            return false;

        if (x + room.cellWidth > maxCellCols || y + room.cellHeight > maxCellRows)
            return false;

        for (int dx = 0; dx < room.cellWidth; dx++)
            for (int dy = 0; dy < room.cellHeight; dy++)
                if (occupied[x + dx, y + dy])
                    return false;

        return true;
    }

    private List<RoomPrefab> BuildRoomPrefabCandidates()
    {
        List<RoomPrefab> candidates = new List<RoomPrefab>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        if (roomPrefabs != null)
        {
            for (int i = 0; i < roomPrefabs.Length; i++)
            {
                RoomPrefab rp = roomPrefabs[i];
                if (rp == null || rp.prefab == null || !seen.Add(rp.prefab))
                    continue;
                candidates.Add(rp);
            }
        }

        if (startingRoomPrefab != null &&
            !seen.Contains(startingRoomPrefab) &&
            RoomLootSpawnTypeHelper.TryGetRoomType(startingRoomPrefab.transform, out RoomType startType) &&
            startType == RoomType.SportsRoom)
        {
            candidates.Add(new RoomPrefab
            {
                prefab = startingRoomPrefab,
                cellWidth = 1,
                cellHeight = 1
            });
        }

        return candidates;
    }

    void PlaceRoom(RoomPrefab room, int x, int y)
    {

        GameObject roomObj = Instantiate(room.prefab, MapToWorld(x, y), Quaternion.identity, transform);

        if (ItemSpawnManager.Instance != null)
        {
            ItemSpawnManager.Instance.RegisterRoom(roomObj, x, y, room.cellWidth, room.cellHeight);
        }

        for (int dx = 0; dx < room.cellWidth; dx++)
        {
            for (int dy = 0; dy < room.cellHeight; dy++)
            {
                occupied[x + dx, y + dy] = true;
                roomGrid[x + dx, y + dy] = roomObj;
            }
        }

        ApplyRoomPresentation(roomObj);
    }

    private IEnumerator ShowOnlyStartingRoomAfterFirstFrame()
    {
        yield return null;
        if (_startingRoomInstance == null)
            yield break;

        Room room = _startingRoomInstance.GetComponent<Room>();
        if (room != null)
            room.ApplyAsCurrentVisibleRoom();
    }

    private void ApplyRoomPresentation(GameObject roomInstance)
    {
        if (roomInstance == null)
            return;

        RoomPresentation presentation = roomInstance.GetComponent<RoomPresentation>();
        if (presentation == null)
            presentation = roomInstance.AddComponent<RoomPresentation>();

        presentation.Initialize(roomFloorTileSprite, roomDecorationCatalog, roomPrefabSpawnCatalog);
    }

    private void SpawnSportsRoomBatPickups()
    {
        if (sportsBatPickupPrefab == null || roomGrid == null)
            return;

        HashSet<GameObject> visitedRooms = new HashSet<GameObject>();
        for (int x = 0; x < maxCellCols; x++)
        {
            for (int y = 0; y < maxCellRows; y++)
            {
                GameObject roomObj = roomGrid[x, y];
                if (roomObj == null || !visitedRooms.Add(roomObj))
                    continue;
                SportsRoomBatPlacer.TrySpawnBat(roomObj, sportsBatPickupPrefab);
            }
        }
    }

    // ==================== Door Spawning stuff ====================

    void GenerateDoors()
    {
        GenerateInternalDoors();
        GenerateBoundaryDoors();
    }

    void GenerateInternalDoors()
    {
        for (int x = 0; x < maxCellCols; x++)
        {
            for (int y = 0; y < maxCellRows; y++)
            {
                if (!occupied[x, y]) continue;

                GameObject currentRoom = roomGrid[x, y];

                // RIGHT neighbor → Vertical door
                if (x < maxCellCols - 1 && occupied[x + 1, y])
                {
                    GameObject neighborRoom = roomGrid[x + 1, y];

                    // 🚨 KEY FIX: skip if same room
                    if (currentRoom != neighborRoom)
                    {
                        Vector3 pos = MapToWorld(x, y) + new Vector3(unitCellSize.x / 2f, 0, 0);

                        Door d = SpawnDoor(verticalDoorPrefab, pos, false);
                        d.Initialize(currentRoom, neighborRoom);
                    }
                }

                // TOP neighbor → Horizontal door
                if (y < maxCellRows - 1 && occupied[x, y + 1])
                {
                    GameObject neighborRoom = roomGrid[x, y + 1];

                    // 🚨 KEY FIX: skip if same room
                    if (currentRoom != neighborRoom)
                    {
                        Vector3 pos = MapToWorld(x, y) + new Vector3(0, unitCellSize.y / 2f, 0);

                        Door d = SpawnDoor(horizontalDoorPrefab, pos, true);
                        d.Initialize(currentRoom, neighborRoom);
                    }
                }
            }
        }
    }

    void GenerateBoundaryDoors()
    {
        for (int x = 0; x < maxCellCols; x++)
        {
            if (!occupied[x, maxCellRows - 1] || !occupied[x, 0])
                continue;

            Door top = SpawnDoor(horizontalBoundaryDoorPrefab,
                MapToWorld(x, maxCellRows - 1) + new Vector3(0, unitCellSize.y / 2f, 0), true);

            Door bottom = SpawnDoor(horizontalBoundaryDoorPrefab,
                MapToWorld(x, 0) + new Vector3(0, -unitCellSize.y / 2f, 0), true);

            top.Initialize(roomGrid[x, maxCellRows - 1], null);
            bottom.Initialize(roomGrid[x, 0], null);

            LinkDoors(top, bottom);
        }

        for (int y = 0; y < maxCellRows; y++)
        {
            if (!occupied[0, y] || !occupied[maxCellCols - 1, y])
                continue;

            Door left = SpawnDoor(verticalBoundaryDoorPrefab,
                MapToWorld(0, y) + new Vector3(-unitCellSize.x / 2f, 0, 0), false);

            Door right = SpawnDoor(verticalBoundaryDoorPrefab,
                MapToWorld(maxCellCols - 1, y) + new Vector3(unitCellSize.x / 2f, 0, 0), false);

            left.Initialize(roomGrid[0, y], null);
            right.Initialize(roomGrid[maxCellCols - 1, y], null);

            LinkDoors(left, right);
        }
    }

    Door SpawnDoor(GameObject prefab, Vector3 pos, bool isHorizontal)
    {
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);
        Door door = obj.GetComponent<Door>();

        if (door != null)
            door.isHorizontal = isHorizontal;

        return door;
    }

    void LinkDoors(Door a, Door b)
    {
        if (a == null || b == null) return;
        a.linkedDoor = b;
        b.linkedDoor = a;
    }

    // ==================== Utils ====================

    public Vector3 MapToWorld(int x, int y)
    {
        return new Vector3(x * unitCellSize.x, y * unitCellSize.y, 0f);
    }
}
