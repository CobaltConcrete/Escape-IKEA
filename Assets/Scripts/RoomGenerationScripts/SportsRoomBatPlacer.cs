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
        // Bat sits next to the center-right baseball spawn with fixed spacing (no overlap).
        Vector3 batWorld = RoomDecorationPlacer.GetAnchoredWorldPosition(
            roomRoot.transform,
            RoomDecorInteriorAnchor.InteriorBottomRight,
            new Vector3(-0.56f, 0.2f, 0f));
        instance.transform.position = batWorld;
        instance.transform.localRotation = Quaternion.identity;

        Room room = roomRoot.GetComponent<Room>();
        room?.RefreshRendererRegistry();
    }
}
