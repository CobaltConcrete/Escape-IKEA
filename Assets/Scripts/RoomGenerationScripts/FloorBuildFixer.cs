using UnityEngine;

public class FloorBuildFixer : MonoBehaviour
{
    [SerializeField] private Material floorLitMaterial;

    private void Start()
    {
        Invoke(nameof(ForceFixFloors), 0.2f);
    }

    private void ForceFixFloors()
    {
        SpriteRenderer[] allRenderers =
            Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        int count = 0;

        foreach (SpriteRenderer sr in allRenderers)
        {
            if (!sr.gameObject.name.Contains("FloorTile"))
                continue;

            // 保留裁剪，不然地板会露出房间
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            sr.sortingLayerName = "Floor";

            if (floorLitMaterial != null)
                sr.sharedMaterial = floorLitMaterial;

            count++;
        }

    }
}