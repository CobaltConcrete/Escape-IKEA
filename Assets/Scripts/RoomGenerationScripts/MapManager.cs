using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField]
    [Tooltip("Maximum number of rooms in a row.")]
    public int maxRows = 5;

    [SerializeField]
    [Tooltip("Maximum number of rooms in a column.")]
    public int maxCols = 5;

    [SerializeField]
    [Tooltip("Size of each 10x10 grid cell.")]
    public Vector2 roomSize = new Vector2(10f, 10f);

    [Header("Room Prefabs")]
    [SerializeField]
    [Tooltip("10x10 room prefabs")]
    private GameObject[] roomPrefabs10x10;

    [SerializeField]
    [Tooltip("20x10 horizontal room prefabs")]
    private GameObject[] roomPrefabs20x10;

    [SerializeField]
    [Tooltip("Starting room prefab (10x10)")]
    private GameObject startingRoomPrefab;

    [Header("Boundary Rooms")]
    [SerializeField]
    [Tooltip("Boundary room prefabs")]
    private GameObject[] boundaryPrefabs;

    [Header("Player")]
    [SerializeField]
    private Transform player;

    private bool[,] occupied;

    private void Start()
    {
        GenerateMap();

        if (player != null)
        {
            Vector3 startPos = MapToWorld(maxCols / 2, maxRows / 2);
            player.position = startPos;
        }
    }

    private void GenerateMap()
    {
        occupied = new bool[maxCols, maxRows];

        int centerX = maxCols / 2;
        int centerY = maxRows / 2;

        for (int y = 0; y < maxRows; y++)
        {
            for (int x = 0; x < maxCols; x++)
            {
                if (occupied[x, y])
                    continue;

                GameObject prefab;
                int width = 1;

                if (x == centerX && y == centerY)
                {
                    prefab = startingRoomPrefab;
                }
                else
                {
                    bool canPlaceWide =
                        x < maxCols - 1 &&
                        !occupied[x + 1, y] &&
                        !(x + 1 == centerX && y == centerY);

                    bool spawnWide = canPlaceWide && Random.value < 0.35f;

                    if (spawnWide && roomPrefabs20x10.Length > 0)
                    {
                        prefab = roomPrefabs20x10[Random.Range(0, roomPrefabs20x10.Length)];
                        width = 2;
                    }
                    else
                    {
                        prefab = roomPrefabs10x10[Random.Range(0, roomPrefabs10x10.Length)];
                        width = 1;
                    }
                }

                Vector3 worldPos = MapToWorld(x, y);
                Instantiate(prefab, worldPos, Quaternion.identity, transform);

                for (int w = 0; w < width; w++)
                {
                    occupied[x + w, y] = true;
                }
            }
        }

        GenerateBoundary();
    }

    private void GenerateBoundary()
    {
        for (int x = -1; x <= maxCols; x++)
        {
            SpawnBoundary(x, -1);
            SpawnBoundary(x, maxRows);
        }

        for (int y = 0; y < maxRows; y++)
        {
            SpawnBoundary(-1, y);
            SpawnBoundary(maxCols, y);
        }
    }

    private void SpawnBoundary(int x, int y)
    {
        if (boundaryPrefabs == null || boundaryPrefabs.Length == 0)
            return;

        GameObject prefab = boundaryPrefabs[Random.Range(0, boundaryPrefabs.Length)];
        Vector3 pos = MapToWorld(x, y);

        Instantiate(prefab, pos, Quaternion.identity, transform);
    }

    private Vector3 MapToWorld(int x, int y)
    {
        return new Vector3(x * roomSize.x, y * roomSize.y, 0f);
    }
}
