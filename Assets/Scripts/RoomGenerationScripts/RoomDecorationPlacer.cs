using System.Collections.Generic;
using UnityEngine;

public static class RoomDecorationPlacer
{
    private const string DecorRootName = "Decorations";
    private const string SpawnedItemsName = "SpawnedItems";
    private const float DefaultInteriorAnchorInset = 0.95f;

    private const float WallPadding = 0.65f;
    private const float PlacementGrid = 0.25f;
    private const float DoorClearanceHalfWidth = 0.9f;
    private const float DoorClearanceDepth = 0.95f;
    private const float DecorationColliderScale = 0.78f;
    private const float MinPropScale = 0.8f;
    private const float MaxPropScale = 1.2f;

    private static bool s_loggedMissingLootArea;

    public static void Place(Transform roomRoot, RoomDecorationCatalog catalog)
    {
        if (roomRoot == null || catalog == null || catalog.entries == null)
            return;

        if (!RoomLootSpawnTypeHelper.TryGetRoomType(roomRoot.transform, out RoomType roomType))
        {
            if (!s_loggedMissingLootArea)
            {
                s_loggedMissingLootArea = true;
                Debug.LogWarning(
                    $"RoomDecorationPlacer: No {nameof(LootSpawnArea)} under room '{roomRoot.name}'. Skipping decorations.",
                    roomRoot);
            }

            return;
        }

        Transform existing = roomRoot.Find(DecorRootName);
        if (existing != null)
            Object.Destroy(existing.gameObject);

        GameObject decorRoot = new GameObject(DecorRootName);
        decorRoot.transform.SetParent(roomRoot, false);
        decorRoot.transform.localPosition = Vector3.zero;
        decorRoot.transform.localRotation = Quaternion.identity;
        decorRoot.transform.localScale = Vector3.one;
        decorRoot.layer = 0;

        Room room = roomRoot.GetComponent<Room>();
        Transform pickupParent = roomRoot.Find(SpawnedItemsName);

        if (roomType == RoomType.LivingRoom)
        {
            PlaceLivingRoomLayout(roomRoot, decorRoot.transform, catalog);
            room?.RefreshRendererRegistry();
            return;
        }
        if (roomType == RoomType.Bedroom)
        {
            PlaceBedroomLayout(roomRoot, decorRoot.transform, catalog);
            room?.RefreshRendererRegistry();
            return;
        }

        bool hasInteriorBounds = TryGetLargestTriggerLocalAabb(roomRoot, out Vector3 localMin, out Vector3 localMax);
        List<Bounds> occupiedLocalBounds = new List<Bounds>();

        foreach (RoomDecorationCatalog.DecorationEntry entry in catalog.entries)
        {
            if (entry == null)
                continue;

            if (entry.roomType != roomType)
                continue;

            Vector3 resolvedLocal = ResolveEntryLocalPosition(roomRoot, entry);
            Vector3 entryScale = ResolveEntryScale(entry);
            Vector2 halfSize = EstimateHalfSize(entry, entryScale);
            Vector3 placedLocal = ResolveNonOverlappingPosition(
                resolvedLocal,
                halfSize,
                occupiedLocalBounds,
                hasInteriorBounds,
                localMin,
                localMax);

            if (entry.catalogPickup != null)
            {
                bool spawnPickup = ShouldSpawnCatalogPickup(entry);
                if (spawnPickup &&
                    !RoomItemWorldQuery.RoomHasDefinitionInPickupScopes(roomRoot.gameObject, entry.catalogPickup))
                {
                    TrySpawnCatalogPickup(roomRoot, room, pickupParent, entry, placedLocal);
                    occupiedLocalBounds.Add(BuildLocalBounds(placedLocal, halfSize));
                }
                else if (entry.sprite != null)
                {
                    // Not on list, or this room already has the pickup: still show decor (extra rows / duplicates).
                    SpawnDecorationSprite(decorRoot.transform, entry, placedLocal, entryScale);
                    occupiedLocalBounds.Add(BuildLocalBounds(placedLocal, halfSize));
                }

                continue;
            }

            if (entry.sprite == null)
                continue;

            SpawnDecorationSprite(decorRoot.transform, entry, placedLocal, entryScale);
            occupiedLocalBounds.Add(BuildLocalBounds(placedLocal, halfSize));
        }

        room?.RefreshRendererRegistry();
    }

