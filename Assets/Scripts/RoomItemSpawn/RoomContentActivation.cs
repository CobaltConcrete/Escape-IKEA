using System.Collections.Generic;
using UnityEngine;

public class RoomContentActivation : MonoBehaviour
{
    [Header("Room Detection")]
    [SerializeField] private Collider2D roomTrigger;

    [Header("Containers")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Transform itemContainer;

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
        SetRoomContentActive(false);
        TryRegisterIfPlayerAlreadyInside();
        ReevaluateActiveRoom();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerTransform = collision.transform;
        playerRooms.Add(this);
        ReevaluateActiveRoom();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerTransform = collision.transform;
        playerRooms.Add(this);
        ReevaluateActiveRoom();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerRooms.Remove(this);

        if (currentActiveRoom == this)
        {
            currentActiveRoom.SetRoomContentActive(false);
            currentActiveRoom = null;
        }

        ReevaluateActiveRoom();
    }

    private void TryRegisterIfPlayerAlreadyInside()
    {
        if (roomTrigger == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        if (roomTrigger.bounds.Intersects(playerCol.bounds))
        {
            playerTransform = player.transform;
            playerRooms.Add(this);
        }
    }

    private static void ReevaluateActiveRoom()
    {
        if (playerTransform == null) return;

        RoomContentActivation bestRoom = null;
        float bestDistance = float.MaxValue;

        foreach (RoomContentActivation room in playerRooms)
        {
            if (room == null || room.roomTrigger == null) continue;

            Vector2 closestPoint = room.roomTrigger.ClosestPoint(playerTransform.position);
            float dist = ((Vector2)playerTransform.position - closestPoint).sqrMagnitude;

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestRoom = room;
            }
        }

        if (bestRoom == currentActiveRoom) return;

        if (currentActiveRoom != null)
        {
            currentActiveRoom.SetRoomContentActive(false);
        }

        currentActiveRoom = bestRoom;

        if (currentActiveRoom != null)
        {
            currentActiveRoom.SetRoomContentActive(true);
        }
    }

    public void SetRoomContentActive(bool active)
    {
        SetEnemiesActive(active);
        SetItemsActive(active);
    }

    private void SetEnemiesActive(bool active)
    {
        if (enemyContainer == null) return;

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
        if (itemContainer != null)
        {
            itemContainer.gameObject.SetActive(active);
        }
    }
}