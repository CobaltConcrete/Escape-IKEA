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
                damagedEnemies.Add(enemyCombat);
            }
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
}
