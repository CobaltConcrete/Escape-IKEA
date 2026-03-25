using System.Collections;
using UnityEngine;

public class EnemyDashCharger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private EnemyWander wander;
    private Rigidbody2D rb;

    [Header("Attack Timing")]
    [SerializeField] private float minAttackInterval = 2.5f;
    [SerializeField] private float maxAttackInterval = 8f;
    [SerializeField] private float aimDuration = 0.8f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 12f;
    [SerializeField] private float dashDuration = 0.5f;

    [Header("Bounce + Stun")]
    [SerializeField] private float stunDuration = 1.2f;
    [SerializeField] private float bouncePauseDuration = 0.12f;

    [Header("Recoil")]
    [SerializeField] private float recoilDistance = 1.5f;
    [SerializeField] private float recoilDuration = 0.12f;

    private float attackTimer;
    private bool isBusy = false;
    private bool isDashing = false;
    private Coroutine activeRoutine;

    private Vector2 dashDirection = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        wander = GetComponent<EnemyWander>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
            }
        }
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
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        ResetState();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        if (isBusy) return;

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
        isBusy = true;
        isDashing = false;

        if (wander != null)
            wander.CanMove = false;

        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.yellow;

        float timer = 0f;
        Vector2 lockedDir = dashDirection;

        while (timer < aimDuration)
        {
            timer += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;

            if (player != null)
            {
                Vector2 raw = (Vector2)(player.position - transform.position);
                if (raw.sqrMagnitude > 0.001f)
                    lockedDir = raw.normalized;
            }

            yield return null;
        }

        dashDirection = lockedDir;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        isDashing = true;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dashDirection * dashForce, ForceMode2D.Impulse);

        float dashTimer = 0f;
        while (dashTimer < dashDuration && isDashing)
        {
            dashTimer += Time.deltaTime;
            yield return null;
        }

        if (isDashing)
        {
            yield return StunThenRecover();
        }

        activeRoutine = null;
    }

    private IEnumerator StunThenRecover()
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.cyan;

        yield return new WaitForSeconds(stunDuration);

        RecoverToIdle();

        activeRoutine = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing) return;
        if (collision.contactCount == 0) return;

        if (collision.gameObject.CompareTag("Wall"))
        {
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
        if (spriteRenderer != null)
            spriteRenderer.color = Color.cyan;

        yield return Recoil(dir);

        yield return new WaitForSeconds(bouncePauseDuration);

        yield return StunThenRecover();
    }

    private void RecoverToIdle()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        if (wander != null)
            wander.CanMove = true;

        attackTimer = GetRandomAttackInterval();
        isBusy = false;
        isDashing = false;
    }

    private void ResetState()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (wander != null)
        {
            wander.CanMove = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
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
}