using UnityEngine;

public class EnemyRoomMember : MonoBehaviour
{
    private RoomEnemyRespawnAnchor roomAnchor;
    private bool hasReportedDeath = false;

    public void Initialize(RoomEnemyRespawnAnchor anchor)
    {
        roomAnchor = anchor;
    }

    public void ReportDeath()
    {
        if (hasReportedDeath) return;
        hasReportedDeath = true;

        Debug.Log($"[EnemyRoomMember] ReportDeath from {name}");
        roomAnchor?.NotifyEnemyDied(gameObject);
    }

    private void OnDestroy()
    {
        if (!hasReportedDeath && roomAnchor != null)
        {
            roomAnchor.NotifyEnemyDied(gameObject);
        }
    }
}