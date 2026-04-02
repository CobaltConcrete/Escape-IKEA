using UnityEngine;
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

    private bool[,] occupied;
    private GameObject[,] roomGrid;

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

        // ===== LOOT SYSTEM INIT =====
        if (LootSpawnManager.Instance != null)
        {
            LootSpawnManager.Instance.RefreshSpawnAreas();
        }

        if (RunObjectiveManager.Instance != null)
        {
            RunObjectiveManager.Instance.GenerateNewRunObjective();
        }
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
        occupied = new bool[maxCellCols, maxCellRows];
        roomGrid = new GameObject[maxCellCols, maxCellRows];

        // Spawn start room
        GameObject startRoomObj = Instantiate(startingRoomPrefab, MapToWorld(centerX, centerY), Quaternion.identity, transform);
        
        if (ItemSpawnManager.Instance != null)
        {
            ItemSpawnManager.Instance.RegisterRoom(startRoomObj, centerX, centerY, 1, 1);
        }
        
        occupied[centerX, centerY] = true;
        roomGrid[centerX, centerY] = startRoomObj;

        // Spawn boss room
        PlaceRoom(bossRoom, bossX, bossY);

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

        foreach (var room in roomPrefabs)
        {
            if (CanPlace(room, x, y))
                candidates.Add(room);
        }

        if (candidates.Count == 0) return;

        PlaceRoom(candidates[Random.Range(0, candidates.Count)], x, y);
    }

    bool CanPlace(RoomPrefab room, int x, int y)
    {
        if (x + room.cellWidth > maxCellCols || y + room.cellHeight > maxCellRows)
            return false;

        for (int dx = 0; dx < room.cellWidth; dx++)
            for (int dy = 0; dy < room.cellHeight; dy++)
                if (occupied[x + dx, y + dy])
                    return false;

        return true;
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