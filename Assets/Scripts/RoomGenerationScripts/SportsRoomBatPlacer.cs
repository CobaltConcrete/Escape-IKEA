using UnityEngine;

/// <summary>
/// Spawns the sports-room bat from the single item-prefab source.
/// </summary>
public static class SportsRoomBatPlacer
{
    public const string BatInstanceName = "SportsBatPickup";

    public static void TrySpawnBat(GameObject roomRoot, GameObject batSpawnerPrefab)
    {
        if (roomRoot == null)
            return;

        if (!RoomLootSpawnTypeHelper.TryGetRoomType(roomRoot.transform, out RoomType roomType) ||
            roomType != RoomType.SportsRoom)
        {
            return;
        }

        if (batSpawnerPrefab == null || batSpawnerPrefab.GetComponent<ItemWorldSpawner>() == null)
        {
            Debug.LogWarning(
                "SportsRoomBatPlacer: Assign the single bat item prefab with ItemWorldSpawner on MapManager.",
                roomRoot);
            return;
        }

        Transform parent = roomRoot.transform.Find("SpawnedItems");
        if (parent == null)
            parent = roomRoot.transform;

        if (parent.Find(BatInstanceName) != null)
            return;

        ItemWorldSpawner prefabSpawner = batSpawnerPrefab.GetComponent<ItemWorldSpawner>();
        ItemDefinition batDefinition = prefabSpawner != null ? prefabSpawner.ItemDefinition : null;

        GameObject instance = Object.Instantiate(batSpawnerPrefab, parent);
        instance.name = BatInstanceName;
        ItemWorldSpawner spawner = instance.GetComponent<ItemWorldSpawner>();
        if (spawner != null)
            Object.DestroyImmediate(spawner);

        MonoBehaviour pickup = GetOrAddWeaponPickup(instance);
        ConfigureWeaponPickup(pickup, batDefinition, 1);

        SportsRoomBatAutoPickup autoPickup = instance.GetComponent<SportsRoomBatAutoPickup>();
        if (autoPickup == null)
            autoPickup = instance.AddComponent<SportsRoomBatAutoPickup>();
        autoPickup.Configure(batDefinition, 1);

        // Keep prefab-authored transform scale as authoritative for bat size.
        // (Do not overwrite with ItemDefinition worldDropScale.)

        Vector3 batWorld = RoomDecorationPlacer.GetAnchoredWorldPosition(
            roomRoot.transform,
            RoomDecorInteriorAnchor.InteriorMiddleLeft,
            new Vector3(0.35f, -0.18f, 0f));
        batWorld = ResolveNonOverlappingSportsBatPosition(roomRoot.transform, batWorld);
        instance.transform.position = batWorld;
        instance.transform.localRotation = Quaternion.identity;

        Room room = roomRoot.GetComponent<Room>();
        room?.RefreshRendererRegistry();
    }

    private static Vector3 ResolveNonOverlappingSportsBatPosition(Transform roomRoot, Vector3 preferredWorld)
    {
        Vector3[] candidates = new Vector3[]
        {
            preferredWorld,
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomLeft, new Vector3(0.52f, 0.36f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(0.52f, -0.36f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorMiddleRight, new Vector3(-0.52f, -0.18f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomRight, new Vector3(-0.72f, 0.42f, 0f))
        };

        const float overlapProbeRadius = 0.36f;
        for (int i = 0; i < candidates.Length; i++)
        {
            Collider2D hit = Physics2D.OverlapCircle(candidates[i], overlapProbeRadius);
            if (hit == null)
                return candidates[i];
        }

        return preferredWorld;
    }

    private static MonoBehaviour GetOrAddWeaponPickup(GameObject instance)
    {
        if (instance == null)
            return null;

        MonoBehaviour[] behaviours = instance.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && string.Equals(behaviour.GetType().Name, "WeaponWorldPickup", System.StringComparison.Ordinal))
                return behaviour;
        }

        System.Type weaponPickupType = FindTypeByName("WeaponWorldPickup");
        if (weaponPickupType == null)
        {
            Debug.LogError("SportsRoomBatPlacer: WeaponWorldPickup type could not be found.");
            return null;
        }

        return instance.AddComponent(weaponPickupType) as MonoBehaviour;
    }

    private static void ConfigureWeaponPickup(MonoBehaviour pickup, ItemDefinition batDefinition, int amount)
    {
        if (pickup == null)
            return;

        System.Reflection.MethodInfo configure = pickup.GetType().GetMethod(
            "Configure",
            new[] { typeof(ItemDefinition), typeof(int) });
        if (configure != null)
            configure.Invoke(pickup, new object[] { batDefinition, amount });
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
                if (types[j] != null && string.Equals(types[j].Name, typeName, System.StringComparison.Ordinal))
                    return types[j];
            }
        }

        return null;
    }
}
