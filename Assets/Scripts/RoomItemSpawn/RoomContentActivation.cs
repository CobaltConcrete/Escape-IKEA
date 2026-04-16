using System.Collections.Generic;
using UnityEngine;

public class RoomContentActivation : MonoBehaviour
{
    [Header("Room Detection")]
    [SerializeField] private Collider2D roomTrigger;

    [Header("Containers")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Transform itemContainer;

    [Header("Debug / Test Scene Override")]
    [SerializeField]
    [Tooltip("If enabled, this room ignores room detection and keeps its contents active for test scenes.")]
    private bool ignoreRoomActivationInThisScene = false;

    private static RoomContentActivation currentActiveRoom;
    private static readonly HashSet<RoomContentActivation> playerRooms = new HashSet<RoomContentActivation>();
    private static Transform playerTransform;

    private void Awake()
    {
        if (roomTrigger == null)
        {
            roomTrigger = GetComponent<Collider2D>();
        }

        if (roomTrigger != null)
        {
            roomTrigger.isTrigger = true;
        }
    }

    private void Start()
    {
        if (ignoreRoomActivationInThisScene)
        {
            SetRoomContentActive(true);
            return;
        }

        SetRoomContentActive(false);
        RegisterIfPlayerInsideVolume();
        ReevaluateActiveRoom();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ignoreRoomActivationInThisScene)
            return;

        if (!collision.CompareTag("Player"))
            return;

        playerTransform = collision.transform;
        playerRooms.Add(this);
        ReevaluateActiveRoom();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (ignoreRoomActivationInThisScene)
            return;

        if (!collision.CompareTag("Player"))
            return;

        playerTransform = collision.transform;
        playerRooms.Add(this);
        ReevaluateActiveRoom();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (ignoreRoomActivationInThisScene)
            return;

        if (!collision.CompareTag("Player"))
            return;

        playerRooms.Remove(this);

        if (currentActiveRoom == this)
        {
            currentActiveRoom.SetRoomContentActive(false);
            currentActiveRoom = null;
        }

        ReevaluateActiveRoom();
    }

    /// <summary>
    /// Adds this room to <see cref="playerRooms"/> when the player is already overlapping the room trigger
    /// (e.g. starting room after teleport). Uses bounds + trigger point tests so <see cref="SpawnedItems"/>
    /// is not left inactive (which would disable loot / bat colliders only in that room).
    /// </summary>
    public void RegisterIfPlayerInsideVolume()
    {
        if (roomTrigger == null || ignoreRoomActivationInThisScene)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null)
        {
            return;
        }

        Bounds pb = playerCol.bounds;
        Vector2 center = pb.center;
        Vector2 feet = player.transform.position;

        if (roomTrigger.bounds.Intersects(pb) ||
            roomTrigger.OverlapPoint(center) ||
            roomTrigger.OverlapPoint(feet))
        {
            playerTransform = player.transform;
            playerRooms.Add(this);
        }
    }

    /// <summary>
    /// Rebuilds which room owns active pickups after map generation / loot spawn / player teleport.
    /// Call from <see cref="MapManager"/> once the player is at their start cell so the starting room
    /// (often Sports) does not keep <see cref="itemContainer"/> disabled.
    /// </summary>
    public static void RefreshPlayerRoomsAfterMapSetup()
    {
        playerRooms.Clear();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        playerTransform = player.transform;

        RoomContentActivation[] rooms = Object.FindObjectsByType<RoomContentActivation>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (RoomContentActivation room in rooms)
        {
            if (room == null)
            {
                continue;
            }

            room.RegisterIfPlayerInsideVolume();
        }

        ReevaluateActiveRoom();
    }

    private static void ReevaluateActiveRoom()
    {
        if (playerTransform == null)
            return;

        RoomContentActivation bestRoom = null;
        float bestDistance = float.MaxValue;

        foreach (RoomContentActivation room in playerRooms)
        {
            if (room == null || room.roomTrigger == null)
                continue;

            if (room.ignoreRoomActivationInThisScene)
                continue;

            Vector2 closestPoint = room.roomTrigger.ClosestPoint(playerTransform.position);
            float dist = ((Vector2)playerTransform.position - closestPoint).sqrMagnitude;

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestRoom = room;
            }
        }

        if (bestRoom == currentActiveRoom)
            return;

        if (currentActiveRoom != null)
        {
            currentActiveRoom.SetRoomContentActive(false);
        }

        currentActiveRoom = bestRoom;

        if (currentActiveRoom != null)
        {
            currentActiveRoom.OnPlayerEnteredThisRoom();
            currentActiveRoom.SetRoomContentActive(true);
        }
    }

    private void OnPlayerEnteredThisRoom()
    {
        RoomVisitTracker.Instance?.RegisterRoomVisit(GetRoomKey());

        RoomEnemyRespawnAnchor respawnAnchor = GetComponentInChildren<RoomEnemyRespawnAnchor>();
        if (respawnAnchor != null)
        {
            respawnAnchor.OnPlayerEnteredRoom();
        }
        else
        {
            Debug.LogWarning($"[RoomContentActivation] No RoomEnemyRespawnAnchor found in {name}");
        }
    }

    private string GetRoomKey()
    {
        return gameObject.GetInstanceID().ToString();
    }

    public void SetRoomContentActive(bool active)
    {
        SetEnemiesActive(active);
        SetItemsActive(active);
    }

    private void SetEnemiesActive(bool active)
    {
        if (enemyContainer == null)
            return;

        RoomContentVisibility[] contents = enemyContainer.GetComponentsInChildren<RoomContentVisibility>(true);
        foreach (RoomContentVisibility content in contents)
        {
            if (content != null)
            {
                content.SetActiveInRoom(active);
            }
        }
    }

    private void SetItemsActive(bool active)
    {
        if (itemContainer == null)
            return;

        ItemWorld[] items = itemContainer.GetComponentsInChildren<ItemWorld>(true);
        foreach (ItemWorld item in items)
        {
            if (item != null)
            {
                item.SetRoomVisible(active);
            }
        }

        // Avoid direct compile-time dependency on WeaponWorldPickup (can fail on some script compile orders).
        MonoBehaviour[] behaviours = itemContainer.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;
            if (!string.Equals(behaviour.GetType().Name, "WeaponWorldPickup", System.StringComparison.Ordinal))
                continue;

            var method = behaviour.GetType().GetMethod("SetRoomVisible", new[] { typeof(bool) });
            if (method != null)
                method.Invoke(behaviour, new object[] { active });
        }

        RoomGeneratedPickup[] generated = itemContainer.GetComponentsInChildren<RoomGeneratedPickup>(true);
        for (int i = 0; i < generated.Length; i++)
        {
            if (generated[i] == null)
                continue;
            generated[i].SetRoomVisible(active);
        }

        itemContainer.gameObject.SetActive(active);
    }
}