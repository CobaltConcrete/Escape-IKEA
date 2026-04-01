using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 30f;

    [Header("Contact Damage")]
    [SerializeField] private float damageToPlayer = 10f;
    [SerializeField] private float damageCooldown = 0.75f;
    [SerializeField] private float contactDamageRadius = 0.55f;

    [Header("HitboxRange")]
    [SerializeField] private float contactDamageDistance = 0f;
    [Header("Boss")]
    [SerializeField] private bool onlyTakeDamageFromBehind = false;
    [SerializeField] private float behindDotThreshold = 0f;

    private float currentHealth;
    private float lastDamageTime = -999f;

    private Rigidbody2D rb;
    private Enemy enemy;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        enemy = GetComponent<Enemy>() ?? GetComponentInParent<Enemy>();
    }

    private void FixedUpdate()
    {
        TryDamagePlayerOnContact();
    }

    private void TryDamagePlayerOnContact()
    {
        // no damage when enemy is stunned
        if (enemy != null && !enemy.CanDealContactDamage())
            return;

        if (Time.time - lastDamageTime < damageCooldown)
            return;

        Vector2 center = rb != null ? rb.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, contactDamageRadius);

        foreach (Collider2D hit in hits)
        {
            bool isPlayer =
                hit.CompareTag("Player") ||
                (hit.transform.parent != null && hit.transform.parent.CompareTag("Player"));

            if (!isPlayer) continue;

            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) continue;

            ColliderDistance2D distanceInfo = hit.Distance(GetComponent<Collider2D>());

            // too far, can't hit
            if (distanceInfo.distance > contactDamageDistance)
                continue;

            lastDamageTime = Time.time;
            playerHealth.TakeDamage(damageToPlayer);

            if (enemy != null)
            {
                enemy.OnSuccessfulHitPlayer();
            }

            return;
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void TakeDamageFrom(Vector2 attackerPosition, float amount)
    {
        if (amount <= 0f) return;
        if (onlyTakeDamageFromBehind)
        {
            Vector2 toAttacker = (attackerPosition - (Vector2)transform.position).normalized;
            Vector2 forward = transform.right;
            float dot = Vector2.Dot(forward, toAttacker);
            if (dot > behindDotThreshold) return;
        }
        TakeDamage(amount);
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    public void SetStats(float health, float damage)
    {
        maxHealth = health;
        currentHealth = health;
        damageToPlayer = damage;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void ConfigureBossMode(float bossHealth, bool requireBehind, float dotThreshold)
    {
        maxHealth = Mathf.Max(1f, bossHealth);
        currentHealth = maxHealth;
        onlyTakeDamageFromBehind = requireBehind;
        behindDotThreshold = dotThreshold;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactDamageRadius);
    }
}