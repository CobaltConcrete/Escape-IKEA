using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float wanderChangeInterval = 2f;

    [SerializeField] private float damageToPlayer = 10f;
    [SerializeField] private float damageCooldown = 0.75f;
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float contactDamageRadius = 0.55f;

    [SerializeField] private bool useEmployeeRedTint = true;
    [SerializeField] private Color employeeTint = new Color(0.9f, 0.2f, 0.15f, 1f);

    private Rigidbody2D rb;
    private Transform playerTransform;
    private float health;
    private float wanderTimer;
    private Vector2 wanderDirection;
    private float lastDamageTime = -999f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.useFullKinematicContacts = true;
        health = maxHealth;
        PickNewWanderDirection();
    }

    private void Start()
    {
        GameplayDrawOrder.ApplyEnemy(gameObject);
        if (useEmployeeRedTint)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                sr.color = employeeTint;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            playerTransform = p.transform;
    }

    private void FixedUpdate()
    {
        Vector2 pos = rb.position;
        Vector2 moveDir = Vector2.zero;

        if (playerTransform != null)
        {
            Vector2 toPlayer = (Vector2)playerTransform.position - pos;
            float dist = toPlayer.magnitude;
            if (dist <= detectionRadius && dist > 0.05f)
                moveDir = toPlayer.normalized;
        }

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

        Vector2 next = pos + moveDir * (moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
        TryDamagePlayerOnContact();
    }

    /// <summary>Overlap check — reliable when collision messages don’t fire (kinematic vs dynamic).</summary>
    private void TryDamagePlayerOnContact()
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, contactDamageRadius);
        foreach (Collider2D h in hits)
        {
            if (!h.CompareTag("Player") && (h.transform.parent == null || !h.transform.parent.CompareTag("Player")))
                continue;

            PlayerHealth ph = h.GetComponent<PlayerHealth>() ?? h.GetComponentInParent<PlayerHealth>();
            if (ph == null) continue;

            lastDamageTime = Time.time;
            ph.TakeDamage(damageToPlayer);
            return;
        }
    }

    private void PickNewWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        if (wanderDirection.sqrMagnitude < 0.01f)
            wanderDirection = Vector2.right;
    }

    private void OnCollisionEnter2D(Collision2D collision) => ApplyDamageFromCollision(collision.gameObject);

    private void OnCollisionStay2D(Collision2D collision) => ApplyDamageFromCollision(collision.gameObject);

    private void ApplyDamageFromCollision(GameObject other)
    {
        if (!other.CompareTag("Player") && (other.transform.parent == null || !other.transform.parent.CompareTag("Player")))
            return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (ph == null) return;

        lastDamageTime = Time.time;
        ph.TakeDamage(damageToPlayer);
    }

    /// <summary>Called by player attacks (e.g. PlayerMeleeAttack).</summary>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        health -= amount;
        if (health <= 0f)
            Destroy(gameObject);
    }

    // Optional: tune from spawner without prefab variants
    public void Configure(float speed, float dmg, float detectRange, float hp = -1f)
    {
        moveSpeed = speed;
        damageToPlayer = dmg;
        detectionRadius = detectRange;
        if (hp > 0f)
        {
            maxHealth = hp;
            health = hp;
        }
    }
}
