using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Runtime room visuals: masked floor tile grid + room label. Tile pivots snap to the floor sprite PPU grid to reduce camera jitter.
/// </summary>
[DisallowMultipleComponent]
public class RoomPresentation : MonoBehaviour
{
    private const string LegacyFloorName = "Floor";
    private const string FloorClipRootName = "FloorClipRoot";
    private const string FloorInteriorMaskName = "FloorInteriorMask";
    private const string FloorTileGridName = "FloorTileGrid";
    private const string FloorTilePrefix = "FloorTile_";
    private const string RoomLabelName = "RoomTypeText";

    private static Sprite s_whiteRectMaskSprite;

    [Header("Sorting — lowest to highest: backdrop → tiles (masked) → walls → gameplay → UI")]
    [SerializeField] private string floorBackdropSortingLayerName = "Default";
    [Tooltip("Room fill (legacy stretched sprite). Keeps tiles visually above this layer.")]
    [SerializeField] private int floorBackdropSortingOrder = -500;
    [SerializeField] private string floorSortingLayerName = "Floor";
    [SerializeField] private int floorTileSortingOrder = 0;
    [Tooltip("SpriteMask sorting on Floor layer; tiles use mask range between back and front orders.")]
    [SerializeField] private int floorMaskSortingOrder = -10;
    [SerializeField] private int floorMaskRangeBackOrder = -5;
    [SerializeField] private int floorMaskRangeFrontOrder = 15;
    [Header("Floor tiles")]
    [Tooltip("Uniform scale per tile. Spacing matches tile.bounds.size * this so tiles stay edge-to-edge with no gaps.")]
    [SerializeField] [Min(0.01f)] private float floorTileScaleFactor = 1.5f;
    [Tooltip("Use the same layer as floor tiles so world-space TMP sorts with the room; Player layer sorts after Floor, so the player draws on top.")]
    [SerializeField] private string labelSortingLayerName = "Item";
    [SerializeField] private int labelSortingOrder = 200;
    [Tooltip("Extra offset in world units after placing the label at the floor coverage center.")]
    [SerializeField] private Vector2 labelOffsetFromRoomCenter = Vector2.zero;
    [SerializeField] private float labelMaxFontSize = 5.5f;
    [SerializeField] private float labelMinFontSize = 0.55f;

    private bool initialized;
    private float referencePixelsPerUnit = 100f;

    public void Initialize(
        Sprite floorTileSprite,
        RoomDecorationCatalog roomDecorationCatalog = null,
        RoomPrefabSpawnCatalog prefabSpawnCatalog = null)
    {
        if (initialized)
            return;
        initialized = true;

        if (floorTileSprite != null && floorTileSprite.pixelsPerUnit > 0.01f)
            referencePixelsPerUnit = floorTileSprite.pixelsPerUnit;

        Bounds coverage = ComputeFloorCoverageBounds(transform);
        Transform floorRoot = FindLegacyFloor(transform);

        if (floorTileSprite != null && floorRoot != null)
            BuildFloorWithMaskAndTiles(floorRoot, floorTileSprite, coverage, referencePixelsPerUnit);

        SetupRoomLabel(coverage, referencePixelsPerUnit);

        Room room = GetComponent<Room>();
        room?.RefreshRendererRegistry();

        if (prefabSpawnCatalog != null)
        {
            RoomPrefabStaticPlacer.PlaceRoomPrefabs(transform, prefabSpawnCatalog);
        }
        else
        {
            Debug.LogWarning($"RoomPresentation: Missing prefab spawn catalog for room '{name}'. Prefab-only mode skips legacy decoration path.", this);
        }
    }

    private static Transform FindLegacyFloor(Transform roomRoot)
    {
        Transform direct = roomRoot.Find(LegacyFloorName);
        if (direct != null && direct.GetComponent<SpriteRenderer>() != null)
            return direct;

        foreach (Transform t in roomRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == LegacyFloorName && t.GetComponent<SpriteRenderer>() != null)
                return t;
        }

