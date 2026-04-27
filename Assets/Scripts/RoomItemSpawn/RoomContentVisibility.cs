using UnityEngine;

public class RoomContentVisibility : MonoBehaviour
{
    private Renderer[] cachedRenderers;
    private Collider2D[] cachedColliders;

    private Enemy enemy;
    private EnemyWander enemyWander;
    private EnemyDashCharger enemyDashCharger;
    private EnemyAimerShooter enemyAimerShooter;
    private CafeteriaBossPattern cafeteriaBossPattern;
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
        cafeteriaBossPattern = GetComponent<CafeteriaBossPattern>();
        enemyCombat = GetComponent<EnemyCombat>();
        enemyBullet = GetComponent<EnemyBullet>();
    }

    public void SetActiveInRoom(bool active)
    {
        // Visuals
        foreach (Renderer r in cachedRenderers)
        {
            if (r != null)
            {
                r.enabled = active;
            }
        }

        // Physics / interaction
        foreach (Collider2D c in cachedColliders)
        {
            if (c != null)
            {
                c.enabled = active;
            }
        }

        // Basic movement
        if (enemyWander != null)
        {
            enemyWander.CanMove = active;
        }

        // Special behavior scripts
        if (enemyDashCharger != null)
        {
            enemyDashCharger.enabled = active;
        }

        if (enemyAimerShooter != null)
        {
            enemyAimerShooter.enabled = active && cafeteriaBossPattern == null;
        }

        if (cafeteriaBossPattern != null)
        {
            cafeteriaBossPattern.enabled = active;
        }

        if (enemyCombat != null)
        {
            enemyCombat.enabled = active;
        }

        // Normal enemy chase script
        if (enemy != null)
        {
            if (!active)
            {
                enemy.NotifyRoomDeactivated();
            }
            enemy.enabled = active;
        }

        // Bullets should disappear when room deactivates
        if (!active && enemyBullet != null)
        {
            Destroy(gameObject);
        }
    }
}
