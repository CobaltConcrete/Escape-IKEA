using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRadius = 1.25f;
    [SerializeField] private KeyCode attackKey = KeyCode.J;

    private void Update()
    {
        if (!WasAttackPressed())
            return;

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
                enemyCombat.TakeDamage(attackDamage);
                damagedEnemies.Add(enemyCombat);
            }
        }
    }

    private bool WasAttackPressed()
    {
        return Input.GetKeyDown(attackKey);
    }
}