using UnityEngine;

public class RoomSpawnArea : MonoBehaviour
{
    [SerializeField] private BoxCollider2D spawnArea;

    public bool TryGetRandomLocalPoint(Transform roomRoot, out Vector3 localPoint)
    {
        localPoint = Vector3.zero;

        if (spawnArea == null)
        {
            return false;
        }

        Bounds bounds = spawnArea.bounds;

        Vector3 randomWorldPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            roomRoot.position.z
        );

        localPoint = roomRoot.InverseTransformPoint(randomWorldPoint);
        localPoint.z = 0f;
        return true;
    }
}