    private static void PlaceLivingRoomLayout(Transform roomRoot, Transform decorRoot, RoomDecorationCatalog catalog)
    {
        if (roomRoot == null || decorRoot == null || catalog == null)
            return;
        if (!TryGetLargestTriggerLocalAabb(roomRoot, out Vector3 minL, out Vector3 maxL))
            return;

        float left = minL.x + WallPadding;
        float right = maxL.x - WallPadding;
        float top = maxL.y - WallPadding;
        float bottom = minL.y + WallPadding;
        float midX = (left + right) * 0.5f;
        float midY = (bottom + top) * 0.5f;

        Sprite couchSprite = FindSpriteByName(catalog, "Couch");
        Sprite couchAfterSprite = FindSpriteByName(catalog, "Couchafter");
        Sprite cushionSprite = FindSpriteByName(catalog, "Cushion");
        Sprite coffeeSprite = FindSpriteByName(catalog, "Coffeetable");
        Sprite remoteSprite = FindSpriteByName(catalog, "Remote");
        Sprite cabinetSprite = FindSpriteByName(catalog, "Cabinet");
        Sprite pictureSprite = FindSpriteByName(catalog, "Picture");
        Sprite curtainSprite = FindSpriteByName(catalog, "Curtain");
        Sprite plantSprite = FindSpriteByName(catalog, "Houseplant");
        Sprite lampSprite = FindSpriteByName(catalog, "Lamp");

        ItemDefinition cabinetLoot = FindLivingLootDefinition(catalog, "Cabinet");
        ItemDefinition couchLoot = FindLivingLootDefinition(catalog, "Couch");

        Vector3 couchPos = new Vector3(midX, bottom + 0.85f, 0f);
        Vector3 tablePos = new Vector3(midX, couchPos.y + 1.0f, 0f);
        Vector3 cabinetPos = new Vector3(left + 1.0f, top - 0.45f, 0f);
        Vector3 curtainPos = new Vector3(midX, top - 0.1f, 0f);
        Vector3 plantPos = new Vector3(left + 0.45f, bottom + 0.55f, 0f);
        Vector3 lampPos = new Vector3(right - 0.45f, bottom + 0.55f, 0f);

        GameObject couch = SpawnLayoutSprite(decorRoot, "Living_Couch", couchSprite, couchPos, 8);
        SpriteRenderer couchRenderer = couch != null ? couch.GetComponent<SpriteRenderer>() : null;

        if (couch != null)
        {
            Vector3 cushionPos = new Vector3(0.12f, 0.35f, 0f);
            SpawnContainerChild(couch.transform, "Living_Cushion", cushionSprite, cushionPos, 9, couchLoot, "Cushion", couchRenderer, couchAfterSprite);
        }

        GameObject table = SpawnLayoutSprite(decorRoot, "Living_Coffeetable", coffeeSprite, tablePos, 8);
        if (table != null)
        {
            Vector3 remotePos = new Vector3(0.1f, 0.2f, 0f);
            SpawnLayoutSprite(table.transform, "Living_Remote", remoteSprite, remotePos, 9);
        }

        GameObject cabinet = SpawnLayoutSprite(decorRoot, "Living_Cabinet", cabinetSprite, cabinetPos, 8);
        if (cabinet != null)
        {
            Vector3 picturePos = new Vector3(0f, 0.45f, 0f);
            SpawnContainerChild(cabinet.transform, "Living_Picture", pictureSprite, picturePos, 9, cabinetLoot, "Picture", null, null);
        }

        SpawnLayoutSprite(decorRoot, "Living_Curtain", curtainSprite, curtainPos, 8);
        SpawnLayoutSprite(decorRoot, "Living_Houseplant", plantSprite, plantPos, 8);
        SpawnLayoutSprite(decorRoot, "Living_Lamp", lampSprite, lampPos, 8);
    }

    private static GameObject SpawnLayoutSprite(Transform parent, string name, Sprite sprite, Vector3 localPos, int sortingOrder)
    {
        if (parent == null || sprite == null)
            return null;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.layer = 0;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Floor";
        sr.sortingOrder = sortingOrder;
        AddBlockingCollider(go, sprite);
        return go;
    }

