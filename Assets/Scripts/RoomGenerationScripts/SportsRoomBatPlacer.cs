using UnityEngine;

/// <summary>
/// Spawns the bat weapon pickup in sports rooms (not part of the weighted ItemSpawnManager list).
/// </summary>
public static class SportsRoomBatPlacer
{
    public const string BatInstanceName = "SportsBatPickup";
    private const string ResourcesBatPath = "BatPickup";

    public static void TrySpawnBat(GameObject roomRoot, GameObject batSpawnerPrefab)
    {
        if (roomRoot == null)
            return;
        if (RoomPrefabObjectiveSpawner.Instance != null)
            return;

        if (!RoomLootSpawnTypeHelper.TryGetRoomType(roomRoot.transform, out RoomType roomType) ||
            roomType != RoomType.SportsRoom)
        {
            return;
        }

        GameObject prefab = batSpawnerPrefab;
        if (prefab == null)
            prefab = Resources.Load<GameObject>(ResourcesBatPath);

        if (prefab == null)
        {
            Debug.LogWarning(
                $"SportsRoomBatPlacer: Assign a bat pickup prefab on MapManager, or add Resources/{ResourcesBatPath}.prefab (WeaponWorldPickup + Bat ItemDefinition).",
                roomRoot);
            return;
        }

        Transform parent = roomRoot.transform.Find("SpawnedItems");
        if (parent == null)
            parent = roomRoot.transform;

        if (parent.Find(BatInstanceName) != null)
            return;

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = BatInstanceName;

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
}
