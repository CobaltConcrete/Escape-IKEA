using System.Collections.Generic;
using UnityEngine;

public static class RoomPrefabStaticPlacer
{
    private const string DecorRootName = "Decorations";
    private const float DoorClearanceHalfWidth = 0.9f;
    private const float DoorClearanceDepth = 0.95f;
    private const float EdgeInset = 0.7f;
    private const float MinSeparation = 0.85f;

    public static bool PlaceRoomPrefabs(Transform roomRoot, RoomPrefabSpawnCatalog catalog)
    {
        if (roomRoot == null || catalog == null)
            return false;
        if (!RoomLootSpawnTypeHelper.TryGetRoomType(roomRoot, out RoomType roomType))
            return false;

        List<GameObject> prefabs = catalog.GetAllPrefabs(roomType);
        if (prefabs.Count == 0)
            return false;

        Transform existing = roomRoot.Find(DecorRootName);
        if (existing != null)
            Object.Destroy(existing.gameObject);
        GameObject decorRoot = new GameObject(DecorRootName);
        Transform spawnedItemsRoot = roomRoot.Find("SpawnedItems");
        decorRoot.transform.SetParent(spawnedItemsRoot != null ? spawnedItemsRoot : roomRoot, false);

        bool spawnedAny = false;
        List<Vector2> used = new List<Vector2>();

        if (roomType == RoomType.Bedroom)
            spawnedAny = PlaceBedroomLayout(roomRoot, decorRoot.transform, catalog, prefabs, used);
        else if (roomType == RoomType.Kitchen)
            spawnedAny = PlaceKitchenLayout(roomRoot, decorRoot.transform, prefabs, used);

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
                continue;
            if (roomType == RoomType.Bedroom && IsLegacyBedroomBed(prefab))
                continue;
            if (roomType == RoomType.Bedroom && IsBedroomLayoutPiece(prefab))
                continue;
            if (roomType == RoomType.Kitchen && IsKitchenLayoutPiece(prefab))
                continue;
            RoomSpawnPrefabDefinition def = prefab.GetComponent<RoomSpawnPrefabDefinition>();
            if (def == null)
                continue;
            if (def.spawnCategory == RoomSpawnCategory.Weapon)
                continue;

            Vector3 position = ResolvePlacement(roomRoot, roomType, prefab, i, used);
            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity, decorRoot.transform);
            instance.SetActive(false);
            StripLegacySpawnerPath(instance);
            NormalizeSpawnedVisuals(instance);
            ClampInstanceInsideRoom(instance, roomRoot);
            EnsureShoppingListPickupComponent(instance, def);
            instance.SetActive(true);
            used.Add(instance.transform.position);
            spawnedAny = true;
        }

        if (spawnedAny)
        {
            Room room = roomRoot.GetComponent<Room>();
            room?.RefreshRendererRegistry();
            RoomContentActivation.RefreshPlayerRoomsAfterMapSetup();
        }

        return spawnedAny;
    }

    private static bool PlaceKitchenLayout(
        Transform roomRoot,
        Transform parent,
        List<GameObject> kitchenPrefabs,
        List<Vector2> used)
    {
        GameObject microwavePrefab = FindByToken(kitchenPrefabs, "microwave");
        GameObject sinkPrefab = FindByToken(kitchenPrefabs, "sink");
        GameObject dishwasherPrefab = FindByToken(kitchenPrefabs, "dishwasher");

        List<GameObject> cabinetPrefabs = new List<GameObject>();
        for (int i = 0; i < kitchenPrefabs.Count; i++)
        {
            GameObject prefab = kitchenPrefabs[i];
            if (prefab == null)
                continue;
            if (HasToken(prefab.name, "cupboard") || HasToken(prefab.name, "cabinet"))
                cabinetPrefabs.Add(prefab);
        }

        bool spawnedAny = false;

        if (cabinetPrefabs.Count > 0)
        {
            Vector3[] cabinetSlots = new[]
            {
                RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(0.95f, -0.55f, 0f)),
                RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopCenter, new Vector3(-1.75f, -0.55f, 0f)),
                RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopCenter, new Vector3(1.75f, -0.55f, 0f)),
                RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopRight, new Vector3(-0.95f, -0.55f, 0f)),
            };

            for (int i = 0; i < cabinetSlots.Length; i++)
            {
                GameObject cabinetPrefab = cabinetPrefabs[i % cabinetPrefabs.Count];
                GameObject cabinet = SpawnConfiguredPrefab(cabinetPrefab, cabinetSlots[i], parent, roomRoot);
                if (cabinet == null)
                    continue;
                used.Add(cabinet.transform.position);
                spawnedAny = true;
            }
        }

        if (microwavePrefab != null)
        {
            Vector3 microwavePos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorTopCenter,
                new Vector3(0f, -0.45f, 0f));
            GameObject microwave = SpawnConfiguredPrefab(microwavePrefab, microwavePos, parent, roomRoot);
            if (microwave != null)
            {
                used.Add(microwave.transform.position);
                spawnedAny = true;
            }
        }

        if (sinkPrefab != null)
        {
            Vector3 sinkPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                new Vector3(0.55f, 0.15f, 0f));
            GameObject sink = SpawnConfiguredPrefab(sinkPrefab, sinkPos, parent, roomRoot);
            if (sink != null)
            {
                used.Add(sink.transform.position);
                spawnedAny = true;
            }
        }

        if (dishwasherPrefab != null)
        {
            Vector3 dishwasherPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                new Vector3(-0.55f, 0.15f, 0f));
            GameObject dishwasher = SpawnConfiguredPrefab(dishwasherPrefab, dishwasherPos, parent, roomRoot);
            if (dishwasher != null)
            {
                used.Add(dishwasher.transform.position);
                spawnedAny = true;
            }
        }

        return spawnedAny;
    }

    private static bool PlaceBedroomLayout(
        Transform roomRoot,
        Transform parent,
        RoomPrefabSpawnCatalog catalog,
        List<GameObject> bedroomPrefabs,
        List<Vector2> used)
    {
        List<GameObject> bedPrefabs = new List<GameObject>();
        List<GameObject> preferredBeds = new List<GameObject>();
        for (int i = 0; i < bedroomPrefabs.Count; i++)
        {
            GameObject p = bedroomPrefabs[i];
            if (p == null)
                continue;
            if (IsLegacyBedroomBed(p))
                continue;
            if (HasToken(p.name, "bed"))
                bedPrefabs.Add(p);
            if (IsPreferredBedroomBed(p))
                preferredBeds.Add(p);
        }

        if (preferredBeds.Count > 0)
            bedPrefabs = preferredBeds;

        if (bedPrefabs.Count == 0)
            return false;

        GameObject lampPrefab = FindByToken(bedroomPrefabs, "lamp");
        GameObject drawerPrefab = FindByToken(bedroomPrefabs, "drawer");
        if (catalog != null)
        {
            List<GameObject> living = catalog.GetAllPrefabs(RoomType.LivingRoom);
            if (lampPrefab == null)
                lampPrefab = FindByToken(living, "lamp");
            if (drawerPrefab == null)
                drawerPrefab = FindByToken(living, "drawer");
        }

        RoomDecorInteriorAnchor[] anchors = new[]
        {
            RoomDecorInteriorAnchor.InteriorTopLeft,
            RoomDecorInteriorAnchor.InteriorTopRight,
            RoomDecorInteriorAnchor.InteriorBottomLeft,
            RoomDecorInteriorAnchor.InteriorBottomRight
        };

        Vector3[] bedOffsets = new[]
        {
            new Vector3(0.86f, -0.72f, 0f),
            new Vector3(-0.86f, -0.72f, 0f),
            new Vector3(0.86f, 0.72f, 0f),
            new Vector3(-0.86f, 0.72f, 0f),
        };

        float[] bedScaleVariants = new[] { 1f, 0.95f, 1.08f, 1.02f };
        Vector3[] lampOffsets = new[]
        {
            new Vector3(1.45f, 0.2f, 0f),
            new Vector3(-1.45f, 0.2f, 0f),
            new Vector3(1.45f, -0.2f, 0f),
            new Vector3(-1.45f, -0.2f, 0f)
        };
        Vector3[] drawerOffsets = new[]
        {
            new Vector3(-1.35f, 0.15f, 0f),
            new Vector3(1.35f, 0.15f, 0f),
            new Vector3(-1.35f, -0.15f, 0f),
            new Vector3(1.35f, -0.15f, 0f)
        };
        bool spawnedAny = false;

        for (int i = 0; i < 4; i++)
        {
            GameObject bedPrefab = bedPrefabs[i % bedPrefabs.Count];
            Vector3 bedPos = RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, anchors[i], bedOffsets[i]);
            bedPos = ResolveBedroomLocalSlot(bedPos, used);
            GameObject bed = Object.Instantiate(bedPrefab, bedPos, Quaternion.identity, parent);
            bed.SetActive(false);
            StripLegacySpawnerPath(bed);
            NormalizeSpawnedVisuals(bed);
            ClampInstanceInsideRoom(bed, roomRoot);

            // Visual variation even when only one bed prefab exists.
            float s = bedScaleVariants[i % bedScaleVariants.Length];
            bed.transform.localScale = Vector3.Scale(bed.transform.localScale, new Vector3(s, s, 1f));
            SpriteRenderer bedRenderer = bed.GetComponentInChildren<SpriteRenderer>();
            if (bedRenderer != null)
                bedRenderer.flipX = (i % 2) == 1;

            RoomSpawnPrefabDefinition bedDef = bedPrefab.GetComponent<RoomSpawnPrefabDefinition>();
            EnsureShoppingListPickupComponent(bed, bedDef);
            bed.SetActive(true);
            used.Add(bed.transform.position);
            spawnedAny = true;

            Vector3 sideA = ResolveBedroomLocalSlot(bed.transform.position + lampOffsets[i], used);
            Vector3 sideB = ResolveBedroomLocalSlot(bed.transform.position + drawerOffsets[i], used);

            if (lampPrefab != null)
            {
                GameObject lamp = Object.Instantiate(lampPrefab, sideA, Quaternion.identity, parent);
                lamp.SetActive(false);
                StripLegacySpawnerPath(lamp);
                NormalizeSpawnedVisuals(lamp);
                ClampInstanceInsideRoom(lamp, roomRoot);
                EnsureShoppingListPickupComponent(lamp, lampPrefab.GetComponent<RoomSpawnPrefabDefinition>());
                lamp.SetActive(true);
                used.Add(lamp.transform.position);
                spawnedAny = true;
            }

            if (drawerPrefab != null)
            {
                GameObject drawer = Object.Instantiate(drawerPrefab, sideB, Quaternion.identity, parent);
                drawer.SetActive(false);
                StripLegacySpawnerPath(drawer);
                NormalizeSpawnedVisuals(drawer);
                ClampInstanceInsideRoom(drawer, roomRoot);
                EnsureShoppingListPickupComponent(drawer, drawerPrefab.GetComponent<RoomSpawnPrefabDefinition>());
                drawer.SetActive(true);
                used.Add(drawer.transform.position);
                spawnedAny = true;
            }
        }

        return spawnedAny;
    }

    private static Vector3 ResolvePlacement(Transform roomRoot, RoomType roomType, GameObject prefab, int index, List<Vector2> used)
    {
        if (roomType == RoomType.SportsRoom)
        {
            Vector3 sports = GetSportsPinnedPosition(roomRoot, prefab);
            if (sports != Vector3.zero)
                return sports;
        }
        else if (roomType == RoomType.LivingRoom)
        {
            Vector3 living = GetLivingRoomPinnedPosition(roomRoot, prefab);
            if (living != Vector3.zero)
                return living;
        }

        Vector3[] candidates = new Vector3[]
        {
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(EdgeInset, -EdgeInset, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopRight, new Vector3(-EdgeInset, -EdgeInset, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomLeft, new Vector3(EdgeInset, EdgeInset, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomRight, new Vector3(-EdgeInset, EdgeInset, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorMiddleLeft, new Vector3(EdgeInset, 0f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorMiddleRight, new Vector3(-EdgeInset, 0f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopCenter, new Vector3(0f, -EdgeInset, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomCenter, new Vector3(0f, EdgeInset, 0f))
        };

        for (int c = 0; c < candidates.Length; c++)
        {
            Vector3 p = candidates[(index + c) % candidates.Length];
            if (IsInsideDoorClearanceZone(p, roomRoot))
                continue;
            if (OverlapsUsed(p, used))
                continue;
            return p;
        }

        switch (roomType)
        {
            case RoomType.Bathroom:
                return RoomDecorationPlacer.GetAnchoredWorldPosition(
                    roomRoot, RoomDecorInteriorAnchor.InteriorBottomLeft, new Vector3(0.8f + index * 0.35f, 0.7f, 0f));
            case RoomType.Bedroom:
                return RoomDecorationPlacer.GetAnchoredWorldPosition(
                    roomRoot, RoomDecorInteriorAnchor.InteriorBottomRight, new Vector3(-0.8f - index * 0.35f, 0.7f, 0f));
            case RoomType.LivingRoom:
                return RoomDecorationPlacer.GetAnchoredWorldPosition(
                    roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(0.7f + index * 0.35f, -0.7f, 0f));
            case RoomType.SportsRoom:
                return RoomDecorationPlacer.GetAnchoredWorldPosition(
                    roomRoot, RoomDecorInteriorAnchor.InteriorTopRight, new Vector3(-0.7f - index * 0.35f, -0.7f, 0f));
            default:
                return roomRoot.position;
        }
    }

    private static Vector3 GetSportsPinnedPosition(Transform roomRoot, GameObject prefab)
    {
        if (roomRoot == null || prefab == null)
            return Vector3.zero;
        string name = prefab.name.ToLowerInvariant();
        if (name.Contains("weights"))
            return RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomLeft, new Vector3(0.72f, 0.62f, 0f));
        if (name.Contains("pennant"))
            return RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(0.72f, -0.58f, 0f));
        if (name.Contains("hoop"))
            return RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopRight, new Vector3(-0.82f, -0.22f, 0f));
        if (name.Contains("baseball"))
            return RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomRight, new Vector3(-0.72f, 0.62f, 0f));
        if (name.Contains("bat"))
            return RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorMiddleRight, new Vector3(-1.12f, -0.08f, 0f));
        return Vector3.zero;
    }

    private static Vector3 GetLivingRoomPinnedPosition(Transform roomRoot, GameObject prefab)
    {
        if (roomRoot == null || prefab == null)
            return Vector3.zero;

        string name = prefab.name.ToLowerInvariant();
        if (name.Contains("coffee") && name.Contains("table"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                Vector3.zero);
        }

        return Vector3.zero;
    }

    private static bool OverlapsUsed(Vector2 p, List<Vector2> used)
    {
        if (used == null)
            return false;
        for (int i = 0; i < used.Count; i++)
        {
            if (Vector2.Distance(p, used[i]) < MinSeparation)
                return true;
        }

        return false;
    }

    private static bool HasToken(string source, string token)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static GameObject FindByToken(List<GameObject> prefabs, string token)
    {
        if (prefabs == null)
            return null;
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject p = prefabs[i];
            if (p == null)
                continue;
            if (HasToken(p.name, token))
                return p;
        }

        return null;
    }

    private static bool IsBedroomLayoutPiece(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return HasToken(prefab.name, "bed") ||
               HasToken(prefab.name, "lamp") ||
               HasToken(prefab.name, "drawer");
    }

    private static bool IsKitchenLayoutPiece(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return HasToken(prefab.name, "microwave") ||
               HasToken(prefab.name, "sink") ||
               HasToken(prefab.name, "dishwasher") ||
               HasToken(prefab.name, "cupboard") ||
               HasToken(prefab.name, "cabinet");
    }

    private static bool IsPreferredBedroomBed(GameObject prefab)
    {
        if (prefab == null)
            return false;

        string n = prefab.name;
        return HasToken(n, "bed bee") ||
               HasToken(n, "bed cherry") ||
               HasToken(n, "bed night owl");
    }

    private static bool IsLegacyBedroomBed(GameObject prefab)
    {
        if (prefab == null)
            return false;

        string n = prefab.name == null ? string.Empty : prefab.name.Trim();
        return string.Equals(n, "Bed", System.StringComparison.OrdinalIgnoreCase);
    }

    private static Vector3 ResolveBedroomLocalSlot(Vector3 preferred, List<Vector2> used)
    {
        if (!OverlapsUsed(preferred, used))
            return preferred;

        Vector3[] offsets =
        {
            new Vector3(0f, 0.36f, 0f),
            new Vector3(0f, -0.36f, 0f),
            new Vector3(0.32f, 0f, 0f),
            new Vector3(-0.32f, 0f, 0f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 alt = preferred + offsets[i];
            if (!OverlapsUsed(alt, used))
                return alt;
        }

        return preferred;
    }

    private static bool IsInsideDoorClearanceZone(Vector2 worldPoint, Transform roomRoot)
    {
        if (roomRoot == null)
            return false;

        Collider2D best = null;
        float bestArea = 0f;
        Collider2D[] colliders = roomRoot.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D c = colliders[i];
            if (c == null || !c.isTrigger)
                continue;
            float a = c.bounds.size.x * c.bounds.size.y;
            if (a > bestArea)
            {
                bestArea = a;
                best = c;
            }
        }

        if (best == null)
            return false;

        Vector3 minL = roomRoot.InverseTransformPoint(best.bounds.min);
        Vector3 maxL = roomRoot.InverseTransformPoint(best.bounds.max);
        Vector3 pL = roomRoot.InverseTransformPoint(worldPoint);
        Vector3 cL = (minL + maxL) * 0.5f;

        bool inTopDoor = Mathf.Abs(pL.x - cL.x) <= DoorClearanceHalfWidth && Mathf.Abs(pL.y - maxL.y) <= DoorClearanceDepth;
        bool inBottomDoor = Mathf.Abs(pL.x - cL.x) <= DoorClearanceHalfWidth && Mathf.Abs(pL.y - minL.y) <= DoorClearanceDepth;
        bool inLeftDoor = Mathf.Abs(pL.y - cL.y) <= DoorClearanceHalfWidth && Mathf.Abs(pL.x - minL.x) <= DoorClearanceDepth;
        bool inRightDoor = Mathf.Abs(pL.y - cL.y) <= DoorClearanceHalfWidth && Mathf.Abs(pL.x - maxL.x) <= DoorClearanceDepth;

        return inTopDoor || inBottomDoor || inLeftDoor || inRightDoor;
    }

    private static void ClampInstanceInsideRoom(GameObject instance, Transform roomRoot)
    {
        if (instance == null || roomRoot == null)
            return;

        Collider2D best = null;
        float bestArea = 0f;
        Collider2D[] roomCols = roomRoot.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < roomCols.Length; i++)
        {
            Collider2D c = roomCols[i];
            if (c == null || !c.isTrigger)
                continue;
            float a = c.bounds.size.x * c.bounds.size.y;
            if (a > bestArea)
            {
                bestArea = a;
                best = c;
            }
        }

        if (best == null)
            return;

        Renderer[] rs = instance.GetComponentsInChildren<Renderer>(true);
        if (rs == null || rs.Length == 0)
            return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
            b.Encapsulate(rs[i].bounds);

        float minX = best.bounds.min.x + b.extents.x + 0.05f;
        float maxX = best.bounds.max.x - b.extents.x - 0.05f;
        float minY = best.bounds.min.y + b.extents.y + 0.05f;
        float maxY = best.bounds.max.y - b.extents.y - 0.05f;

        if (minX > maxX)
        {
            float cx = (best.bounds.min.x + best.bounds.max.x) * 0.5f;
            minX = cx;
            maxX = cx;
        }

        if (minY > maxY)
        {
            float cy = (best.bounds.min.y + best.bounds.max.y) * 0.5f;
            minY = cy;
            maxY = cy;
        }

        Vector3 p = instance.transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        instance.transform.position = p;
    }

    private static void EnsureShoppingListPickupComponent(GameObject instance, RoomSpawnPrefabDefinition def)
    {
        if (instance == null || def == null)
            return;
        string key = def.shoppingListKey;
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (instance.GetComponent<RoomGeneratedPickup>() == null)
            instance.AddComponent<RoomGeneratedPickup>();
    }

    private static void StripLegacySpawnerPath(GameObject instance)
    {
        if (instance == null)
            return;

        ItemWorldSpawner[] spawners = instance.GetComponentsInChildren<ItemWorldSpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
                Object.DestroyImmediate(spawners[i]);
        }
    }

    private static void NormalizeSpawnedVisuals(GameObject instance)
    {
        if (instance == null)
            return;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
                continue;
            sr.sortingLayerName = "Item";
            if (sr.sortingOrder < 2)
                sr.sortingOrder = 2;
        }
    }

    private static GameObject SpawnConfiguredPrefab(GameObject prefab, Vector3 position, Transform parent, Transform roomRoot)
    {
        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity, parent);
        instance.SetActive(false);
        StripLegacySpawnerPath(instance);
        NormalizeSpawnedVisuals(instance);
        ClampInstanceInsideRoom(instance, roomRoot);
        EnsureShoppingListPickupComponent(instance, prefab.GetComponent<RoomSpawnPrefabDefinition>());
        instance.SetActive(true);
        return instance;
    }
}
