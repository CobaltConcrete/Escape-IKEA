using UnityEngine;

public class CafeteriaBossPattern : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float moveRadius = 2f;
    [SerializeField] private float angularSpeed = 1.2f;
    [SerializeField] private int bulletsPerVolley = 3;
    [SerializeField] private float volleySpread = 24f;
    [SerializeField] private float shotInterval = 0.6f;
    [SerializeField] private float attackDuration = 3.5f;
    [SerializeField] private float pauseDuration = 2f;
    [SerializeField] private bool keepBulletsInBossRoom = true;
    [SerializeField] private bool bulletsBounceOnWalls = true;

    private Vector3 center;
    private float angle;
    private float shotTimer;
    private float phaseTimer;
    private bool attacking = true;
    private bool hasRoomBounds;
    private Bounds roomBounds;

    private void Awake()
    {
        EnsureReferences();
        center = transform.position;
        phaseTimer = attackDuration;
        shotTimer = 0f;

        CacheRoomBounds();
    }

    private void OnEnable()
    {
        EnsureReferences();
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
    }

    private void Update()
    {
        MoveCircle();

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

    private void MoveCircle()
    {
        angle += angularSpeed * Time.deltaTime;
        float x = Mathf.Cos(angle) * moveRadius;
        float y = Mathf.Sin(angle) * moveRadius;
        Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
        if (tangent.sqrMagnitude > 0.0001f)
            transform.right = tangent.normalized;
        transform.position = center + new Vector3(x, y, 0f);
    }

    private void ShootVolley()
    {
        if (bulletPrefab == null) return;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        int count = Mathf.Max(1, bulletsPerVolley);
        float facingAngle = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
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
