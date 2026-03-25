using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 30f;

    [Header("Contact Damage")]
    [SerializeField] private float damageToPlayer = 10f;
    [SerializeField] private float damageCooldown = 0.75f;
    [SerializeField] private float contactDamageRadius = 0.55f;

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactDamageRadius);
    }
}