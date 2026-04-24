using System.Collections.Generic;
using UnityEngine;

public class RoomContentActivation : MonoBehaviour
{
    [Header("Room Detection")]
    [SerializeField] private Collider2D roomTrigger;

    [Header("Containers")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Transform objectContainer;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private Transform lootContainer;

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

        if (objectContainer == null)
        {
            Transform t = transform.Find("SpawnedObjects");
            if (t != null)
                objectContainer = t;
        }

        if (itemContainer == null)
        {
            Transform t = transform.Find("SpawnedItems");
            if (t != null)
                itemContainer = t;
        }

        if (lootContainer == null)
        {
            Transform t = transform.Find("SpawnedLoots");
            if (t == null) t = transform.Find("SpawnedLoot");
            if (t != null)
                lootContainer = t;
        }
        if (enemyContainer == null)
        {
            Transform t = transform.Find("SpawnedEnemies");
            if (t == null) t = transform.Find("SpawnedObjects");
            if (t != null)
                enemyContainer = t;
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

        if (playerRooms.Count == 0)
            RebuildPlayerRoomsFromCurrentPosition();

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
        {
            if (currentActiveRoom != null)
                currentActiveRoom.SetRoomContentActive(true);
            return;
        }

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

    private static void RebuildPlayerRoomsFromCurrentPosition()
    {
        if (playerTransform == null)
            return;

        RoomContentActivation[] rooms = Object.FindObjectsByType<RoomContentActivation>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        RoomContentActivation nearestRoom = null;
        float nearestDistance = float.MaxValue;
        Vector2 playerPos = playerTransform.position;

        foreach (RoomContentActivation room in rooms)
        {
            if (room == null || room.roomTrigger == null || room.ignoreRoomActivationInThisScene)
                continue;

            Collider2D trigger = room.roomTrigger;
            if (trigger.OverlapPoint(playerPos))
            {
                playerRooms.Add(room);
                continue;
            }

            Vector2 closest = trigger.ClosestPoint(playerPos);
            float distance = (playerPos - closest).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestRoom = room;
            }
        }

        // Door seams can briefly put the player outside every room trigger. Keep the closest
        // room alive instead of dropping to a fully black view.
        if (playerRooms.Count == 0 && nearestRoom != null && nearestDistance <= 4f)
            playerRooms.Add(nearestRoom);
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
        SetContainerItemsActive(objectContainer, active);
        SetContainerItemsActive(itemContainer, active);
        SetContainerItemsActive(lootContainer, active);
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

        if (!active)
            return;

        Enemy[] enemies = enemyContainer.GetComponentsInChildren<Enemy>(true);
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
                enemy.NotifyRoomActivated();
        }
    }

    private void SetContainerItemsActive(Transform container, bool active)
    {
        if (container == null)
            return;

        ItemWorld[] items = container.GetComponentsInChildren<ItemWorld>(true);
        foreach (ItemWorld item in items)
        {
            if (item != null)
            {
                item.SetRoomVisible(active);
            }
        }

        MonoBehaviour[] behaviours = container.GetComponentsInChildren<MonoBehaviour>(true);
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

        RoomGeneratedPickup[] generated = container.GetComponentsInChildren<RoomGeneratedPickup>(true);
        for (int i = 0; i < generated.Length; i++)
        {
            if (generated[i] == null)
                continue;
            generated[i].SetRoomVisible(active);
        }

        container.gameObject.SetActive(active);
    }
}
