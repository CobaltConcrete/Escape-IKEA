using System.Collections.Generic;
using UnityEngine;
using static EquipmentEnum;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float bonusDamagePerExtraBat = 7.5f;
    [SerializeField] private float attackRadius = 1.25f;
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
    [SerializeField] private float bulletClearRadius = 1.6f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackForwardOffsetFactor = 0.75f;
    [SerializeField] private float attackFacingDotThreshold = 0.2f;

    [Header("Hit Audio")]
    [SerializeField] private string hitOneEnemySoundKey = "HitOneEnemy";
    [SerializeField] private string hitMultipleEnemiesSoundKey = "Hit2OrMoreEnemies";

    [Header("Enemy Knockback")]
    [SerializeField] private float enemyKnockbackDistance = 0.45f;
    [SerializeField] private float enemyKnockbackDuration = 0.12f;
    [SerializeField] private float multiHitScreenShakeDuration = 0.12f;
    [SerializeField] private float multiHitScreenShakeStrength = 0.08f;

    private float cooldownRemaining;
    private PlayerInventoryInteraction inventoryInteraction;
    private BossRoomNoticeUI noWeaponNoticeUI;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        inventoryInteraction = GetComponent<PlayerInventoryInteraction>();
        if (inventoryInteraction == null)
            inventoryInteraction = GetComponentInParent<PlayerInventoryInteraction>();
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void Update()
    {
        cooldownRemaining -= Time.deltaTime;

        if (!WasAttackPressed())
            return;

        if (!HasEquippedWeapon())
        {
            ShowNoWeaponMessage();
            return;
        }

        if (cooldownRemaining > 0f)
            return;

        if (playerMovement != null && playerMovement.IsAttackAnimationPlaying())
            return;

        cooldownRemaining = attackCooldown;
        Vector2 facingDirection = playerMovement != null ? playerMovement.GetFacingDirection() : Vector2.down;
        if (facingDirection.sqrMagnitude < 0.001f)
            facingDirection = Vector2.down;
        facingDirection.Normalize();

        playerMovement?.PlayAttackAnimation(attackCooldown);

        Vector2 attackCenter = (Vector2)transform.position + facingDirection * (attackRadius * attackForwardOffsetFactor);

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRadius);
        HashSet<EnemyCombat> damagedEnemies = new HashSet<EnemyCombat>();


        foreach (Collider2D col in hits)
        {
            bool isEnemy =
                col.CompareTag("Enemy") ||
                (col.transform.parent != null && col.transform.parent.CompareTag("Enemy"));

            if (!isEnemy) continue;

            EnemyCombat enemyCombat =
                col.GetComponent<EnemyCombat>() ??
                col.GetComponentInParent<EnemyCombat>();

            Vector2 toEnemy = ((Vector2)col.bounds.center - (Vector2)transform.position);
            if (toEnemy.sqrMagnitude > 0.001f &&
                Vector2.Dot(facingDirection, toEnemy.normalized) < attackFacingDotThreshold)
                continue;

            if (enemyCombat != null && !damagedEnemies.Contains(enemyCombat))
            {
                enemyCombat.TakeDamageFrom(transform.position, GetCurrentAttackDamage());
                KnockbackEnemy(enemyCombat);
                damagedEnemies.Add(enemyCombat);
            }
        }
        int enemyHitCount = damagedEnemies.Count;

        if (enemyHitCount > 0)
        {
            float shakeStrength = Mathf.Clamp(
                0.08f + Mathf.Pow(enemyHitCount, 1.3f) * 0.04f,
                0f,
                0.35f
            );

            float shakeDuration = Mathf.Clamp(
                0.08f + enemyHitCount * 0.03f,
                0f,
                0.25f
            );

            ScreenShake.Instance?.Shake(shakeDuration, shakeStrength);

            if (enemyHitCount == 1)
                SoundManager.Instance?.PlaySound(hitOneEnemySoundKey, 1f);
            else
                SoundManager.Instance?.PlaySound(hitMultipleEnemiesSoundKey, 1f);
        }

        Collider2D[] bulletHits = Physics2D.OverlapCircleAll(attackCenter, bulletClearRadius);
        for (int i = 0; i < bulletHits.Length; i++)
        {
            EnemyBullet bullet = bulletHits[i].GetComponent<EnemyBullet>();
            if (bullet == null) continue;
            Vector2 toBullet = ((Vector2)bulletHits[i].bounds.center - (Vector2)transform.position);
            if (toBullet.sqrMagnitude > 0.001f &&
                Vector2.Dot(facingDirection, toBullet.normalized) < attackFacingDotThreshold)
                continue;
            Destroy(bullet.gameObject);
        }
    }

    private bool HasEquippedWeapon()
    {
        EquipmentData equipmentData = inventoryInteraction != null ? inventoryInteraction.EquipmentData : null;
        Item equippedWeapon =
            equipmentData != null ? equipmentData.GetEquippedItem(EquipTag.Weapon) : null;
        if (equippedWeapon == null || equippedWeapon.definition == null)
            return false;

        string itemName = equippedWeapon.definition.itemName;
        return !string.IsNullOrWhiteSpace(itemName) &&
               itemName.IndexOf(BatWeapon.ItemName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private float GetCurrentAttackDamage()
    {
        int batCount = inventoryInteraction != null
            ? inventoryInteraction.CountOwnedItemsByName(BatWeapon.ItemName, includeEquipped: true)
            : 1;

        int extraBats = Mathf.Max(0, batCount - 1);
        return attackDamage + extraBats * bonusDamagePerExtraBat;
    }

    private bool WasAttackPressed()
    {
        return Input.GetKeyDown(attackKey);
    }

    private void ShowNoWeaponMessage()
    {
        if (noWeaponNoticeUI == null)
        {
            BossRoomNoticeUI[] notices = FindObjectsByType<BossRoomNoticeUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (notices != null && notices.Length > 0)
                noWeaponNoticeUI = notices[0];
        }

        if (noWeaponNoticeUI != null)
            noWeaponNoticeUI.ShowMessage("I have no weapon");
    }
    private void KnockbackEnemy(EnemyCombat enemyCombat)
    {
        if (enemyCombat == null)
            return;

        Vector2 dir = (enemyCombat.transform.position - transform.position).normalized;

        if (dir.sqrMagnitude < 0.001f)
            dir = playerMovement != null ? playerMovement.GetFacingDirection() : Vector2.down;

        Enemy enemy = enemyCombat.GetComponent<Enemy>() ?? enemyCombat.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.HitByDash(dir, enemyKnockbackDistance, enemyKnockbackDuration);
            return;
        }

        Rigidbody2D rb = enemyCombat.GetComponent<Rigidbody2D>() ?? enemyCombat.GetComponentInParent<Rigidbody2D>();
        if (rb != null)
        {
            rb.MovePosition(rb.position + dir * enemyKnockbackDistance);
        }
    }
}