    private static void SpawnContainerChild(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 localPos,
        int sortingOrder,
        ItemDefinition lootDef,
        string displayName,
        SpriteRenderer couchRendererToSwap,
        Sprite couchAfterSprite)
    {
        GameObject child = SpawnLayoutSprite(parent, name, sprite, localPos, sortingOrder);
        if (child == null)
            return;

        BoxCollider2D c = child.AddComponent<BoxCollider2D>();
        c.isTrigger = false;
        c.size = Vector2.one * 0.45f;

        LivingRoomContainerPickup p = child.AddComponent<LivingRoomContainerPickup>();
        p.Configure(lootDef, 1, displayName);
        if (couchRendererToSwap != null && couchAfterSprite != null)
            p.ConfigureCouchSwap(couchRendererToSwap, couchAfterSprite);
    }

    private static Sprite FindSpriteByName(RoomDecorationCatalog catalog, string spriteName)
    {
        if (catalog == null || catalog.entries == null || string.IsNullOrEmpty(spriteName))
            return null;

        for (int i = 0; i < catalog.entries.Count; i++)
        {
            RoomDecorationCatalog.DecorationEntry e = catalog.entries[i];
            if (e?.sprite == null)
                continue;
            if (string.Equals(e.sprite.name, spriteName, System.StringComparison.OrdinalIgnoreCase) ||
                e.sprite.name.StartsWith(spriteName + "_", System.StringComparison.OrdinalIgnoreCase))
                return e.sprite;
        }

        return null;
    }

    private static ItemDefinition FindLivingLootDefinition(RoomDecorationCatalog catalog, string shoppingListKey)
    {
        if (catalog == null || catalog.entries == null || string.IsNullOrEmpty(shoppingListKey))
            return null;

        for (int i = 0; i < catalog.entries.Count; i++)
        {
            RoomDecorationCatalog.DecorationEntry e = catalog.entries[i];
            if (e?.catalogPickup == null)
                continue;
            if (!string.Equals(e.catalogPickup.GetShoppingListKey(), shoppingListKey, System.StringComparison.Ordinal))
                continue;
            if (e.roomType != RoomType.LivingRoom)
                continue;
            return e.catalogPickup;
        }

        return null;
    }

