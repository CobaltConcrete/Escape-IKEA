using UnityEngine;

public class RoomGameplayPopulate : MonoBehaviour
{
    [SerializeField] private GameplaySpawner spawner;

    [SerializeField] private int employeeCount = 2;
    [SerializeField] private int meatballCount = 2;
    [SerializeField] private float spawnEdgeInset = 0.4f;

    private Vector2[] employeeStandPositions = new Vector2[]
    {
        new Vector2(5f, 0f),
        new Vector2(8f, -2f),
        new Vector2(2f, 2f),
        new Vector2(-2f, 0f),
        new Vector2(5f, 3f),
    };

    private Vector2[] meatballPositions = new Vector2[]
    {
        new Vector2(0.35f, 0.5f),
        new Vector2(0.65f, 0.45f),
        new Vector2(0.5f, 0.35f),
    };

    private Transform sessionRoot;
    private bool spawnedForThisVisit;

    private void Awake()
    {
        var holder = new GameObject("SessionSpawns");
        holder.transform.SetParent(transform, false);
        holder.transform.localPosition = Vector3.zero;
        sessionRoot = holder.transform;
    }

    private void Start()
    {
        Invoke(nameof(TryPopulateIfPlayerAlreadyInside), 0.35f);
        Invoke(nameof(TryPopulateIfPlayerAlreadyInside), 1.2f);
    }

    private void TryPopulateIfPlayerAlreadyInside()
    {
        if (spawnedForThisVisit) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        if (!IsPlayerInsideRoomVolume(p.transform.position)) return;
        PopulateRoomForPlayer();
    }

    private bool IsPlayerInsideRoomVolume(Vector3 worldPos)
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null && c.isTrigger)
            return c.OverlapPoint(worldPos);
        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            if (col.isTrigger && col.OverlapPoint(worldPos))
                return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PopulateRoomForPlayer();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!spawnedForThisVisit)
            PopulateRoomForPlayer();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        spawnedForThisVisit = false;
        ClearSession();
    }

    private void PopulateRoomForPlayer()
    {
        if (spawner == null)
            spawner = Object.FindFirstObjectByType<GameplaySpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("RoomGameplayPopulate: add GameplaySpawner to the scene.");
            return;
        }

        ClearSession();
        SpawnForThisVisit();
        spawnedForThisVisit = true;
    }

    private void ClearSession()
    {
        if (sessionRoot == null) return;
        for (int i = sessionRoot.childCount - 1; i >= 0; i--)
            Destroy(sessionRoot.GetChild(i).gameObject);
    }

    private void SpawnForThisVisit()
    {
        Collider2D roomTrigger = GetPrimaryRoomTrigger();
        Bounds b = roomTrigger != null
            ? roomTrigger.bounds
            : new Bounds(transform.position, Vector3.one * 8f);

        float inset = spawnEdgeInset;
        float hx = Mathf.Max(b.size.x * 0.5f - inset, 0.05f);
        float hy = Mathf.Max(b.size.y * 0.5f - inset, 0.05f);
        Vector3 c = b.center;

        int e = Mathf.Min(employeeCount, employeeStandPositions.Length);
        for (int i = 0; i < e; i++)
        {
            Vector3 p = transform.position + (Vector3)employeeStandPositions[i];
            p = ClampToBounds(p, b, inset);
            spawner.SpawnEnemy(p, sessionRoot);
        }

        int m = Mathf.Min(meatballCount, meatballPositions.Length);
        for (int i = 0; i < m; i++)
        {
            Vector2 n = meatballPositions[i];
            Vector3 p;
            if (Mathf.Max(Mathf.Abs(n.x), Mathf.Abs(n.y)) <= 1.001f)
            {
                float u = Mathf.Clamp01(n.x);
                float v = Mathf.Clamp01(n.y);
                p = new Vector3(
                    Mathf.Lerp(c.x - hx, c.x + hx, u),
                    Mathf.Lerp(c.y - hy, c.y + hy, v),
                    c.z);
            }
            else
                p = ClampToBounds(transform.position + (Vector3)n, b, inset);

            spawner.SpawnMeatball(p, sessionRoot);
        }
    }

    private static Vector3 ClampToBounds(Vector3 p, Bounds b, float inset)
    {
        return new Vector3(
            Mathf.Clamp(p.x, b.min.x + inset, b.max.x - inset),
            Mathf.Clamp(p.y, b.min.y + inset, b.max.y - inset),
            p.z);
    }

    private Collider2D GetPrimaryRoomTrigger()
    {
        Collider2D onSelf = GetComponent<Collider2D>();
        if (onSelf != null && onSelf.isTrigger)
            return onSelf;

        Collider2D best = null;
        float bestArea = 0f;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            if (!col.isTrigger) continue;
            float a = col.bounds.size.x * col.bounds.size.y;
            if (a > bestArea)
            {
                bestArea = a;
                best = col;
            }
        }

        return best;
    }
}
