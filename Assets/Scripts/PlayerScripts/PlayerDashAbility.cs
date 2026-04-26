using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDashAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private EquipmentData equipmentData;

    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 3.5f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 1.2f;

    [Header("Dash Hit Settings")]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float dashHitRadius = 0.6f;
    [SerializeField] private float dashDamage = 20f;
    [SerializeField] private float dashStunDuration = 0.35f;

    [Header("Dash Phase Settings")]
    [SerializeField] private int playerLayer = 8;
    [SerializeField] private int enemyLayer = 10;

    private bool isDashing = false;
    private float lastDashTime = -999f;
    private Vector2 lastNonZeroMoveDirection = Vector2.right;

    private readonly HashSet<Collider2D> hitThisDash = new HashSet<Collider2D>();

    public KeyCode dashKey = KeyCode.Mouse1;

    public bool IsDashing => isDashing;
    public bool IsDashOnCooldown => Time.time < lastDashTime + dashCooldown;
    public float DashCooldownRemaining => Mathf.Max(0f, (lastDashTime + dashCooldown) - Time.time);

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CacheMoveDirection();

        if (CanStartDash() && IsKeyPressed())
        {
            StartCoroutine(DashRoutine());
        }
    }

    private void CacheMoveDirection()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input.sqrMagnitude > 0.001f)
        {
            lastNonZeroMoveDirection = input.normalized;
        }
    }

    private bool CanStartDash()
    {
        if (isDashing) return false;
        if (IsDashOnCooldown) return false;
        if (!HasDashArmorEquipped()) return false;
        if (lastNonZeroMoveDirection.sqrMagnitude <= 0.001f) return false;

        return true;
    }

    private bool IsKeyPressed()
    {
        return Input.GetKeyDown(dashKey);
    }

    private bool HasDashArmorEquipped()
    {
        if (equipmentData == null) return false;

        Item equippedArmor = equipmentData.GetEquippedArmor();
        if (equippedArmor == null) return false;
        if (equippedArmor.definition == null) return false;

        ItemDefinition def = equippedArmor.definition;

        return def.itemCategory == ItemCategory.Normal
            && def.equipTag == EquipmentEnum.EquipTag.Armor
            && def.armorSpecialAbility == ArmorSpecialAbility.Dash;
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        lastDashTime = Time.time;
        hitThisDash.Clear();

        Vector2 dashDirection = lastNonZeroMoveDirection.normalized;
        float dashSpeed = dashDistance / dashDuration;
        float elapsed = 0f;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        rb.linearVelocity = Vector2.zero;

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        while (elapsed < dashDuration)
        {
            float step = dashSpeed * Time.fixedDeltaTime;
            Vector2 nextPosition = rb.position + dashDirection * step;
            rb.MovePosition(nextPosition);

            CheckDashHits(dashDirection);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        rb.linearVelocity = Vector2.zero;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        isDashing = false;
    }

    private void CheckDashHits(Vector2 dashDirection)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, dashHitRadius, enemyLayerMask);

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (hitThisDash.Contains(hit)) continue;

            hitThisDash.Add(hit);

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = hit.GetComponentInParent<Enemy>();
            }

            if (enemy != null)
            {
                enemy.HitByDash(
                    dashDirection,
                    dashDamage,
                    dashStunDuration
                );
            }
            else
            {
                hit.gameObject.SendMessage("TakeDamage", dashDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dashHitRadius);
    }
}