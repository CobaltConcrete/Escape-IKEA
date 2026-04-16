using System.Collections.Generic;
using UnityEngine;

public class RoomPrefabObjectiveSpawner : MonoBehaviour
{
    public static RoomPrefabObjectiveSpawner Instance { get; private set; }

    [SerializeField] private RoomPrefabSpawnCatalog prefabSpawnCatalog;
    [SerializeField] private LayerMask blockedLayerMask;
    [SerializeField] private int maxPointAttemptsPerSpawn = 12;
    [SerializeField] private Vector2 fallbackFootprint = new Vector2(0.8f, 0.8f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TrySpawnFromObjective(IReadOnlyList<ShoppingListEntry> shoppingList)
    {
        if (prefabSpawnCatalog == null)
            return false;

        LootSpawnArea[] areas = FindObjectsByType<LootSpawnArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (areas == null || areas.Length == 0)
            return false;

        Dictionary<string, int> roomKeySpawnCount = new Dictionary<string, int>();
        // Item-category loot is now placed by RoomPrefabStaticPlacer in every room regardless of objective.
        // Objective spawner is reserved for weapon-category entries (bat).
        SpawnWeapons(areas, roomKeySpawnCount);
        return true;
    }

    private void SpawnWeapons(LootSpawnArea[] areas, Dictionary<string, int> roomKeySpawnCount)
    {
        for (int i = 0; i < areas.Length; i++)
        {
            LootSpawnArea area = areas[i];
            if (area == null || area.RoomType != RoomType.SportsRoom)
                continue;
            List<GameObject> weapons = prefabSpawnCatalog.GetPrefabs(RoomType.SportsRoom, RoomSpawnCategory.Weapon);
            if (weapons.Count == 0)
            {
                Debug.LogWarning("RoomPrefabObjectiveSpawner: No SportsRoom weapon prefabs configured in prefab spawn catalog.");
                continue;
            }
            GameObject prefab = weapons[Random.Range(0, weapons.Count)];
            RoomSpawnPrefabDefinition def = prefab.GetComponent<RoomSpawnPrefabDefinition>();
            if (def == null)
                continue;
            TrySpawnOnePrefab(prefab, new[] { area }, roomKeySpawnCount, def.roomType, def.shoppingListKey, sideBias: true);
        }
    }

    private bool TrySpawnOnePrefab(
        GameObject prefab,
        LootSpawnArea[] areas,
        Dictionary<string, int> roomKeySpawnCount,
        RoomType roomType,
        string shoppingListKey,
        bool sideBias = false)
    {
        List<LootSpawnArea> validAreas = new List<LootSpawnArea>();
        for (int i = 0; i < areas.Length; i++)
        {
            LootSpawnArea area = areas[i];
            if (area == null || !area.CanSpawn() || area.RoomType != roomType)
                continue;
            validAreas.Add(area);
        }

        for (int i = validAreas.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (validAreas[i], validAreas[j]) = (validAreas[j], validAreas[i]);
        }

        for (int i = 0; i < validAreas.Count; i++)
        {
            LootSpawnArea area = validAreas[i];
            Room room = area.GetComponentInParent<Room>();
            string roomKey = $"{room?.GetInstanceID() ?? 0}:{shoppingListKey}";
            if (!string.IsNullOrWhiteSpace(shoppingListKey) && roomKeySpawnCount.ContainsKey(roomKey))
                continue;

            for (int attempt = 0; attempt < maxPointAttemptsPerSpawn; attempt++)
            {
                Vector2 footprint = fallbackFootprint;
                Vector2 point = sideBias
                    ? GetSportsSidePoint(room != null ? room.transform : area.transform, area, attempt)
                    : area.GetRandomPoint(footprint);
                if (Physics2D.OverlapBox(point, footprint, 0f, blockedLayerMask) != null)
                    continue;

                Transform parent = area.SpawnParent != null ? area.SpawnParent : area.transform;
                if (room != null)
                {
                    Transform spawnedItems = room.transform.Find("SpawnedItems");
                    if (spawnedItems != null)
                        parent = spawnedItems;
                }
                GameObject obj = Instantiate(prefab, point, Quaternion.identity, parent);
                obj.SetActive(false);
                EnsureInteractionComponent(obj, shoppingListKey);
                obj.SetActive(true);
                if (room != null)
                    room.RefreshRendererRegistry();
                RoomContentActivation.RefreshPlayerRoomsAfterMapSetup();
                area.RegisterSpawn();
                if (!string.IsNullOrWhiteSpace(shoppingListKey))
                    roomKeySpawnCount[roomKey] = 1;
                return true;
            }
        }

        return false;
    }

    private static Vector2 GetSportsSidePoint(Transform roomRoot, LootSpawnArea area, int attempt)
    {
        if (roomRoot == null)
            return area.GetRandomPoint(new Vector2(0.8f, 0.8f));

        Vector3 world = RoomDecorationPlacer.GetAnchoredWorldPosition(
            roomRoot,
            RoomDecorInteriorAnchor.InteriorMiddleLeft,
            new Vector3(0.7f + attempt * 0.05f, 0.2f, 0f));
        return new Vector2(world.x, world.y);
    }

    private static void EnsureInteractionComponent(GameObject obj, string shoppingListKey)
    {
        if (obj == null)
            return;
        if (string.IsNullOrWhiteSpace(shoppingListKey))
            return;
        if (obj.GetComponent<RoomGeneratedPickup>() == null)
            obj.AddComponent<RoomGeneratedPickup>();
    }

}
