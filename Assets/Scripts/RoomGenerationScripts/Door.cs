using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] public float openDistance = 2f;
    [SerializeField] public float speed = 3f;

    [Header("Interaction")]
    [SerializeField] public KeyCode interactKey = KeyCode.F;
    [SerializeField] public float interactionRange = 2f;

    [Header("Door Behavior")]
    [SerializeField] public bool isHorizontal = true; // determines slide direction
    [SerializeField] public Door linkedDoor;          // for boundary syncing
    [SerializeField] private bool isLocked = false;

    [Header("Rendering")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 interactionPoint; // fixed point for interaction

    private bool isOpen = false;
    private bool isMoving = false;
    private Transform player;

    // Connected rooms for rendering visibility
    private GameObject roomA;
    private GameObject roomB;

    void Start()
    {
        closedPosition = transform.position;

        // Set the open position based on door orientation
        if (isHorizontal)
            openPosition = closedPosition + (-transform.right * openDistance);
        else
            openPosition = closedPosition + (-transform.up * openDistance);

        // Store the initial doorway position for interaction
        interactionPoint = closedPosition;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null) return;

        float distToDoorway = Vector3.Distance(player.position, interactionPoint);
        float distToPanel = Vector3.Distance(player.position, transform.position);
        float dist = Mathf.Min(distToDoorway, distToPanel);

        if (!isLocked && dist <= interactionRange)
        {
            if (Input.GetKeyDown(interactKey) && !isMoving)
            {
                ToggleDoor(true);
            }
        }

        MoveDoor();
        UpdateVisibility();
    }

    public void Initialize(GameObject a, GameObject b)
    {
        roomA = a;
        roomB = b;
    }

    void UpdateVisibility()
    {
        if (spriteRenderer == null) return;

        bool visibleA = IsRoomVisible(roomA);
        bool visibleB = IsRoomVisible(roomB);

        spriteRenderer.enabled = visibleA || visibleB;
    }

    bool IsRoomVisible(GameObject room)
    {
        if (room == null) return false;

        SpriteRenderer[] renderers = room.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            if (sr.enabled)
                return true;
        }

        return false;
    }

    public void ToggleDoor(bool propagate)
    {
        if (isLocked) return;
        isOpen = !isOpen;
        isMoving = true;

        if (propagate && linkedDoor != null)
        {
            linkedDoor.ToggleDoor(false);
        }
    }

    public void SetLocked(bool locked, bool propagate = true)
    {
        isLocked = locked;
        if (locked)
        {
            isOpen = false;
        }
        isMoving = false;
        transform.position = closedPosition;

        if (propagate && linkedDoor != null)
        {
            linkedDoor.SetLocked(locked, false);
        }
    }

    public bool IsConnectedToRoom(GameObject room)
    {
        if (room == null) return false;
        return roomA == room || roomB == room;
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    void MoveDoor()
    {
        Vector3 target = isOpen ? openPosition : closedPosition;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            transform.position = target;
            isMoving = false;
        }
    }
}