using UnityEngine;

public class RoomContentVisibility : MonoBehaviour
{
    private Renderer[] cachedRenderers;
    private Collider2D[] cachedColliders;

    private Enemy enemy;
    private EnemyWander enemyWander;
    private EnemyDashCharger enemyDashCharger;
    private EnemyAimerShooter enemyAimerShooter;
    private EnemyCombat enemyCombat;
    private EnemyBullet enemyBullet;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider2D>(true);

        enemy = GetComponent<Enemy>();
        enemyWander = GetComponent<EnemyWander>();
        enemyDashCharger = GetComponent<EnemyDashCharger>();
        enemyAimerShooter = GetComponent<EnemyAimerShooter>();
        enemyCombat = GetComponent<EnemyCombat>();
        enemyBullet = GetComponent<EnemyBullet>();
    }

    public void SetActiveInRoom(bool active)
    {
        // show & hide
        foreach (Renderer r in cachedRenderers)
        {
            if (r != null)
                r.enabled = active;
        }

        // collider
        foreach (Collider2D c in cachedColliders)
        {
            if (c != null)
                c.enabled = active;
        }

        // basic movement
        if (enemyWander != null)
        {
            enemyWander.CanMove = active;
        }

        // actions
        if (enemyDashCharger != null)
            enemyDashCharger.enabled = active;

        if (enemyAimerShooter != null)
            enemyAimerShooter.enabled = active;

        if (enemyCombat != null)
            enemyCombat.enabled = active;

        // normal enemy chase script
        if (enemy != null)
            enemy.enabled = active;

        // bullets
        if (!active && enemyBullet != null)
        {
            Destroy(gameObject);
        }
    }
}