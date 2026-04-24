using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float wanderChangeInterval = 2f;

    [Header("Visual")]
    [SerializeField] private bool useEmployeeRedTint = true;
    [SerializeField] private Color employeeTint = new Color(0.9f, 0.2f, 0.15f, 1f);
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool playTransformIntroOnRoomEntry = false;
    [SerializeField] private string transformAnimationState = "Transform";
    [SerializeField] private float transformIntroDuration = 0.7f;

    [Header("Bounds")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;
    [SerializeField] private float boundsPadding = 0.1f;

    [Header("Hit Reaction")]
    [SerializeField] private float recoilDistance = 0.3f;
    [SerializeField] private float recoilDuration = 0.12f;
    [SerializeField] private float stunDurationAfterHit = 2f;

    [Header("Dash Through Reaction")]
    [SerializeField] private float dashStaggerDistance = 0.8f;
    [SerializeField] private float dashStaggerDuration = 0.14f;
    [SerializeField] private float dashSideBias = 0.7f;

    [Header("Wall Bounce")]
    [SerializeField] private float wallBounceCooldown = 0.15f;


    private float lastWallBounceTime = -999f;

    private Rigidbody2D rb;
    private Transform playerTransform;

    private float wanderTimer;
    private Vector2 wanderDirection;

    private bool isRecoiling = false;
    private bool isStunned = false;
    private float recoilTimer = 0f;
    private float stunTimer = 0f;
    private Vector2 recoilDirection = Vector2.zero;
    [SerializeField] private float wallRecoverDuration = 0.4f;
    private float wallRecoverTimer = 0f;
    private Vector2 lastMoveDirection = Vector2.right;
    private int currentAnimationStateHash;
    private bool hasPlayedTransformIntro = false;
    private bool isPlayingTransformIntro = false;
    private float transformIntroTimer = 0f;

    private static readonly int WalkLeftHash = Animator.StringToHash("Base Layer.Walk_L");
    private static readonly int WalkRightHash = Animator.StringToHash("Base Layer.Walk_R");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.useFullKinematicContacts = true;
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        PickNewWanderDirection();
    }

    private void Start()
    {
        GameplayDrawOrder.ApplyEnemy(gameObject);

        if (useEmployeeRedTint)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.color = employeeTint;
            }
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
        }
    }

    private void FixedUpdate()
    {
        Vector2 pos = rb.position;

        if (isPlayingTransformIntro)
        {
            transformIntroTimer -= Time.fixedDeltaTime;
            rb.MovePosition(pos);

            if (transformIntroTimer <= 0f)
            {
                isPlayingTransformIntro = false;
                currentAnimationStateHash = 0;
                UpdateAnimationState();
            }

            return;
        }

        // 1) stagger recoiling
        if (isRecoiling)
        {
            recoilTimer -= Time.fixedDeltaTime;

            Vector2 next = pos + recoilDirection * (recoilDistance / recoilDuration) * Time.fixedDeltaTime;

            if (useBounds)
                next = ClampToBounds(next);

            rb.MovePosition(next);

            if (recoilTimer <= 0f)
            {
                isRecoiling = false;
                isStunned = true;
                stunTimer = stunDurationAfterHit;
            }

            UpdateAnimationState();
            return;
        }

        // 2) stunned
        if (isStunned)
        {
            stunTimer -= Time.fixedDeltaTime;
            rb.MovePosition(pos);

            if (stunTimer <= 0f)
                isStunned = false;

            UpdateAnimationState();
            return;
        }

        Vector2 moveDir;

        // 3) if on side, move more to the center first than chase the player
        if (useBounds && IsNearBounds(pos))
        {
            Vector2 center = (minBounds + maxBounds) * 0.5f;
            moveDir = (center - pos).sqrMagnitude > 0.001f
                ? (center - pos).normalized
                : Vector2.zero;
        }
        else
        {
            moveDir = Vector2.zero;

            // 4) chase player normally
            if (wallRecoverTimer > 0f)
            {
                wallRecoverTimer -= Time.fixedDeltaTime;
            }
            else if (playerTransform != null)
            {
                Vector2 toPlayer = (Vector2)playerTransform.position - pos;
                float dist = toPlayer.magnitude;

                if (dist <= detectionRadius && dist > 0.05f)
                {
                    moveDir = toPlayer.normalized;
                }
            }
            if (playerTransform != null)
            {
                Vector2 toPlayer = (Vector2)playerTransform.position - pos;
                float dist = toPlayer.magnitude;

                if (dist <= detectionRadius && dist > 0.05f)
                {
                    moveDir = toPlayer.normalized;
                }
            }

            // 5) wander otherwise
            if (moveDir.sqrMagnitude < 0.01f)
            {
                wanderTimer -= Time.fixedDeltaTime;

                if (wanderTimer <= 0f)
                {
                    PickNewWanderDirection();
                    wanderTimer = wanderChangeInterval;
                }

                moveDir = wanderDirection;
            }
        }

        Vector2 nextMove = pos + moveDir * (moveSpeed * Time.fixedDeltaTime);
        if (moveDir.sqrMagnitude > 0.001f)
            lastMoveDirection = moveDir.normalized;

        if (useBounds)
            nextMove = ClampToBounds(nextMove);

        rb.MovePosition(nextMove);
        UpdateAnimationState();
    }

    private void BounceAwayFromWall(Vector2 wallNormal)
    {
        Vector2 bounceDir = wallNormal.normalized;

        if (bounceDir.sqrMagnitude < 0.001f)
        {
            Vector2 center = (minBounds + maxBounds) * 0.5f;
            bounceDir = (center - rb.position).normalized;
        }

        wanderDirection = bounceDir;
        wanderTimer = wanderChangeInterval;
    }

    private void PickNewWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;

        if (wanderDirection.sqrMagnitude < 0.01f)
        {
            wanderDirection = Vector2.right;
        }
    }

    private Vector2 ClampToBounds(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, minBounds.x + boundsPadding, maxBounds.x - boundsPadding),
            Mathf.Clamp(pos.y, minBounds.y + boundsPadding, maxBounds.y - boundsPadding)
        );
    }

    private bool IsNearBounds(Vector2 pos)
    {
        return pos.x <= minBounds.x + boundsPadding ||
               pos.x >= maxBounds.x - boundsPadding ||
               pos.y <= minBounds.y + boundsPadding ||
               pos.y >= maxBounds.y - boundsPadding;
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    public void Configure(float speed, float detectRange)
    {
        moveSpeed = speed;
        detectionRadius = detectRange;
    }

    public bool CanDealContactDamage()
    {
        return !isRecoiling && !isStunned && !isPlayingTransformIntro;
    }

    public void OnSuccessfulHitPlayer()
    {
        if (playerTransform == null) return;
        if (isRecoiling || isStunned) return;

        Vector2 awayFromPlayer = rb.position - (Vector2)playerTransform.position;

        if (awayFromPlayer.sqrMagnitude < 0.001f)
        {
            awayFromPlayer = Random.insideUnitCircle;
        }

        recoilDirection = awayFromPlayer.normalized;
        recoilTimer = recoilDuration;
        isRecoiling = true;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Wall")) return;
        if (Time.time - lastWallBounceTime < wallBounceCooldown) return;
        if (collision.contactCount == 0) return;

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 wallNormal = contact.normal;

        BounceAwayFromWall(wallNormal);
        wallRecoverTimer = wallRecoverDuration;
        lastWallBounceTime = Time.time;
    }
    public void HitByDash(Vector2 dashDirection, float dashDamage, float dashStunDuration)
    {
        if (dashDirection.sqrMagnitude < 0.001f)
            return;

        Vector2 forward = dashDirection.normalized;
        Vector2 side = new Vector2(-forward.y, forward.x);

        float sideSign = Random.value < 0.5f ? -1f : 1f;
        Vector2 staggerDirection = (forward + side * sideSign * dashSideBias).normalized;

        recoilDirection = staggerDirection;
        recoilDistance = dashStaggerDistance;
        recoilDuration = dashStaggerDuration;
        recoilTimer = recoilDuration;

        isRecoiling = true;
        isStunned = false;

        stunDurationAfterHit = dashStunDuration;

        SendMessage("TakeDamage", dashDamage, SendMessageOptions.DontRequireReceiver);
    }

    public void NotifyRoomActivated()
    {
        if (!playTransformIntroOnRoomEntry || hasPlayedTransformIntro)
            return;

        hasPlayedTransformIntro = true;
        isPlayingTransformIntro = true;
        transformIntroTimer = Mathf.Max(0.05f, transformIntroDuration);
        currentAnimationStateHash = 0;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.Play($"Base Layer.{transformAnimationState}", 0, 0f);
    }

    private void UpdateAnimationState()
    {
        if (animator == null || isPlayingTransformIntro)
            return;

        bool faceLeft = lastMoveDirection.x < -0.01f;
        if (spriteRenderer != null)
            spriteRenderer.flipX = faceLeft;

        int nextHash = faceLeft ? WalkLeftHash : WalkRightHash;
        if (currentAnimationStateHash == nextHash)
            return;

        animator.Play(nextHash, 0, 0f);
        currentAnimationStateHash = nextHash;
    }
}
