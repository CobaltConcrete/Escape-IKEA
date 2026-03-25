using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyWander : MonoBehaviour
{
    [Header("Wander Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float changeDirectionInterval = 2f;

    [Header("Room Bounds")]
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;
    [SerializeField] private float boundsPadding = 0.15f;

    [Header("Bounce Behavior")]
    [SerializeField] private float playerBounceDistance = 0.7f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float changeDirectionTimer;
    private Transform playerTransform;

    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        PickNewDirection();
        changeDirectionTimer = changeDirectionInterval;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (!CanMove) return;

        changeDirectionTimer -= Time.deltaTime;

        if (IsTooCloseToPlayer())
        {
            PickDirectionAwayFromPlayer();
        }
        else if (changeDirectionTimer <= 0f)
        {
            PickNewDirection();
            changeDirectionTimer = changeDirectionInterval;
        }

        KeepInsideBounds();
    }

    private void FixedUpdate()
    {
        if (!CanMove) return;

        Vector2 pos = rb.position;
        Vector2 next = pos + moveDirection * (moveSpeed * Time.fixedDeltaTime);

        if (WouldLeaveBounds(next))
        {
            RedirectInsideBounds(pos);
            next = pos + moveDirection * (moveSpeed * Time.fixedDeltaTime);
        }

        next = ClampToBounds(next);
        rb.linearVelocity = (next - pos) / Time.fixedDeltaTime;
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }

    private void PickNewDirection()
    {
        Vector2 randomDir = Random.insideUnitCircle;

        if (randomDir.sqrMagnitude < 0.01f)
        {
            randomDir = Vector2.right;
        }

        moveDirection = randomDir.normalized;
    }

    private bool IsTooCloseToPlayer()
    {
        if (playerTransform == null) return false;

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        return toPlayer.magnitude <= playerBounceDistance;
    }

    private void PickDirectionAwayFromPlayer()
    {
        if (playerTransform == null)
        {
            PickNewDirection();
            return;
        }

        Vector2 away = rb.position - (Vector2)playerTransform.position;

        if (away.sqrMagnitude < 0.01f)
        {
            away = Random.insideUnitCircle;
        }

        moveDirection = away.normalized;
        changeDirectionTimer = changeDirectionInterval;
    }

    private void RedirectInsideBounds(Vector2 currentPos)
    {
        Vector2 center = (minBounds + maxBounds) * 0.5f;
        Vector2 toCenter = center - currentPos;

        if (toCenter.sqrMagnitude < 0.01f)
        {
            toCenter = Random.insideUnitCircle;
        }

        moveDirection = toCenter.normalized;
        changeDirectionTimer = changeDirectionInterval;
    }

    private bool WouldLeaveBounds(Vector2 pos)
    {
        return pos.x < minBounds.x + boundsPadding ||
               pos.x > maxBounds.x - boundsPadding ||
               pos.y < minBounds.y + boundsPadding ||
               pos.y > maxBounds.y - boundsPadding;
    }

    private Vector2 ClampToBounds(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, minBounds.x + boundsPadding, maxBounds.x - boundsPadding),
            Mathf.Clamp(pos.y, minBounds.y + boundsPadding, maxBounds.y - boundsPadding)
        );
    }

    private void KeepInsideBounds()
    {
        Vector2 pos = rb.position;

        if (pos.x <= minBounds.x + boundsPadding)
        {
            moveDirection = Vector2.right;
            changeDirectionTimer = changeDirectionInterval;
        }
        else if (pos.x >= maxBounds.x - boundsPadding)
        {
            moveDirection = Vector2.left;
            changeDirectionTimer = changeDirectionInterval;
        }

        if (pos.y <= minBounds.y + boundsPadding)
        {
            moveDirection = Vector2.up;
            changeDirectionTimer = changeDirectionInterval;
        }
        else if (pos.y >= maxBounds.y - boundsPadding)
        {
            moveDirection = Vector2.down;
            changeDirectionTimer = changeDirectionInterval;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Wall")) return;
        if (collision.contactCount == 0) return;

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 wallNormal = contact.normal;

        moveDirection = wallNormal.normalized;
        changeDirectionTimer = changeDirectionInterval;
    }
}