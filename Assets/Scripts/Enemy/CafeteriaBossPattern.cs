using UnityEngine;

public class CafeteriaBossPattern : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.4f;
    [SerializeField] private float waypointReachDistance = 0.18f;
    [SerializeField] private float minWalkDuration = 0.8f;
    [SerializeField] private float maxWalkDuration = 1.8f;
    [SerializeField] private float minPauseBetweenWalks = 0.15f;
    [SerializeField] private float maxPauseBetweenWalks = 0.45f;
    [SerializeField] private float boundsPadding = 0.65f;

    [SerializeField] private int bulletsPerVolley = 3;
    [SerializeField] private float volleySpread = 24f;
    [SerializeField] private float shotInterval = 0.6f;
    [SerializeField] private float attackDuration = 3.5f;
    [SerializeField] private float pauseDuration = 2f;
    [SerializeField] private bool keepBulletsInBossRoom = true;
    [SerializeField] private bool bulletsBounceOnWalls = true;

    [Header("Flee when chased")]
    [SerializeField] private float fleeRadius = 4f;
    [SerializeField] private float fleePushPerSecond = 3.5f;
    [SerializeField] private float chaseDotThreshold = 0.2f;
    [SerializeField] private float minPlayerSpeed = 0.35f;

    private Vector3 homePosition;
    private Vector2 walkTarget;
    private float walkTimer;
    private float walkPauseTimer;
    private float shotTimer;
    private float phaseTimer;
    private bool attacking = true;
    private bool hasRoomBounds;
    private Bounds roomBounds;
    private Vector2 lastFacingDirection = Vector2.right;

    private Transform playerTransform;
    private Rigidbody2D playerRb;

    private void Awake()
    {
        EnsureReferences();
        homePosition = transform.position;
        phaseTimer = attackDuration;
        shotTimer = 0f;

        CacheRoomBounds();
        PickNewWalkTarget();
    }

    private void OnEnable()
    {
        EnsureReferences();
        transform.rotation = Quaternion.identity;
        EnemyWander wander = GetComponent<EnemyWander>();
        if (wander != null) wander.enabled = false;

        EnemyAimerShooter aimer = GetComponent<EnemyAimerShooter>();
        if (aimer != null) aimer.enabled = false;
        LineRenderer aimLine = GetComponent<LineRenderer>();
        if (aimLine != null) aimLine.enabled = false;
    }

    private void EnsureReferences()
    {
        EnemyAimerShooter shooter = GetComponent<EnemyAimerShooter>();
        if (bulletPrefab == null && shooter != null)
            bulletPrefab = shooter.GetBulletPrefab();
        if (firePoint == null && shooter != null)
            firePoint = shooter.GetFirePoint();
    }

    public void SetRoomBounds(Bounds bounds)
    {
        roomBounds = bounds;
        hasRoomBounds = true;
        PickNewWalkTarget();
    }

    private void Update()
    {
        MoveLikePerson();

        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
        {
            attacking = !attacking;
            phaseTimer = attacking ? attackDuration : pauseDuration;
        }

        if (!attacking) return;

        shotTimer -= Time.deltaTime;
        if (shotTimer <= 0f)
        {
            shotTimer = shotInterval;
            ShootVolley();
        }
    }

    private void MoveLikePerson()
    {
        Vector2 current = transform.position;
        bool isFleeing = TryGetFleeDirection(current, out Vector2 fleeDirection);
        if (isFleeing)
        {
            walkTarget = ClampToWalkableBounds(current + fleeDirection * fleeRadius);
            walkTimer = Mathf.Max(walkTimer, 0.35f);
            walkPauseTimer = 0f;
        }

        if (walkPauseTimer > 0f)
        {
            walkPauseTimer -= Time.deltaTime;
            return;
        }

        if (walkTimer <= 0f || Vector2.Distance(current, walkTarget) <= waypointReachDistance)
            PickNewWalkTarget();

        float currentSpeed = isFleeing ? Mathf.Max(walkSpeed, fleePushPerSecond) : walkSpeed;
        float stepDistance = currentSpeed * Time.deltaTime;
        Vector2 next = Vector2.MoveTowards(current, walkTarget, stepDistance);
        Vector2 clampedNext = ClampToWalkableBounds(next);
        bool hitBounds = (clampedNext - next).sqrMagnitude > 0.0001f;

        Vector2 actualMove = clampedNext - current;
        if (actualMove.sqrMagnitude > 0.0001f)
            lastFacingDirection = actualMove.normalized;

        transform.rotation = Quaternion.identity;
        transform.position = new Vector3(clampedNext.x, clampedNext.y, transform.position.z);
        walkTimer -= Time.deltaTime;

        if (hitBounds || walkTimer <= 0f || Vector2.Distance(clampedNext, walkTarget) <= waypointReachDistance)
        {
            walkPauseTimer = Random.Range(Mathf.Min(minPauseBetweenWalks, maxPauseBetweenWalks), Mathf.Max(minPauseBetweenWalks, maxPauseBetweenWalks));
            walkTimer = 0f;
        }
    }

    private bool TryGetFleeDirection(Vector2 bossPosition, out Vector2 fleeDirection)
    {
        fleeDirection = Vector2.zero;
        EnsurePlayerRefs();

        if (playerTransform == null || playerRb == null || fleeRadius <= 0.01f)
            return false;

        Vector2 playerPosition = playerTransform.position;
        Vector2 toBoss = bossPosition - playerPosition;
        float distance = toBoss.magnitude;
        if (distance <= 0.05f || distance >= fleeRadius)
            return false;

        Vector2 playerVelocity = playerRb.linearVelocity;
        if (playerVelocity.magnitude < minPlayerSpeed)
            return false;

        Vector2 toBossNormal = toBoss / distance;
        float towardBoss = Vector2.Dot(playerVelocity.normalized, toBossNormal);
        if (towardBoss <= chaseDotThreshold)
            return false;

        fleeDirection = toBossNormal;
        return true;
    }

    private void PickNewWalkTarget()
    {
        Vector2 current = transform.position;
        walkTarget = PickRandomWalkablePoint(current);
        walkTimer = Random.Range(Mathf.Min(minWalkDuration, maxWalkDuration), Mathf.Max(minWalkDuration, maxWalkDuration));
        walkPauseTimer = 0f;
    }

    private Vector2 PickRandomWalkablePoint(Vector2 current)
    {
        GetWalkableBounds(out float minX, out float maxX, out float minY, out float maxY);

        for (int i = 0; i < 10; i++)
        {
            Vector2 candidate = hasRoomBounds
                ? new Vector2(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY))
                : (Vector2)homePosition + Random.insideUnitCircle * 3f;

            if ((candidate - current).sqrMagnitude >= 1f)
                return candidate;
        }

        Vector2 fallbackDirection = Random.insideUnitCircle.normalized;
        if (fallbackDirection.sqrMagnitude <= 0.0001f)
            fallbackDirection = Vector2.right;
        return ClampToWalkableBounds(current + fallbackDirection * 1.5f);
    }

    private Vector2 ClampToWalkableBounds(Vector2 position)
    {
        if (!hasRoomBounds)
            return position;

        GetWalkableBounds(out float minX, out float maxX, out float minY, out float maxY);
        return new Vector2(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY));
    }

    private void GetWalkableBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = roomBounds.min.x + boundsPadding;
        maxX = roomBounds.max.x - boundsPadding;
        minY = roomBounds.min.y + boundsPadding;
        maxY = roomBounds.max.y - boundsPadding;

        if (minX > maxX)
        {
            minX = roomBounds.min.x;
            maxX = roomBounds.max.x;
        }

        if (minY > maxY)
        {
            minY = roomBounds.min.y;
            maxY = roomBounds.max.y;
        }
    }

    private void EnsurePlayerRefs()
    {
        if (playerTransform != null && playerRb != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        playerTransform = p.transform;
        playerRb = p.GetComponent<Rigidbody2D>();
    }

    private void ShootVolley()
    {
        if (bulletPrefab == null) return;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        int count = Mathf.Max(1, bulletsPerVolley);
        Vector2 facing = lastFacingDirection.sqrMagnitude > 0.0001f ? lastFacingDirection.normalized : Vector2.right;
        float facingAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        float halfSpread = volleySpread * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float a = facingAngle + Mathf.Lerp(-halfSpread, halfSpread, t);
            float rad = a * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            GameObject bulletObj = Object.Instantiate(bulletPrefab, origin, Quaternion.identity);
            if (transform.parent != null)
                bulletObj.transform.SetParent(transform.parent, true);
            if (bulletObj.GetComponent<RoomContentVisibility>() == null)
                bulletObj.AddComponent<RoomContentVisibility>();

            bulletObj.transform.rotation = Quaternion.Euler(0f, 0f, a);
            EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.SetDirection(dir);
                if (keepBulletsInBossRoom && hasRoomBounds)
                    bullet.SetBounds(roomBounds, bulletsBounceOnWalls);
            }
        }
    }

    private void CacheRoomBounds()
    {
        Transform t = transform;
        while (t != null)
        {
            Room room = t.GetComponent<Room>();
            if (room != null)
            {
                Collider2D trigger = t.GetComponent<Collider2D>();
                if (trigger != null && trigger.isTrigger)
                {
                    roomBounds = trigger.bounds;
                    hasRoomBounds = true;
                }
                return;
            }
            t = t.parent;
        }
    }
}
