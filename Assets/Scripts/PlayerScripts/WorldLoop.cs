using UnityEngine;

public class WorldLoop : MonoBehaviour
{
    [SerializeField] private GameObject mapManagerGO;
    [SerializeField] private Transform cameraTransform;

    private MapManager mapManager;
    private static bool externalLoopLock = false;

    float minX, maxX, minY, maxY;

    void Start()
    {
        mapManager = mapManagerGO.GetComponent<MapManager>();

        float halfX = mapManager.unitCellSize.x * 0.5f;
        float halfY = mapManager.unitCellSize.y * 0.5f;

        minX = mapManager.MapToWorld(0, 0).x - halfX;
        maxX = mapManager.MapToWorld(mapManager.maxCellCols - 1, 0).x + halfX;

        minY = mapManager.MapToWorld(0, 0).y - halfY;
        maxY = mapManager.MapToWorld(0, mapManager.maxCellRows - 1).y + halfY;
    }

    void Update()
    {
        if (externalLoopLock) return;

        Vector3 pos = transform.position;
        Vector3 delta = Vector3.zero;

        if (pos.x > maxX)
        {
            delta.x = minX - pos.x;
            pos.x = minX;
        }
        else if (pos.x < minX)
        {
            delta.x = maxX - pos.x;
            pos.x = maxX;
        }

        if (pos.y > maxY)
        {
            delta.y = minY - pos.y;
            pos.y = minY;
        }
        else if (pos.y < minY)
        {
            delta.y = maxY - pos.y;
            pos.y = maxY;
        }

        if (delta != Vector3.zero)
        {
            transform.position = pos;

            if (cameraTransform != null)
                cameraTransform.position += delta;
        }
    }

    public static void SetExternalLoopLock(bool isLocked)
    {
        externalLoopLock = isLocked;
    }
}