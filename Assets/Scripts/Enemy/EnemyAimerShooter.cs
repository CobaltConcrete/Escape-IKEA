using System.Collections;
using UnityEngine;

public class EnemyAimerShooter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Attack Timing")]
    [SerializeField] private float minAttackInterval = 2.8f;
    [SerializeField] private float maxAttackInterval = 5.2f;
    [SerializeField] private float aimDuration = 3f;
    [SerializeField] private bool useBurstPattern = false;
    [SerializeField] private int burstShots = 3;
    [SerializeField] private float burstGap = 0.45f;
    [SerializeField] private float burstPause = 2f;

    [Header("Tracking")]
    [SerializeField] private float trackingRange = 6f;
    [SerializeField] private float cancelThreshold = 0.4f;

    [Header("Bullet")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Aim Line")]
    [SerializeField] private LineRenderer lineRenderer;

    private bool isAttacking = false;
    private EnemyWander wander;
    private Coroutine attackLoopRoutine;

    private void Awake()
    {
        wander = GetComponent<EnemyWander>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private void Start()
    {
        InitializeVisualsAndState();
        StartAttackLoop();
    }

    private void OnEnable()
    {
        InitializeVisualsAndState();
        StartAttackLoop();
    }

    private void OnDisable()
    {
        if (attackLoopRoutine != null)
        {
            StopCoroutine(attackLoopRoutine);
            attackLoopRoutine = null;
        }

        isAttacking = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        if (wander != null)
        {
            wander.CanMove = true;
        }
    }

    private void InitializeVisualsAndState()
    {
        isAttacking = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2;
        }

        if (wander != null)
        {
            wander.CanMove = true;
        }
    }

    private void StartAttackLoop()
    {
        if (!gameObject.activeInHierarchy) return;
        if (attackLoopRoutine != null) return;

        attackLoopRoutine = StartCoroutine(BeginAttackLoop());
    }

    private IEnumerator BeginAttackLoop()
    {
        float firstDelay = GetRandomAttackInterval();
        yield return new WaitForSeconds(firstDelay);

        yield return AttackLoop();
        attackLoopRoutine = null;
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            if (useBurstPattern)
            {
                for (int i = 0; i < burstShots; i++)
                {
                    if (!isAttacking)
                        yield return AimAndShoot();

                    if (i < burstShots - 1)
                        yield return new WaitForSeconds(burstGap);
                }
                yield return new WaitForSeconds(burstPause);
            }
            else
            {
                float nextDelay = GetRandomAttackInterval();
                yield return new WaitForSeconds(nextDelay);

                if (!isAttacking)
                    yield return AimAndShoot();
            }
        }
    }
    private float GetRandomAttackInterval()
    {
        float min = Mathf.Min(minAttackInterval, maxAttackInterval);
        float max = Mathf.Max(minAttackInterval, maxAttackInterval);
        return Random.Range(min, max);
    }

    //private IEnumerator AimAndShoot()
    //{
    //    if (firePoint == null)
    //        yield break;
    //    if (player == null || Vector2.Distance(firePoint.position, player.position) > trackingRange)
    //    {
    //        isAttacking = false;
    //        if (wander != null) wander.CanMove = true;
    //        yield break;
    //    }

    //    isAttacking = true;

    //    if (wander != null)
    //        wander.CanMove = false;

    //    float timer = 0f;
    //    Vector3 lockedTargetPosition = firePoint.position;

    //    // try lock player's position when first start to aim
    //    if (player != null)
    //    {
    //        float distToPlayer = Vector2.Distance(firePoint.position, player.position);
    //        if (distToPlayer <= trackingRange)
    //        {
    //            lockedTargetPosition = player.position;
    //        }
    //    }

    //    if (lineRenderer != null)
    //        lineRenderer.enabled = true;

    //    while (timer < aimDuration)
    //    {
    //        timer += Time.deltaTime;

    //        if (player != null)
    //        {
    //            float distToPlayer = Vector2.Distance(firePoint.position, player.position);

    //            // update aim only when player is in range
    //            if (distToPlayer <= trackingRange)
    //            {
    //                lockedTargetPosition = player.position;
    //            }
    //        }

    //        // lock the current place no matter if the player is in the range or not
    //        UpdateAimLine(lockedTargetPosition);

    //        yield return null;
    //    }

    //    UpdateAimLine(lockedTargetPosition);

    //    Vector2 lockedDirection = (lockedTargetPosition - firePoint.position).normalized;
    //    ShootBullet(lockedDirection);

    //    yield return new WaitForSeconds(0.15f);

    //    if (lineRenderer != null)
    //        lineRenderer.enabled = false;

    //    if (wander != null)
    //        wander.CanMove = true;

    //    isAttacking = false;
    //}
    private IEnumerator AimAndShoot()
    {
        if (firePoint == null)
            yield break;

        // if not in range at the start, cancel attack
        if (player == null || Vector2.Distance(firePoint.position, player.position) > trackingRange)
        {
            yield break;
        }

        isAttacking = true;

        if (wander != null)
            wander.CanMove = false;

        float timer = 0f;
        Vector3 lockedTargetPosition = player.position;

        if (lineRenderer != null)
            lineRenderer.enabled = true;

        bool shouldCancel = false;

        while (timer < aimDuration)
        {
            timer += Time.deltaTime;

            float progress = timer / aimDuration;

            if (player != null)
            {
                float dist = Vector2.Distance(firePoint.position, player.position);

                if (dist <= trackingRange)
                {
                    // tracks normally
                    lockedTargetPosition = player.position;
                }
                else
                {
                    // player out of range
                    if (progress < cancelThreshold)
                    {
                        shouldCancel = true;
                        break;
                    }
                    // leaves area late, no longer updates aim, but lock to the last place that spotted the player in range
                }
            }

            UpdateAimLine(lockedTargetPosition);
            yield return null;
        }

        // cancel shoot
        if (shouldCancel)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;

            if (wander != null)
                wander.CanMove = true;

            isAttacking = false;
            yield break;
        }

        // shoot normally
        UpdateAimLine(lockedTargetPosition);

        Vector2 dir = (lockedTargetPosition - firePoint.position).normalized;
        ShootBullet(dir);

        yield return new WaitForSeconds(0.15f);

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        if (wander != null)
            wander.CanMove = true;

        isAttacking = false;
    }

    private void UpdateAimLine(Vector3 targetPosition)
    {
        if (lineRenderer == null || firePoint == null) return;

        lineRenderer.SetPosition(0, firePoint.position);
        lineRenderer.SetPosition(1, targetPosition);
    }

    private void ShootBullet(Vector2 direction)
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        if (transform.parent != null)
        {
            bulletObj.transform.SetParent(transform.parent, true);
        }
        if (bulletObj.GetComponent<RoomContentVisibility>() == null)
            bulletObj.AddComponent<RoomContentVisibility>();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bulletObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.SetDirection(direction);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position, trackingRange);
    }

    public GameObject GetBulletPrefab()
    {
        return bulletPrefab;
    }

    public Transform GetFirePoint()
    {
        return firePoint;
    }
}