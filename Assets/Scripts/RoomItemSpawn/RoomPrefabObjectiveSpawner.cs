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
        // Bat spawning is handled directly by MapManager via sportsBatPickupPrefab so there is only one source of truth.
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
                if (IsWeaponPrefab(prefab))
                    obj.name = SportsRoomBatPlacer.BatInstanceName;
                obj.SetActive(false);
                StripLegacySpawnerPath(obj);
                EnsureInteractionComponent(obj, prefab, shoppingListKey);
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
            RoomDecorInteriorAnchor.InteriorTopLeft,
            new Vector3(1.05f + attempt * 0.08f, -1.05f, 0f));
        return new Vector2(world.x, world.y);
    }

    private static void EnsureInteractionComponent(GameObject obj, GameObject sourcePrefab, string shoppingListKey)
    {
        if (obj == null)
            return;

        RoomSpawnPrefabDefinition def = obj.GetComponent<RoomSpawnPrefabDefinition>();
        if (def == null && sourcePrefab != null)
            def = sourcePrefab.GetComponent<RoomSpawnPrefabDefinition>();

        if (def != null && def.spawnCategory == RoomSpawnCategory.Weapon)
        {
            RemoveRoomGeneratedPickup(obj);
            EnsureWeaponPickupComponent(obj);
            return;
        }

        if (string.IsNullOrWhiteSpace(shoppingListKey))
            return;

        if (obj.GetComponent<RoomGeneratedPickup>() == null)
            obj.AddComponent<RoomGeneratedPickup>();
    }

    private static bool IsWeaponPrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;

        RoomSpawnPrefabDefinition def = prefab.GetComponent<RoomSpawnPrefabDefinition>();
        return def != null && def.spawnCategory == RoomSpawnCategory.Weapon;
    }

    private static void EnsureWeaponPickupComponent(GameObject obj)
    {
        if (obj == null || HasBehaviourNamed(obj, "WeaponWorldPickup"))
            return;

        System.Type weaponPickupType = FindTypeByName("WeaponWorldPickup");
        if (weaponPickupType != null)
            obj.AddComponent(weaponPickupType);
    }

    private static void RemoveRoomGeneratedPickup(GameObject obj)
    {
        if (obj == null)
            return;

        RoomGeneratedPickup pickup = obj.GetComponent<RoomGeneratedPickup>();
        if (pickup != null)
            DestroyImmediate(pickup);
    }

    private static void StripLegacySpawnerPath(GameObject obj)
    {
        if (obj == null)
            return;

        ItemWorldSpawner[] spawners = obj.GetComponentsInChildren<ItemWorldSpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
                DestroyImmediate(spawners[i]);
        }
    }

    private static bool HasBehaviourNamed(GameObject obj, string typeName)
    {
        MonoBehaviour[] behaviours = obj.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && string.Equals(behaviour.GetType().Name, typeName, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static System.Type FindTypeByName(string typeName)
    {
        System.AppDomain domain = System.AppDomain.CurrentDomain;
        System.Reflection.Assembly[] assemblies = domain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            System.Type[] types;
            try
            {
                types = assemblies[i].GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            if (types == null)
                continue;

            for (int j = 0; j < types.Length; j++)
            {
                System.Type type = types[j];
                if (type != null && string.Equals(type.Name, typeName, System.StringComparison.Ordinal))
                    return type;
            }
        }

        return null;
    }

}
