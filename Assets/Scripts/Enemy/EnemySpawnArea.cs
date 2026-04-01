using UnityEngine;

public class EnemySpawnArea : MonoBehaviour
{
    private BoxCollider2D box;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
    }

    public bool TryGetRandomPoint(out Vector3 worldPos)
    {
        if (box == null)
        {
            worldPos = Vector3.zero;
            return false;
        }

        Bounds bounds = box.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        worldPos = new Vector3(x, y, 0f);
        return true;
    }
}