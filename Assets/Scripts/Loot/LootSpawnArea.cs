using System.Collections.Generic;
using UnityEngine;

public class LootSpawnArea : MonoBehaviour
{
    [SerializeField] private RoomType roomType;
    [SerializeField] private int maxSpawnCount = 4;
    [SerializeField] private float areaWeight = 1f;

    [Header("Spawn Parent")]
    [SerializeField] private Transform spawnParent;

    private readonly List<BoxCollider2D> subAreas = new List<BoxCollider2D>();
    private int currentSpawnCount = 0;

    public RoomType RoomType => roomType;
    public int MaxSpawnCount => maxSpawnCount;
    public int CurrentSpawnCount => currentSpawnCount;
    public float AreaWeight => areaWeight;
    public Transform SpawnParent => spawnParent;

    private void Awake()
    {
        RefreshSubAreas();

        if (spawnParent == null)
        {
            Room room = GetComponentInParent<Room>();
            if (room != null)
            {
                Transform fallback = room.transform.Find("SpawnedLoots");
                if (fallback == null) fallback = room.transform.Find("SpawnedItems");
                if (fallback == null) fallback = room.transform.Find("SpawnedObjects");

                if (fallback == null)
                {
                    GameObject created = new GameObject("SpawnedLoots");
                    created.transform.SetParent(room.transform, false);
                    created.transform.localPosition = Vector3.zero;
                    created.transform.localRotation = Quaternion.identity;
                    created.transform.localScale = Vector3.one;
                    fallback = created.transform;
                }

                spawnParent = fallback;
            }
        }
    }

    [ContextMenu("Refresh Sub Areas")]
    public void RefreshSubAreas()
    {
        subAreas.Clear();

        BoxCollider2D[] colliders = GetComponentsInChildren<BoxCollider2D>(true);
        foreach (BoxCollider2D col in colliders)
        {
            if (col != null)
            {
                subAreas.Add(col);
            }
        }
    }

    public bool CanSpawn()
    {
        return currentSpawnCount < maxSpawnCount && subAreas.Count > 0;
    }

    public void RegisterSpawn()
    {
        currentSpawnCount++;
    }

    public void ResetSpawnCount()
    {
        currentSpawnCount = 0;
    }

    public Vector2 GetRandomPoint(Vector2 footprint)
    {
        if (subAreas.Count == 0)
        {
            return transform.position;
        }

        BoxCollider2D chosen = subAreas[Random.Range(0, subAreas.Count)];
        Bounds bounds = chosen.bounds;

        float halfWidth = footprint.x * 0.5f;
        float halfHeight = footprint.y * 0.5f;

        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        if (minX > maxX || minY > maxY)
        {
            return bounds.center;
        }

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);

        return new Vector2(x, y);
    }
}