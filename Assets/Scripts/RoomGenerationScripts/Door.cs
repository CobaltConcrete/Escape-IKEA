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
    [SerializeField] public bool isHorizontal = true;
    [SerializeField] public Door linkedDoor;

    [Header("Rendering")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool isOpen = false;
    private bool isMoving = false;
    private Transform player;

    // Connected rooms for rendering visibility ie only make door visible if either room it is in contact with is visible
    private GameObject roomA;
    private GameObject roomB;

    void Start()
    {
        closedPosition = transform.position;

        if (isHorizontal)
            openPosition = closedPosition + (-transform.right * openDistance);
        else
            openPosition = closedPosition + (-transform.up * openDistance);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= interactionRange)
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
        isOpen = !isOpen;
        isMoving = true;

        if (propagate && linkedDoor != null)
        {
            linkedDoor.ToggleDoor(false);
        }
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