    private static void PlaceBedroomLayout(Transform roomRoot, Transform decorRoot, RoomDecorationCatalog catalog)
    {
        if (roomRoot == null || decorRoot == null || catalog == null)
            return;
        if (!TryGetLargestTriggerLocalAabb(roomRoot, out Vector3 minL, out Vector3 maxL))
            return;

        float left = minL.x + WallPadding;
        float right = maxL.x - WallPadding;
        float top = maxL.y - WallPadding;
        float bottom = minL.y + WallPadding;

        float x0 = Mathf.Lerp(left, right, 0.3f);
        float x1 = Mathf.Lerp(left, right, 0.7f);
        float y0 = Mathf.Lerp(bottom, top, 0.35f);
        float y1 = Mathf.Lerp(bottom, top, 0.68f);

        ItemDefinition bedLoot = FindLootDefinitionByKey(catalog, "Bed", RoomType.Bedroom);
        ItemDefinition lampLoot = FindLootDefinitionByKey(catalog, "Lamp", RoomType.None);
        ItemDefinition drawerLoot = FindLootDefinitionByKey(catalog, "Drawer", RoomType.None);
        ItemDefinition beePlushLoot = FindLootDefinitionByKey(catalog, "Bee_Plush", RoomType.None);
        ItemDefinition owlPlushLoot = FindLootDefinitionByKey(catalog, "Owl_Plush", RoomType.None);

        Sprite bedBee = FindSpriteByName(catalog, "Bed_Bee");
        Sprite bedBeeNo = FindSpriteByName(catalog, "Bed_Bee_No_Plush");
        Sprite bedOwl = FindSpriteByName(catalog, "Bed_Night_Owl");
        Sprite bedOwlNo = FindSpriteByName(catalog, "Bed_Night_No_Owl");
        Sprite beePlush = FindSpriteByName(catalog, "Bee_Plush");
        Sprite owlPlush = FindSpriteByName(catalog, "Owl_Plush");
        Sprite lampSprite = FindSpriteByName(catalog, "Lamp_0");
        if (lampSprite == null) lampSprite = FindSpriteByName(catalog, "Lamp");
        Sprite drawerSprite = FindSpriteByName(catalog, "Drawer (1)_0");
        if (drawerSprite == null) drawerSprite = FindSpriteByName(catalog, "Drawer");

        Vector3[] bedPositions = new Vector3[]
        {
            new Vector3(x0, y0, 0f),
            new Vector3(x1, y0, 0f),
            new Vector3(x0, y1, 0f),
            new Vector3(x1, y1, 0f)
        };

        List<GameObject> sideCandidates = new List<GameObject>();
        for (int i = 0; i < bedPositions.Length; i++)
        {
            bool useBee = (i % 2 == 0);
            bool hidePlushBed = Random.value > 0.5f;

            Sprite bedSprite = useBee
                ? (hidePlushBed ? bedBeeNo : bedBee)
                : (hidePlushBed ? bedOwlNo : bedOwl);
            if (bedSprite == null)
                bedSprite = useBee ? bedBee : bedOwl;

            GameObject bed = SpawnLayoutSprite(decorRoot, $"Bedroom_Bed_{i}", bedSprite, bedPositions[i], 8);
            if (bed == null)
                continue;

            BoxCollider2D bedCol = bed.AddComponent<BoxCollider2D>();
            bedCol.isTrigger = false;
            bedCol.size = new Vector2(1.05f, 0.75f);

            Sprite plushSprite = useBee ? beePlush : owlPlush;
            ItemDefinition plushDef = useBee ? beePlushLoot : owlPlushLoot;
            GameObject plush = SpawnLayoutSprite(
                bed.transform,
                useBee ? $"BeePlush_{i}" : $"OwlPlush_{i}",
                plushSprite,
                new Vector3(0f, 0.18f, 0f),
                9);

            SpriteRenderer plushSr = plush != null ? plush.GetComponent<SpriteRenderer>() : null;
            BoxCollider2D plushCol = null;
            if (plush != null)
            {
                plushCol = plush.AddComponent<BoxCollider2D>();
                plushCol.isTrigger = false;
                plushCol.size = new Vector2(0.32f, 0.28f);
            }

            BedroomBedContainerPickup bedInteract = bed.AddComponent<BedroomBedContainerPickup>();
            bedInteract.Configure(plushDef, plushSr, plushCol);

            bool lampSide = (i % 2 == 0);
            Sprite sideSprite = lampSide ? lampSprite : drawerSprite;
            Vector3 sideLocal = new Vector3(i % 2 == 0 ? 0.6f : -0.6f, -0.02f, 0f);
            GameObject side = SpawnLayoutSprite(decorRoot, lampSide ? $"Bedroom_Lamp_{i}" : $"Bedroom_Drawer_{i}", sideSprite, bedPositions[i] + sideLocal, 8);
            if (side != null)
            {
                BoxCollider2D sideCol = side.AddComponent<BoxCollider2D>();
                sideCol.isTrigger = false;
                sideCol.size = new Vector2(0.55f, 0.5f);
                sideCandidates.Add(side);
            }
        }

        ShuffleGameObjects(sideCandidates);
        int collectibleCount = Mathf.Min(sideCandidates.Count, Random.Range(2, 5));
        for (int i = 0; i < collectibleCount; i++)
        {
            GameObject side = sideCandidates[i];
            if (side == null)
                continue;

            bool isLamp = side.name.Contains("Lamp");
            ItemDefinition def = isLamp ? lampLoot : drawerLoot;
            LivingRoomContainerPickup pickup = side.AddComponent<LivingRoomContainerPickup>();
            pickup.Configure(def, 1, isLamp ? "Lamp" : "Drawer");
        }

        if (bedLoot != null)
        {
            // Beds remain non-collectible by design; this keeps definition intentionally referenced for future systems.
        }
    }

    private static ItemDefinition FindLootDefinitionByKey(RoomDecorationCatalog catalog, string shoppingListKey, RoomType roomType)
    {
        if (catalog == null || catalog.entries == null || string.IsNullOrEmpty(shoppingListKey))
            return null;

        for (int i = 0; i < catalog.entries.Count; i++)
        {
            RoomDecorationCatalog.DecorationEntry e = catalog.entries[i];
            if (e?.catalogPickup == null)
                continue;
            if (!string.Equals(e.catalogPickup.GetShoppingListKey(), shoppingListKey, System.StringComparison.Ordinal))
                continue;
            if (roomType != RoomType.None && e.roomType != roomType)
                continue;
            return e.catalogPickup;
        }

        return null;
    }


