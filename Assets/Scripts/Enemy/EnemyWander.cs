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
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float obstacleCheckDistance = 0.55f;
    [SerializeField] private float obstacleCheckRadius = 0.28f;
    [SerializeField] private float obstacleRedirectCooldown = 0.2f;

    private float obstacleRedirectTimer;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float changeDirectionTimer;
    private Transform playerTransform;
    private Vector2 lastFacingDirection = Vector2.right;
    private int currentAnimationStateHash;

    private static readonly int WalkLeftHash = Animator.StringToHash("Base Layer.Walk_L");
    private static readonly int WalkRightHash = Animator.StringToHash("Base Layer.Walk_R");
    private static readonly int WalkFrontHash = Animator.StringToHash("Base Layer.Front_WALKING");
    private static readonly int WalkBackHash = Animator.StringToHash("Base Layer.Back_WALKING");

    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
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
        obstacleRedirectTimer -= Time.deltaTime;

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

        if (ShouldAvoidObstacle())
        {
            PickDirectionAwayFromObstacle();
        }

        Vector2 next = pos + moveDirection * (moveSpeed * Time.fixedDeltaTime);

        if (WouldLeaveBounds(next))
        {
            RedirectInsideBounds(pos);
            next = pos + moveDirection * (moveSpeed * Time.fixedDeltaTime);
        }

        next = ClampToBounds(next);
        rb.linearVelocity = (next - pos) / Time.fixedDeltaTime;

        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude > 0.0001f)
            lastFacingDirection = velocity.normalized;

        UpdateAnimationState();
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
        if (((1 << collision.gameObject.layer) & obstacleLayerMask) == 0)
            return;
        if (collision.contactCount == 0) return;

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 wallNormal = contact.normal;

        moveDirection = wallNormal.normalized;
        changeDirectionTimer = changeDirectionInterval;
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
            return;

        bool preferVertical = Mathf.Abs(lastFacingDirection.y) > Mathf.Abs(lastFacingDirection.x);
        bool hasFrontBack = animator.HasState(0, WalkFrontHash) && animator.HasState(0, WalkBackHash);
        int nextHash;

        if (preferVertical && hasFrontBack)
        {
            nextHash = lastFacingDirection.y > 0f ? WalkBackHash : WalkFrontHash;
            if (spriteRenderer != null)
                spriteRenderer.flipX = false;
        }
        else
        {
            bool faceLeft = lastFacingDirection.x < -0.01f;
            if (spriteRenderer != null)
                spriteRenderer.flipX = faceLeft;
            nextHash = faceLeft ? WalkLeftHash : WalkRightHash;
        }

        if (currentAnimationStateHash == nextHash)
            return;

        animator.Play(nextHash, 0, 0f);
        currentAnimationStateHash = nextHash;
    }
    private bool ShouldAvoidObstacle()
    {
        if (obstacleRedirectTimer > 0f)
            return false;

        Vector2 checkCenter = rb.position + moveDirection.normalized * obstacleCheckDistance;

        Collider2D hit = Physics2D.OverlapCircle(
            checkCenter,
            obstacleCheckRadius,
            obstacleLayerMask
        );

        return hit != null;
    }

    private void PickDirectionAwayFromObstacle()
    {
        Vector2 checkCenter = rb.position + moveDirection.normalized * obstacleCheckDistance;

        Collider2D hit = Physics2D.OverlapCircle(
            checkCenter,
            obstacleCheckRadius,
            obstacleLayerMask
        );

        if (hit == null)
            return;

        Vector2 closest = hit.ClosestPoint(rb.position);
        Vector2 away = rb.position - closest;

        if (away.sqrMagnitude < 0.001f)
        {
            away = rb.position - (Vector2)hit.bounds.center;
        }

        if (away.sqrMagnitude < 0.001f)
        {
            away = -moveDirection;
        }

        Vector2 randomSide = Random.insideUnitCircle.normalized * 0.35f;
        moveDirection = (away.normalized + randomSide).normalized;

        changeDirectionTimer = changeDirectionInterval;
        obstacleRedirectTimer = obstacleRedirectCooldown;
    }
}
