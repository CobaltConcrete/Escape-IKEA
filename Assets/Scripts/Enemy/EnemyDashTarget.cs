using System.Collections;
using UnityEngine;

public class EnemyDashTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Enemy enemy;
    [SerializeField] private EnemyCombat enemyCombat;
    [SerializeField] private EnemyWander enemyWander;
    [SerializeField] private EnemyAimerShooter enemyAimerShooter;
    [SerializeField] private EnemyDashCharger enemyDashCharger;

    [Header("Stagger")]
    [SerializeField] private float staggerDistance = 0.9f;
    [SerializeField] private float staggerDuration = 0.14f;
    [SerializeField] private float sidewaysOffsetStrength = 0.35f;

    private Coroutine fallbackStunCoroutine;
    private Coroutine staggerCoroutine;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        enemy = GetComponent<Enemy>() ?? GetComponentInParent<Enemy>();
        enemyCombat = GetComponent<EnemyCombat>() ?? GetComponentInParent<EnemyCombat>();
        enemyWander = GetComponent<EnemyWander>() ?? GetComponentInParent<EnemyWander>();
        enemyAimerShooter = GetComponent<EnemyAimerShooter>() ?? GetComponentInParent<EnemyAimerShooter>();
        enemyDashCharger = GetComponent<EnemyDashCharger>() ?? GetComponentInParent<EnemyDashCharger>();
    }

    public void HitByDash(Vector2 dashDirection, Vector2 playerPosition, float damage, float stunDuration)
    {
        DealDamage(damage);
        ApplyStun(stunDuration);
        StartStagger(playerPosition, dashDirection);
    }

    private void DealDamage(float damage)
    {
        if (enemyCombat != null)
        {
            enemyCombat.TakeDamage(damage);
        }
    }

    private void ApplyStun(float stunDuration)
    {
        if (enemy != null)
        {
            enemy.SendMessage("ApplyStun", stunDuration, SendMessageOptions.DontRequireReceiver);
            enemy.SendMessage("Stun", stunDuration, SendMessageOptions.DontRequireReceiver);
            enemy.SendMessage("SetStunned", true, SendMessageOptions.DontRequireReceiver);
        }

        bool needsFallback =
            (enemyWander != null) ||
            (enemyAimerShooter != null) ||
            (enemyDashCharger != null);

        if (!needsFallback) return;

        if (fallbackStunCoroutine != null)
        {
            StopCoroutine(fallbackStunCoroutine);
        }

        fallbackStunCoroutine = StartCoroutine(FallbackStunRoutine(stunDuration));
    }

    private void StartStagger(Vector2 playerPosition, Vector2 dashDirection)
    {
        if (staggerCoroutine != null)
        {
            StopCoroutine(staggerCoroutine);
        }

        staggerCoroutine = StartCoroutine(StaggerRoutine(playerPosition, dashDirection));
    }

    private IEnumerator StaggerRoutine(Vector2 playerPosition, Vector2 dashDirection)
    {
        if (rb == null)
            yield break;

        Vector2 start = rb.position;

        Vector2 toPlayer = (playerPosition - rb.position).normalized;
        Vector2 perp = new Vector2(-toPlayer.y, toPlayer.x);

        float sideSign = Random.value < 0.5f ? -1f : 1f;

        Vector2 staggerDir =
            (toPlayer + perp * sidewaysOffsetStrength * sideSign).normalized;

        Vector2 end = start + staggerDir * staggerDistance;

        float elapsed = 0f;

        while (elapsed < staggerDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / staggerDuration);

            Vector2 pos = Vector2.Lerp(start, end, t);
            rb.MovePosition(pos);

            yield return new WaitForFixedUpdate();
        }

        staggerCoroutine = null;
    }

    private IEnumerator FallbackStunRoutine(float duration)
    {
        bool hadWander = enemyWander != null;
        bool oldCanMove = false;

        bool hadShooter = enemyAimerShooter != null;
        bool oldShooterEnabled = false;

        bool hadCharger = enemyDashCharger != null;
        bool oldChargerEnabled = false;

        if (hadWander)
        {
            oldCanMove = enemyWander.CanMove;
            enemyWander.CanMove = false;
        }

        if (hadShooter)
        {
            oldShooterEnabled = enemyAimerShooter.enabled;
            enemyAimerShooter.enabled = false;
        }

        if (hadCharger)
        {
            oldChargerEnabled = enemyDashCharger.enabled;
            enemyDashCharger.enabled = false;
        }

        yield return new WaitForSeconds(duration);

        if (hadWander)
        {
            enemyWander.CanMove = oldCanMove;
        }

        if (hadShooter)
        {
            enemyAimerShooter.enabled = oldShooterEnabled;
        }

        if (hadCharger)
        {
            enemyDashCharger.enabled = oldChargerEnabled;
        }

        if (enemy != null)
        {
            enemy.SendMessage("SetStunned", false, SendMessageOptions.DontRequireReceiver);
        }

        fallbackStunCoroutine = null;
    }
}