    private static void ShuffleGameObjects(List<GameObject> list)
    {
        if (list == null || list.Count < 2)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GameObject tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private static bool ShouldSpawnCatalogPickup(RoomDecorationCatalog.DecorationEntry entry)
    {
        if (entry == null || entry.catalogPickup == null)
            return false;

        if (!entry.pickupOnlyWhenOnShoppingList)
            return true;

        RunObjectiveManager rom = RunObjectiveManager.Instance;
        if (rom == null)
            return false;

        return rom.ContainsShoppingListKey(entry.catalogPickup.GetShoppingListKey());
    }

    private static void SpawnDecorationSprite(
        Transform decorRoot,
        RoomDecorationCatalog.DecorationEntry entry,
        Vector3 resolvedLocal,
        Vector3 resolvedScale)
    {
        if (decorRoot == null || entry == null || entry.sprite == null)
            return;

        GameObject go = new GameObject(entry.sprite.name);
        go.transform.SetParent(decorRoot, false);
        go.transform.localPosition = resolvedLocal;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = resolvedScale;
        go.layer = 0;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = entry.sprite;
        sr.sortingLayerName = entry.sortingLayerName;
        sr.sortingOrder = entry.sortingOrder;
        AddBlockingCollider(go, entry.sprite);
    }

    private static void AddBlockingCollider(GameObject target, Sprite sprite)
    {
        if (target == null || sprite == null)
            return;

        BoxCollider2D box = target.GetComponent<BoxCollider2D>();
        if (box == null)
            box = target.AddComponent<BoxCollider2D>();

        box.isTrigger = false;
        Vector2 spriteSize = sprite.bounds.size;
        box.size = new Vector2(
            Mathf.Max(0.18f, spriteSize.x * DecorationColliderScale),
            Mathf.Max(0.18f, spriteSize.y * DecorationColliderScale));
        box.offset = Vector2.zero;
    }

    /// <summary>World-space position for a sports-room bat (or other scripted pickups) using the same anchor rules as catalog rows.</summary>
    public static Vector3 GetAnchoredWorldPosition(
        Transform roomRoot,
        RoomDecorInteriorAnchor anchor,
        Vector3 offsetFromAnchor,
        float insetOverride = 0f)
    {
        if (roomRoot == null)
            return offsetFromAnchor;

        float inset = insetOverride > 0.0001f ? insetOverride : DefaultInteriorAnchorInset;
        return roomRoot.TransformPoint(ResolveAnchoredLocalPosition(roomRoot, anchor, offsetFromAnchor, inset));
    }

    public static Vector3 ResolveEntryLocalPosition(Transform roomRoot, RoomDecorationCatalog.DecorationEntry entry)
    {
        if (entry == null || roomRoot == null)
            return Vector3.zero;

        float inset = entry.interiorAnchorInset > 0.0001f ? entry.interiorAnchorInset : DefaultInteriorAnchorInset;
        Vector3 p = ResolveAnchoredLocalPosition(roomRoot, entry.interiorAnchor, entry.localPosition, inset);
        if (entry.randomLocalOffsetRadius > 0.0001f)
        {
            Vector2 r = Random.insideUnitCircle * entry.randomLocalOffsetRadius;
            p.x += r.x;
            p.y += r.y;
        }

        p.x = Mathf.Round(p.x / PlacementGrid) * PlacementGrid;
        p.y = Mathf.Round(p.y / PlacementGrid) * PlacementGrid;
        p.z = 0f;
        return p;
    }

    private static Vector3 ResolveEntryScale(RoomDecorationCatalog.DecorationEntry entry)
    {
        Vector3 baseScale = entry != null && entry.localScale != Vector3.zero ? entry.localScale : Vector3.one;
        float sx = Mathf.Clamp(baseScale.x, MinPropScale, MaxPropScale);
        float sy = Mathf.Clamp(baseScale.y, MinPropScale, MaxPropScale);
        return new Vector3(sx, sy, 1f);
    }

    private static Vector2 EstimateHalfSize(RoomDecorationCatalog.DecorationEntry entry, Vector3 scale)
    {
        if (entry?.sprite != null)
        {
            Vector2 s = entry.sprite.bounds.size;
            return new Vector2(Mathf.Max(0.2f, s.x * scale.x * 0.5f), Mathf.Max(0.2f, s.y * scale.y * 0.5f));
        }

        if (entry?.catalogPickup?.icon != null)
        {
            Vector2 s = entry.catalogPickup.icon.bounds.size;
            return new Vector2(Mathf.Max(0.2f, s.x * 0.5f), Mathf.Max(0.2f, s.y * 0.5f));
        }

        return new Vector2(0.35f, 0.35f);
    }

    private static Bounds BuildLocalBounds(Vector3 center, Vector2 halfSize)
    {
        return new Bounds(
            new Vector3(center.x, center.y, 0f),
            new Vector3(halfSize.x * 2f, halfSize.y * 2f, 0.2f));
    }

    private static Vector3 ResolveNonOverlappingPosition(
        Vector3 desiredLocal,
        Vector2 halfSize,
        List<Bounds> occupiedLocalBounds,
        bool hasInteriorBounds,
        Vector3 localMin,
        Vector3 localMax)
    {
        Vector3[] candidateOffsets = new Vector3[]
        {
            Vector3.zero,
            new Vector3(PlacementGrid, 0f, 0f),
            new Vector3(-PlacementGrid, 0f, 0f),
            new Vector3(0f, PlacementGrid, 0f),
            new Vector3(0f, -PlacementGrid, 0f),
            new Vector3(PlacementGrid, PlacementGrid, 0f),
            new Vector3(PlacementGrid, -PlacementGrid, 0f),
            new Vector3(-PlacementGrid, PlacementGrid, 0f),
            new Vector3(-PlacementGrid, -PlacementGrid, 0f),
            new Vector3(PlacementGrid * 2f, 0f, 0f),
            new Vector3(-PlacementGrid * 2f, 0f, 0f),
            new Vector3(0f, PlacementGrid * 2f, 0f),
            new Vector3(0f, -PlacementGrid * 2f, 0f)
        };

        Vector3 fallback = desiredLocal;
        for (int i = 0; i < candidateOffsets.Length; i++)
        {
            Vector3 candidate = desiredLocal + candidateOffsets[i];
            if (hasInteriorBounds)
                candidate = ClampInsideBounds(candidate, halfSize, localMin, localMax);

            Bounds b = BuildLocalBounds(candidate, halfSize);
            if (!IntersectsAny(b, occupiedLocalBounds) &&
                (!hasInteriorBounds || !IntersectsDoorClearance(b, localMin, localMax)))
                return candidate;

            fallback = candidate;
        }

        return fallback;
    }

    private static bool IntersectsAny(Bounds b, List<Bounds> occupiedLocalBounds)
    {
        if (occupiedLocalBounds == null)
            return false;

        for (int i = 0; i < occupiedLocalBounds.Count; i++)
        {
            if (b.Intersects(occupiedLocalBounds[i]))
                return true;
        }

        return false;
    }

    private static bool IntersectsDoorClearance(Bounds b, Vector3 localMin, Vector3 localMax)
    {
        Vector3 c = (localMin + localMax) * 0.5f;

        Bounds topDoor = new Bounds(
            new Vector3(c.x, localMax.y - DoorClearanceDepth * 0.5f, 0f),
            new Vector3(DoorClearanceHalfWidth * 2f, DoorClearanceDepth, 0.2f));
        Bounds bottomDoor = new Bounds(
            new Vector3(c.x, localMin.y + DoorClearanceDepth * 0.5f, 0f),
            new Vector3(DoorClearanceHalfWidth * 2f, DoorClearanceDepth, 0.2f));
        Bounds leftDoor = new Bounds(
            new Vector3(localMin.x + DoorClearanceDepth * 0.5f, c.y, 0f),
            new Vector3(DoorClearanceDepth, DoorClearanceHalfWidth * 2f, 0.2f));
        Bounds rightDoor = new Bounds(
            new Vector3(localMax.x - DoorClearanceDepth * 0.5f, c.y, 0f),
            new Vector3(DoorClearanceDepth, DoorClearanceHalfWidth * 2f, 0.2f));

        return b.Intersects(topDoor) || b.Intersects(bottomDoor) || b.Intersects(leftDoor) || b.Intersects(rightDoor);
    }

    private static Vector3 ClampInsideBounds(Vector3 p, Vector2 halfSize, Vector3 localMin, Vector3 localMax)
    {
        float minX = localMin.x + WallPadding + halfSize.x;
        float maxX = localMax.x - WallPadding - halfSize.x;
        float minY = localMin.y + WallPadding + halfSize.y;
        float maxY = localMax.y - WallPadding - halfSize.y;

        if (minX > maxX)
        {
            float cx = (localMin.x + localMax.x) * 0.5f;
            minX = cx;
            maxX = cx;
        }

        if (minY > maxY)
        {
            float cy = (localMin.y + localMax.y) * 0.5f;
            minY = cy;
            maxY = cy;
        }

        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        p.x = Mathf.Round(p.x / PlacementGrid) * PlacementGrid;
        p.y = Mathf.Round(p.y / PlacementGrid) * PlacementGrid;
        p.z = 0f;
        return p;
    }

    private static Vector3 ResolveAnchoredLocalPosition(
        Transform roomRoot,
        RoomDecorInteriorAnchor anchor,
        Vector3 offset,
        float inset)
    {
        if (anchor == RoomDecorInteriorAnchor.None)
            return offset;

        if (!TryGetLargestTriggerLocalAabb(roomRoot, out Vector3 minL, out Vector3 maxL))
            return offset;

        Vector3 c = (minL + maxL) * 0.5f;
        Vector3 anchorPoint = Vector3.zero;

        switch (anchor)
        {
            case RoomDecorInteriorAnchor.InteriorTopLeft:
                anchorPoint = new Vector3(minL.x + inset, maxL.y - inset, 0f);
                break;
            case RoomDecorInteriorAnchor.InteriorTopRight:
                anchorPoint = new Vector3(maxL.x - inset, maxL.y - inset, 0f);
                break;
            case RoomDecorInteriorAnchor.InteriorMiddleRight:
                anchorPoint = new Vector3(maxL.x - inset, c.y, 0f);
                break;
            case RoomDecorInteriorAnchor.InteriorBottomLeft:
                anchorPoint = new Vector3(minL.x + inset, minL.y + inset, 0f);
                break;
            case RoomDecorInteriorAnchor.InteriorBottomRight:
                anchorPoint = new Vector3(maxL.x - inset, minL.y + inset, 0f);
                break;
            case RoomDecorInteriorAnchor.InteriorTopCenter:
                anchorPoint = new Vector3(c.x, maxL.y - inset, 0f);
                break;
            case RoomDecorInteriorAnchor.InteriorMiddleLeft:
                anchorPoint = new Vector3(minL.x + inset, c.y, 0f);
                break;
            case RoomDecorInteriorAnchor.InteriorMiddleCenter:
                anchorPoint = new Vector3(c.x, c.y, 0f);
                break;
            case RoomDecorInteriorAnchor.InteriorBottomCenter:
                anchorPoint = new Vector3(c.x, minL.y + inset, 0f);
                break;
        }

        return anchorPoint + offset;
    }

    private static bool TryGetLargestTriggerLocalAabb(Transform roomRoot, out Vector3 localMin, out Vector3 localMax)
    {
        localMin = Vector3.zero;
        localMax = Vector3.zero;

        if (roomRoot == null)
            return false;

        Collider2D best = null;
        float bestArea = 0f;
        foreach (Collider2D c in roomRoot.GetComponentsInChildren<Collider2D>(true))
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

        if (best == null)
            return false;

        Bounds b = best.bounds;
        localMin = roomRoot.InverseTransformPoint(b.min);
        localMax = roomRoot.InverseTransformPoint(b.max);
        return true;
    }

    private static void TrySpawnCatalogPickup(
        Transform roomRoot,
        Room room,
        Transform pickupParent,
        RoomDecorationCatalog.DecorationEntry entry,
        Vector3 resolvedLocalPosition)
    {
        ItemDefinition def = entry.catalogPickup;
        if (def == null || pickupParent == null)
            return;

        Vector3 spawnScale = def.worldDropScale.sqrMagnitude > 1e-8f
            ? def.worldDropScale
            : Vector3.one;
        spawnScale *= ItemWorldSpawner.RoomPickupWorldScale;

        Vector3 worldPos = roomRoot.TransformPoint(resolvedLocalPosition);

        Item item = new Item
        {
            definition = def,
            amount = 1,
            worldScale = spawnScale
        };

        ItemWorld spawned = ItemWorld.SpawnItemWorld(worldPos, Quaternion.identity, spawnScale, item);
        if (spawned == null)
            return;

        if (room != null)
            spawned.SetRoom(room);

        ItemWorldSpawner.ApplyWorldSpawnSettings(spawned.gameObject, def);
        spawned.transform.SetParent(pickupParent, true);
    }
}