        return null;
    }

    private static Bounds ComputeFloorCoverageBounds(Transform roomRoot)
    {
        Transform floor = FindLegacyFloor(roomRoot);
        if (floor != null)
        {
            SpriteRenderer sr = floor.GetComponent<SpriteRenderer>();
            if (sr != null)
                return sr.bounds;
        }

        Collider2D[] cols = roomRoot.GetComponentsInChildren<Collider2D>(true);
        Collider2D best = null;
        float bestArea = 0f;
        foreach (Collider2D c in cols)
        {
            if (c == null || !c.isTrigger)
                continue;
            float a = c.bounds.size.x * c.bounds.size.y;
            if (a > bestArea)
            {
                bestArea = a;
                best = c;
            }
        }

        if (best != null)
            return best.bounds;

        return new Bounds(roomRoot.position, new Vector3(10f, 10f, 0.1f));
    }

    private void BuildFloorWithMaskAndTiles(Transform floorTransform, Sprite tile, Bounds floorWorldBounds, float pixelsPerUnit)
    {
        SpriteRenderer legacySr = floorTransform.GetComponent<SpriteRenderer>();
        if (legacySr == null)
            return;

        Vector2 baseTileSize = tile.bounds.size;
        if (baseTileSize.x < 0.0001f || baseTileSize.y < 0.0001f)
            return;

        float scaleFactor = Mathf.Max(0.01f, floorTileScaleFactor);
        Vector2 cellStride = baseTileSize * scaleFactor;

        float ppu = pixelsPerUnit > 0.01f ? pixelsPerUnit : 100f;

        DestroyPreviousFloorClipHierarchy();
        Transform oldGrid = floorTransform.Find(FloorTileGridName);
        if (oldGrid != null)
            Destroy(oldGrid.gameObject);

        float spanX = floorWorldBounds.size.x;
        float spanY = floorWorldBounds.size.y;
        int countX = Mathf.CeilToInt(spanX / cellStride.x) + 1;
        int countY = Mathf.CeilToInt(spanY / cellStride.y) + 1;
        if (countX < 1 || countY < 1)
            return;

        int floorLayerId = SortingLayer.NameToID(floorSortingLayerName);

        // Match the tiled floor art so the stretched backdrop fills any masked-edge seams.
        legacySr.sprite = tile;
        legacySr.color = Color.white;
        legacySr.drawMode = SpriteDrawMode.Tiled;
        Vector3 floorScale = floorTransform.lossyScale;
        legacySr.size = new Vector2(
            floorWorldBounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(floorScale.x)),
            floorWorldBounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(floorScale.y)));
        legacySr.enabled = true;
        legacySr.sortingLayerName = floorSortingLayerName;
        legacySr.sortingOrder = floorBackdropSortingOrder;
        legacySr.maskInteraction = SpriteMaskInteraction.None;

        GameObject clipRoot = new GameObject(FloorClipRootName);
        clipRoot.transform.SetParent(transform, false);
        clipRoot.transform.localPosition = Vector3.zero;
        clipRoot.transform.localRotation = Quaternion.identity;
        clipRoot.transform.localScale = Vector3.one;

        Sprite maskSprite = GetWhiteRectMaskSprite();
        GameObject maskGo = new GameObject(FloorInteriorMaskName);
        maskGo.transform.SetParent(clipRoot.transform, false);
        float maskMinX = SnapScalarToPpuGrid(floorWorldBounds.min.x, ppu);
        float maskMinY = SnapScalarToPpuGrid(floorWorldBounds.min.y, ppu);
        float tileStartX = SnapDownScalarToPpuGrid(floorWorldBounds.center.x - (countX * cellStride.x * 0.5f), ppu);
        float tileStartY = SnapDownScalarToPpuGrid(floorWorldBounds.center.y - (countY * cellStride.y * 0.5f), ppu);
        float z = floorWorldBounds.center.z;
        Vector3 maskCenter = new Vector3(
            maskMinX + spanX * 0.5f,
            maskMinY + spanY * 0.5f,
            z);
        maskGo.transform.position = maskCenter;
        maskGo.transform.rotation = Quaternion.identity;
        float mw = Mathf.Max(0.0001f, maskSprite.bounds.size.x);
        float mh = Mathf.Max(0.0001f, maskSprite.bounds.size.y);
        maskGo.transform.localScale = new Vector3(floorWorldBounds.size.x / mw, floorWorldBounds.size.y / mh, 1f);

        SpriteMask spriteMask = maskGo.AddComponent<SpriteMask>();
        spriteMask.sprite = maskSprite;
        spriteMask.isCustomRangeActive = true;
        spriteMask.frontSortingLayerID = floorLayerId;
        spriteMask.backSortingLayerID = floorLayerId;
        spriteMask.frontSortingOrder = floorMaskRangeFrontOrder;
        spriteMask.backSortingOrder = floorMaskRangeBackOrder;
        spriteMask.sortingLayerID = floorLayerId;
        spriteMask.sortingOrder = floorMaskSortingOrder;
        spriteMask.alphaCutoff = 0f;

        GameObject grid = new GameObject(FloorTileGridName);
        grid.transform.SetParent(clipRoot.transform, false);
        grid.transform.localPosition = Vector3.zero;
        grid.transform.localRotation = Quaternion.identity;
        grid.transform.localScale = Vector3.one;

        int lo = Mathf.Min(floorMaskRangeBackOrder, floorMaskRangeFrontOrder);
        int hi = Mathf.Max(floorMaskRangeBackOrder, floorMaskRangeFrontOrder);
        if (hi - lo < 2)
            hi = lo + 2;
        int tileOrder = Mathf.Clamp(floorTileSortingOrder, lo + 1, hi - 1);

        for (int x = 0; x < countX; x++)
        {
            for (int y = 0; y < countY; y++)
            {
                Vector3 bottomLeftWorld = new Vector3(
                    tileStartX + x * cellStride.x,
                    tileStartY + y * cellStride.y,
                    z);

                Vector3 minLocal = tile.bounds.min;
                Vector3 pivotWorld = bottomLeftWorld - new Vector3(
                    minLocal.x * scaleFactor,
                    minLocal.y * scaleFactor,
                    minLocal.z * scaleFactor);
                pivotWorld = SnapWorldPositionToPpuGrid(pivotWorld, ppu);

                GameObject cell = new GameObject($"{FloorTilePrefix}{x}_{y}");
                cell.transform.SetParent(grid.transform, false);
                cell.transform.position = pivotWorld;
                cell.transform.rotation = Quaternion.identity;
                float overlap = 1f;
                cell.transform.localScale = new Vector3(scaleFactor * overlap, scaleFactor * overlap, 1f);

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = tile;
                sr.drawMode = SpriteDrawMode.Simple;
                sr.sortingLayerID = floorLayerId;
                sr.sortingOrder = tileOrder;
                sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                sr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
        }
    }

    private void DestroyPreviousFloorClipHierarchy()
    {
        Transform clip = transform.Find(FloorClipRootName);
        if (clip != null)
            Destroy(clip.gameObject);
    }

    private static Sprite GetWhiteRectMaskSprite()
    {
        if (s_whiteRectMaskSprite != null)
            return s_whiteRectMaskSprite;

        Texture2D tex = Texture2D.whiteTexture;
        s_whiteRectMaskSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            1f);
        return s_whiteRectMaskSprite;
    }

    private static float SnapScalarToPpuGrid(float value, float pixelsPerUnit)
    {
        if (pixelsPerUnit < 0.0001f)
            return value;
        return Mathf.Round(value * pixelsPerUnit) / pixelsPerUnit;
    }

    private static float SnapDownScalarToPpuGrid(float value, float pixelsPerUnit)
    {
        if (pixelsPerUnit < 0.0001f)
            return value;
        return Mathf.Floor(value * pixelsPerUnit) / pixelsPerUnit;
    }

    private static Vector3 SnapWorldPositionToPpuGrid(Vector3 world, float pixelsPerUnit)
    {
        if (pixelsPerUnit < 0.0001f)
            return world;
        return new Vector3(
            SnapScalarToPpuGrid(world.x, pixelsPerUnit),
            SnapScalarToPpuGrid(world.y, pixelsPerUnit),
            world.z);
    }

    private void SetupRoomLabel(Bounds floorBounds, float pixelsPerUnit)
    {
        Transform labelTr = FindDeepChildByName(transform, RoomLabelName);
        if (labelTr == null)
            return;

        TMP_Text tmp = labelTr.GetComponent<TMP_Text>();
        if (tmp == null)
            return;

        if (!string.IsNullOrEmpty(tmp.text))
        {
            tmp.text = Regex.Replace(tmp.text, @"\s*\d+\s*[xX]\s*\d+\s*", " ", RegexOptions.IgnoreCase);
            tmp.text = Regex.Replace(tmp.text, @"\s+", " ").Trim();
            tmp.text = tmp.text.ToUpperInvariant();
        }

        tmp.enableVertexGradient = false;
        tmp.color = new Color32(0, 0, 0, 255);
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;

        RectTransform rt = labelTr as RectTransform;
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            float maxWidth = Mathf.Max(0.25f, floorBounds.size.x * 0.98f);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 4.2f);
        }

        float maxTextWidth = Mathf.Max(0.25f, floorBounds.size.x * 0.98f);
        FitLabelFontToRoomWidth(tmp, maxTextWidth);

        Vector2 roomSpecificOffset = GetRoomSpecificLabelOffset();
        Vector3 labelPos = new Vector3(
            floorBounds.center.x + labelOffsetFromRoomCenter.x + roomSpecificOffset.x,
            floorBounds.center.y + labelOffsetFromRoomCenter.y + roomSpecificOffset.y,
            labelTr.position.z);
        labelTr.position = SnapWorldPositionToPpuGrid(labelPos, pixelsPerUnit > 0.01f ? pixelsPerUnit : 100f);

        MeshRenderer meshRenderer = labelTr.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = labelSortingLayerName;
            meshRenderer.sortingOrder = labelSortingOrder;
            if (meshRenderer.material != null)
            {
                meshRenderer.material.color = Color.white;
            }
        }

        Material fontMat = tmp.fontMaterial;
        if (fontMat != null)
        {
            if (fontMat.HasProperty("_FaceColor"))
            {
                fontMat.SetColor("_FaceColor", new Color32(0, 0, 0, 255));
            }

            if (fontMat.HasProperty("_FaceDilate"))
            {
                fontMat.SetFloat("_FaceDilate", 0.28f);
            }
        }

        tmp.ForceMeshUpdate(true);
    }

    private Vector2 GetRoomSpecificLabelOffset()
    {
        if (!RoomLootSpawnTypeHelper.TryGetRoomType(transform, out RoomType roomType))
            return Vector2.zero;

        switch (roomType)
        {
            case RoomType.Kitchen:
                return new Vector2(0f, -1.35f);
            default:
                return Vector2.zero;
        }
    }

    private void FitLabelFontToRoomWidth(TMP_Text tmp, float maxWidthWorld)
    {
        if (maxWidthWorld <= 0.01f)
            return;

        float lo = Mathf.Max(0.5f, labelMinFontSize);
        float hi = Mathf.Max(lo + 0.01f, labelMaxFontSize);
        string text = tmp.text;
        if (string.IsNullOrEmpty(text))
            return;

        for (int i = 0; i < 22; i++)
        {
            float mid = (lo + hi) * 0.5f;
            tmp.fontSize = mid;
            float w = tmp.GetPreferredValues(text).x;
            if (w <= maxWidthWorld)
                lo = mid;
            else
                hi = mid;
        }

        tmp.fontSize = Mathf.Max(lo, 0.5f);
        tmp.ForceMeshUpdate(true);

        float finalW = tmp.GetPreferredValues(text).x;
        if (finalW > maxWidthWorld + 0.01f && tmp.rectTransform != null)
        {
            float s = maxWidthWorld / Mathf.Max(finalW, 0.01f);
            Vector3 ls = tmp.rectTransform.localScale;
            tmp.rectTransform.localScale = new Vector3(ls.x * s, ls.y * s, ls.z);
        }
    }

    private static Transform FindDeepChildByName(Transform root, string childName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName)
                return t;
        }

        return null;
    }
}
