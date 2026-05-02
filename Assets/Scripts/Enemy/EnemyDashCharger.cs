using System.Collections;
using UnityEngine;

public class EnemyDashCharger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private EnemyWander wander;
    private Rigidbody2D rb;

    [Header("Attack Timing")]
    [SerializeField] private float minAttackInterval = 2.5f;
    [SerializeField] private float maxAttackInterval = 8f;
    [SerializeField] private float aimDuration = 0.8f;

    [Header("Detection")]
    [SerializeField] private float detectRange = 6f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 12f;
    [SerializeField] private float dashDuration = 0.5f;

    [Header("Bounce + Stun")]
    [SerializeField] private float stunDuration = 1.2f;
    [SerializeField] private float bouncePauseDuration = 0.12f;

    [Header("Recoil")]
    [SerializeField] private float recoilDistance = 1.5f;
    [SerializeField] private float recoilDuration = 0.12f;

    [Header("Line of Sight")]
    [SerializeField] private LayerMask dashBlockerLayerMask;
    [SerializeField] private float lineOfSightSkin = 0.15f;
    [SerializeField] private bool debugLineOfSight = true;

    private Collider2D[] ownColliders;
    private readonly System.Collections.Generic.List<Collider2D> ignoredColliders =
        new System.Collections.Generic.List<Collider2D>();

    private float attackTimer;
    private bool isBusy = false;
    private bool isDashing = false;
    private Coroutine activeRoutine;

    private Vector2 dashDirection = Vector2.right;
    private int currentAnimationStateHash;

    private static readonly int AttackLeftHash = Animator.StringToHash("Base Layer.Attack_L");
    private static readonly int AttackRightHash = Animator.StringToHash("Base Layer.Attack_R");
    private static readonly int AttackFrontHash = Animator.StringToHash("Base Layer.Front_ATTACK");
    private static readonly int AttackBackHash = Animator.StringToHash("Base Layer.Back_ATTACK");

    private DistortedShopperAudio shopperAudio;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        wander = GetComponent<EnemyWander>();
        ownColliders = GetComponentsInChildren<Collider2D>();
        shopperAudio = GetComponent<DistortedShopperAudio>();
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        TryFindPlayer();
    }

    private void Start()
    {
        ResetState();
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void OnDisable()
    {
        SetDashIgnoreCollisions(false);
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        ResetState();
    }

    private void Update()
    {
        TryFindPlayer();
        if (!PlayerInRange() || !HasLineOfSightToPlayer())
        {
            if (!isBusy && !isDashing && wander != null)
                wander.CanMove = true;

            return;
        }

        // 玩家在范围内：开始冲刺循环
        if (isBusy) return;

        if (wander != null)
            wander.CanMove = false;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }

            activeRoutine = StartCoroutine(AimThenDash());
        }
    }

    private IEnumerator Recoil(Vector2 direction)
    {
        float timer = 0f;

        while (timer < recoilDuration)
        {
            timer += Time.deltaTime;

            float speed = recoilDistance / recoilDuration;
            rb.linearVelocity = direction * speed;

            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator AimThenDash()
    {

        TryFindPlayer();

        if (player == null || !PlayerInRange() || !HasLineOfSightToPlayer())
        {
            attackTimer = GetRandomAttackInterval();
            activeRoutine = null;

            if (wander != null)
                wander.CanMove = true;

            yield break;
        }

        isBusy = true;
        shopperAudio?.PlayChargerAim();
        isDashing = false;

        if (wander != null)
            wander.CanMove = false;

        rb.linearVelocity = Vector2.zero;

        float timer = 0f;
        Vector2 lockedDir = dashDirection;

        while (timer < aimDuration)
        {
            timer += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
            TryFindPlayer();

            if (player != null)
            {
                Vector2 raw = (Vector2)(player.position - transform.position);


                if (raw.sqrMagnitude > 0.001f)
                {
                    lockedDir = raw.normalized;
                    UpdateAttackAnimation(lockedDir);
                }
            }

            yield return null;
        }

        dashDirection = lockedDir;
        UpdateAttackAnimation(dashDirection);

        isDashing = true;

        // 先忽略家具/loot/item，避免刚起步就被卡住
        SetDashIgnoreCollisions(true);

        // 起步前把自己从重叠的家具里推出一点
        PushOutOfIgnoredObstacles();

        shopperAudio?.PlayChargerCharge();

        rb.linearVelocity = dashDirection.normalized * dashForce;

        float dashTimer = 0f;
        while (dashTimer < dashDuration && isDashing)
        {
            dashTimer += Time.deltaTime;
            rb.linearVelocity = dashDirection.normalized * dashForce;
            yield return new WaitForFixedUpdate();
        }

        if (isDashing)
        {
            yield return StunThenRecover();
        }

        activeRoutine = null;
    }
    private void PushOutOfIgnoredObstacles()
    {
        if (ownColliders == null || ownColliders.Length == 0)
            ownColliders = GetComponentsInChildren<Collider2D>();

        Vector2 totalPush = Vector2.zero;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.8f);

        foreach (Collider2D other in hits)
        {
            if (other == null) continue;
            if (other.isTrigger) continue;
            if (IsOwnCollider(other)) continue;
            if (!ShouldIgnoreColliderDuringDash(other)) continue;

            Vector2 closest = other.ClosestPoint(transform.position);
            Vector2 away = (Vector2)transform.position - closest;

            if (away.sqrMagnitude < 0.001f)
                away = (Vector2)transform.position - (Vector2)other.bounds.center;

            if (away.sqrMagnitude < 0.001f)
                away = -dashDirection;

            totalPush += away.normalized;
        }

        if (totalPush.sqrMagnitude > 0.001f && rb != null)
        {
            rb.position += totalPush.normalized * 0.25f;
        }
    }

    private IEnumerator StunThenRecover()
    {
        isDashing = false;
        SetDashIgnoreCollisions(false);
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(stunDuration);

        RecoverToIdle();

        activeRoutine = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing) return;
        if (collision.contactCount == 0) return;

        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.GetComponentInParent<Door>() != null)
        {
            shopperAudio?.PlayChargerHit();
            isDashing = false;

            Vector2 normal = collision.GetContact(0).normal;

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            activeRoutine = StartCoroutine(RecoilThenStun(normal));
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            shopperAudio?.PlayChargerHit();
            isDashing = false;

            Vector2 away = ((Vector2)transform.position - (Vector2)player.position).normalized;

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            activeRoutine = StartCoroutine(RecoilThenStun(away));
        }
    }

    private IEnumerator RecoilThenStun(Vector2 dir)
    {
        yield return Recoil(dir);

        yield return new WaitForSeconds(bouncePauseDuration);

        yield return StunThenRecover();
    }

    private void RecoverToIdle()
    {
        SetDashIgnoreCollisions(false);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (wander != null)
            wander.CanMove = true;

        attackTimer = GetRandomAttackInterval();
        isBusy = false;
        isDashing = false;
    }

    private void TryFindPlayer()
    {
        if (player != null)
            return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    private void ResetState()
    {
        SetDashIgnoreCollisions(false);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (wander != null)
        {
            wander.CanMove = true;
        }

        attackTimer = GetRandomAttackInterval();
        isBusy = false;
        isDashing = false;
    }

    private float GetRandomAttackInterval()
    {
        float min = Mathf.Min(minAttackInterval, maxAttackInterval);
        float max = Mathf.Max(minAttackInterval, maxAttackInterval);
        return Random.Range(min, max);
    }

    public bool IsBusy => isBusy || isDashing;

    private void UpdateAttackAnimation(Vector2 direction)
    {
        if (animator == null)
            return;

        bool preferVertical = Mathf.Abs(direction.y) > Mathf.Abs(direction.x);
        bool hasFrontBack = animator.HasState(0, AttackFrontHash) && animator.HasState(0, AttackBackHash);
        int nextHash;

        if (preferVertical && hasFrontBack)
        {
            nextHash = direction.y > 0f ? AttackBackHash : AttackFrontHash;
            if (spriteRenderer != null)
                spriteRenderer.flipX = false;
        }
        else
        {
            bool faceLeft = direction.x < -0.01f;
            if (spriteRenderer != null)
                spriteRenderer.flipX = faceLeft;
            nextHash = faceLeft ? AttackLeftHash : AttackRightHash;
        }

        if (currentAnimationStateHash == nextHash)
            return;

        animator.Play(nextHash, 0, 0f);
        currentAnimationStateHash = nextHash;
    }
    private void SetDashIgnoreCollisions(bool ignore)
    {
        if (ownColliders == null || ownColliders.Length == 0)
            ownColliders = GetComponentsInChildren<Collider2D>();

        if (!ignore)
        {
            for (int i = 0; i < ignoredColliders.Count; i++)
            {
                Collider2D other = ignoredColliders[i];
                if (other == null) continue;

                for (int j = 0; j < ownColliders.Length; j++)
                {
                    if (ownColliders[j] != null)
                        Physics2D.IgnoreCollision(ownColliders[j], other, false);
                }
            }

            ignoredColliders.Clear();
            return;
        }

        ignoredColliders.Clear();

        Collider2D[] allColliders = FindObjectsByType<Collider2D>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (Collider2D other in allColliders)
        {
            if (other == null) continue;
            if (other.isTrigger) continue;
            if (IsOwnCollider(other)) continue;

            if (!ShouldIgnoreColliderDuringDash(other))
                continue;

            for (int i = 0; i < ownColliders.Length; i++)
            {
                Collider2D own = ownColliders[i];
                if (own == null) continue;

                Physics2D.IgnoreCollision(own, other, true);
            }

            ignoredColliders.Add(other);
        }
    }

    private bool IsOwnCollider(Collider2D col)
    {
        if (ownColliders == null)
            return false;

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == col)
                return true;
        }

        return false;
    }

    private bool ShouldIgnoreColliderDuringDash(Collider2D other)
    {
        if (other == null)
            return false;

        // 不允许忽略：墙
        if (other.CompareTag("Wall") || other.GetComponentInParent<Door>() != null)
            return false;

        // 不允许忽略：玩家
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
            return false;

        if (other.CompareTag("Door") || other.GetComponentInParent<PlayerHealth>() != null)
            return false;

        // 只忽略：家具 / loot
        Transform t = other.transform;

        while (t != null)
        {
            string layerName = LayerMask.LayerToName(t.gameObject.layer);

            if (layerName == "Loot" ||
                layerName == "Item" ||
                layerName == "Furniture")
            {
                return true;
            }

            t = t.parent;
        }

        // 默认不忽略
        return false;
    }
    private bool PlayerInRange()
    {
        TryFindPlayer();

        if (player == null)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        return distance <= detectRange;
    }
    private bool HasLineOfSightToPlayer()
    {
        if (player == null)
            return false;

        Vector2 from = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 to = player.position;

        Vector2 dir = to - from;
        float distance = dir.magnitude;

        if (distance <= 0.001f)
            return true;

        dir.Normalize();

        // 从自己身体外一点点开始射，避免打到自己的 collider
        Vector2 rayStart = from + dir * lineOfSightSkin;
        float rayDistance = Mathf.Max(0f, distance - lineOfSightSkin);

        RaycastHit2D hit = Physics2D.Raycast(
            rayStart,
            dir,
            rayDistance,
            dashBlockerLayerMask
        );

        if (debugLineOfSight)
        {
            Debug.DrawLine(
                rayStart,
                rayStart + dir * rayDistance,
                hit.collider == null ? Color.green : Color.red,
                0.05f
            );
        }

        return hit.collider == null;
    }
}
