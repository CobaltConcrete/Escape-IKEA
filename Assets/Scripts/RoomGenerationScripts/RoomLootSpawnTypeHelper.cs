using UnityEngine;

/// <summary>
/// Resolves a room's <see cref="RoomType"/> from the first <see cref="LootSpawnArea"/> under the room root.
/// Shared by decoration placement and item spawn filtering.
/// </summary>
public static class RoomLootSpawnTypeHelper
{
    public static bool TryGetRoomType(Transform roomRoot, out RoomType roomType)
    {
        roomType = RoomType.None;

        if (roomRoot == null)
            return false;

        LootSpawnArea area = roomRoot.GetComponentInChildren<LootSpawnArea>(true);
        if (area == null)
            return false;

        roomType = area.RoomType;
        return true;
    }
}
