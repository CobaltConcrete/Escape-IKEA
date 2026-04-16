using System.Collections.Generic;
using UnityEngine;
using static EquipmentEnum;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRadius = 1.25f;
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private float bulletClearRadius = 1.6f;
    [SerializeField] private float attackCooldown = 0.4f;

    private float cooldownRemaining;
    private PlayerInventoryInteraction inventoryInteraction;

    private void Awake()
    {
        inventoryInteraction = GetComponent<PlayerInventoryInteraction>();
        if (inventoryInteraction == null)
            inventoryInteraction = GetComponentInParent<PlayerInventoryInteraction>();
    }

    private void Update()
    {
        cooldownRemaining -= Time.deltaTime;

        if (!WasAttackPressed())
            return;

        if (cooldownRemaining > 0f)
            return;

        EquipmentData equipmentData = inventoryInteraction != null ? inventoryInteraction.EquipmentData : null;
        Item equippedWeapon =
            equipmentData != null ? equipmentData.GetEquippedItem(EquipTag.Weapon) : null;

        if (equippedWeapon == null)
        {
            BossRoomNoticeUI.Instance?.ShowMessage("I have no weapon");
            return;
        }

        cooldownRemaining = attackCooldown;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);
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

            if (enemyCombat != null && !damagedEnemies.Contains(enemyCombat))
            {
                enemyCombat.TakeDamageFrom(transform.position, attackDamage);
                damagedEnemies.Add(enemyCombat);
            }
        }

        Collider2D[] bulletHits = Physics2D.OverlapCircleAll(transform.position, bulletClearRadius);
        for (int i = 0; i < bulletHits.Length; i++)
        {
            EnemyBullet bullet = bulletHits[i].GetComponent<EnemyBullet>();
            if (bullet == null) continue;
            Destroy(bullet.gameObject);
        }
    }

    private bool WasAttackPressed()
    {
        return Input.GetKeyDown(attackKey);
    }
}