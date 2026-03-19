using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RoomPrefab
{
    public GameObject prefab;
    
    [Tooltip("Number of grid cells this room occupies horizontally")]
    public int cellWidth = 1;

    [Tooltip("Number of grid cells this room occupies vertically")]
    public int cellHeight = 1;
}

public class MapManager : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField]
    [Tooltip("Max number of unit cells in a row")]
    public int maxCellRows = 5;
    
    [SerializeField]
    [Tooltip("Max number of unit cells in a column")]
    public int maxCellCols = 5;

    [SerializeField]
    [Tooltip("Actual size of each unit cell")]
    public Vector2 unitCellSize = new Vector2(10f, 10f);

    [Header("Room Prefabs")]
    [SerializeField]
    private RoomPrefab[] roomPrefabs;

    [SerializeField]
    private GameObject startingRoomPrefab;

    [SerializeField]
    private RoomPrefab bossRoom;

    [Header("Boundary Rooms")]
    [SerializeField]
    private GameObject[] boundaryPrefabs;

    [Header("Player")]
    [SerializeField]
    private Transform player;

    private bool[,] occupied;

    // Boss X and Y coordinates (of the center of its left unit cell)
    private int bossX;
    private int bossY;

    // Center coordinates for the entire map
    private int centerX;   
    private int centerY;

    private void Start()
    {
        centerX = maxCellCols / 2;
        centerY = maxCellRows / 2;

        ChooseBossLocation();
        GenerateMap();

        if (player != null)
        {
            player.position = MapToWorld(maxCellCols / 2, maxCellRows / 2);
        }
    }

    // Boss Location (size = 2 cells x 1 cell) will be at the edge of the map. Its coordinate is the center of its left unit cell
    void ChooseBossLocation()
    {
        // Initialize possible candidates for boss spawn
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = 0; x <= maxCellCols - bossRoom.cellWidth; x++)
        {
            candidates.Add(new Vector2Int(x, 0));                                   // touch left boundary
            candidates.Add(new Vector2Int(x, maxCellRows - bossRoom.cellHeight));   // touch right boundary
        }

        for (int y = 1; y < maxCellRows - 1; y++)
        {
            candidates.Add(new Vector2Int(0, y));                                   // touch top boundary
            candidates.Add(new Vector2Int(maxCellCols - bossRoom.cellWidth, y));    // touch bottom boundary
        }

        while (candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);  // randomly choose candidates
            Vector2Int pos = candidates[index];
            candidates.RemoveAt(index);                     // eliminate from candidates list in case its reject

            // Check is boss room overlaps spawn
            bool overlapsSpawn =
                pos.x <= centerX && centerX < pos.x + bossRoom.cellWidth &&
                pos.y <= centerY && centerY < pos.y + bossRoom.cellHeight;

            // Accept if not overlap spawn
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
        // 2D map showing which cells occupied already
        occupied = new bool[maxCellCols, maxCellRows];

        // Place boss room first so its cells are marked occupied
        PlaceRoom(bossRoom, bossX, bossY);

        // Create a list of all cell positions
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int y = 0; y < maxCellRows; y++)
            for (int x = 0; x < maxCellCols; x++)
                positions.Add(new Vector2Int(x, y));

        // Shuffle positions
        for (int i = 0; i < positions.Count; i++)
        {
            int j = Random.Range(i, positions.Count);
            var temp = positions[i];
            positions[i] = positions[j];
            positions[j] = temp;
        }

        // Place rooms in shuffled order
        foreach (var pos in positions)
        {
            int x = pos.x;
            int y = pos.y;

            if (occupied[x, y])
                continue;

            //if (x == centerX && y == centerY)
            //{
            //    Instantiate(startingRoomPrefab, MapToWorld(x, y), Quaternion.identity, transform);
            //    occupied[x, y] = true;
            //    continue;
            //}

            if (x == centerX && y == centerY)
            {
                GameObject startRoomObj = Instantiate(startingRoomPrefab, MapToWorld(x, y), Quaternion.identity, transform);

                if (ItemSpawnManager.Instance != null)
                {
                    ItemSpawnManager.Instance.RegisterRoom(startRoomObj, x, y, 1, 1);
                }

                occupied[x, y] = true;
                continue;
            }

            TryPlaceRandomRoom(x, y);
        }

        // GenerateBoundary();
    }

    void TryPlaceRandomRoom(int x, int y)
    {
        // Generate a list of candidate rooms that can be places at (x,y) without overlapping other rooms / boundaries
        List<RoomPrefab> candidateRooms = new List<RoomPrefab>();

        foreach (var room in roomPrefabs)
        {
            if (CanPlace(room, x, y))
                candidateRooms.Add(room);
        }

        if (candidateRooms.Count == 0)
            return;

        // Randomly select a room from one of these candidates
        RoomPrefab selectedRoom = candidateRooms[Random.Range(0, candidateRooms.Count)];
        PlaceRoom(selectedRoom, x, y);
    }

    bool OverlapsCenter(RoomPrefab room, int x, int y)
    {
        // Check if this room overlaps spawnpoint (map center)
        return x <= centerX && centerX < x + room.cellWidth &&
            y <= centerY && centerY < y + room.cellHeight;
    }

    bool CanPlace(RoomPrefab room, int x, int y)
    {
        // Bounds check
        if (x + room.cellWidth > maxCellCols || y + room.cellHeight > maxCellRows)
            return false;

        // Occupancy check
        for (int dx = 0; dx < room.cellWidth; dx++)
        {
            for (int dy = 0; dy < room.cellHeight; dy++)
            {
                if (occupied[x + dx, y + dy])
                    return false;
            }
        }

        // Prevent encroaching on center spawn
        if (OverlapsCenter(room, x, y))
            return false;

        return true;
    }

    void PlaceRoom(RoomPrefab room, int x, int y)
    {
        //Instantiate(room.prefab, MapToWorld(x, y), Quaternion.identity, transform);

        //for (int dx = 0; dx < room.cellWidth; dx++)
        //{
        //    for (int dy = 0; dy < room.cellHeight; dy++)
        //    {
        //        occupied[x + dx, y + dy] = true;
        //    }
        //}
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
            }
        }
    }

    // void GenerateBoundary()
    // {
    //     for (int x = -1; x <= maxCellCols; x++)
    //     {
    //         SpawnBoundary(x, -1);
    //         SpawnBoundary(x, maxCellRows);
    //     }

    //     for (int y = 0; y < maxCellRows; y++)
    //     {
    //         SpawnBoundary(-1, y);
    //         SpawnBoundary(maxCellCols, y);
    //     }
    // }

    // void SpawnBoundary(int x, int y)
    // {
    //     if (boundaryPrefabs == null || boundaryPrefabs.Length == 0)
    //         return;

    //     GameObject prefab = boundaryPrefabs[Random.Range(0, boundaryPrefabs.Length)];
    //     Instantiate(prefab, MapToWorld(x, y), Quaternion.identity, transform);
    // }

    // Map coordinates -> World coordinates
    public Vector3 MapToWorld(int x, int y)
    {
        return new Vector3(x * unitCellSize.x, y * unitCellSize.y, 0f);
    }
}
