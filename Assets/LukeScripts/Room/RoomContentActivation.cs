using UnityEngine;

public class RoomContentActivation : MonoBehaviour
{
    [Header("Room Detection")]
    [SerializeField] private Collider2D roomTrigger;

    [Header("Containers")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Transform itemContainer;

    private static RoomContentActivation currentActiveRoom;

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
        TryActivateIfPlayerAlreadyInside();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        ActivateThisRoom();
    }

    public void ActivateThisRoom()
    {
        if (currentActiveRoom != null && currentActiveRoom != this)
        {
            currentActiveRoom.SetRoomContentActive(false);
        }

        currentActiveRoom = this;
        SetRoomContentActive(true);
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

    private void TryActivateIfPlayerAlreadyInside()
    {
        if (roomTrigger == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        if (roomTrigger.bounds.Intersects(playerCol.bounds))
        {
            ActivateThisRoom();
        }
    }
}