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
    private const float DecorationColliderScale = 0.3f;
    private const float MinPropScale = 0.8f;
    private const float MaxPropScale = 1.85f;

    private readonly struct PlacementExtents
    {
        public readonly float HalfX;
        public readonly float HalfY;
        public readonly float Left;
        public readonly float Right;
        public readonly float Down;
        public readonly float Up;

        public PlacementExtents(float halfX, float halfY, float left, float right, float down, float up)
        {
            HalfX = halfX;
            HalfY = halfY;
            Left = left;
            Right = right;
            Down = down;
            Up = up;
        }
    }

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
        Transform pickupParent =
    roomRoot.Find("SpawnedLoots") ??
    roomRoot.Find("SpawnedItems");

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
        if (roomType == RoomType.Bathroom)
        {
            PlaceBathroomLayout(roomRoot, decorRoot.transform, catalog);
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
            PlacementExtents extents = EstimatePlacementExtents(entry, entryScale);
            Vector3 placedLocal = ResolveNonOverlappingPosition(
                resolvedLocal,
                extents,
                occupiedLocalBounds,
                hasInteriorBounds,
                localMin,
                localMax);

            if (entry.catalogPickup != null)
            {
                bool spawnPickup = ShouldSpawnCatalogPickup(entry);
                bool roomAlreadyHasThisType =
                    roomRoot != null &&
                    entry.catalogPickup != null &&
                    RoomItemWorldQuery.RoomHasShoppingListKeyInPickupScopes(
                        roomRoot.gameObject,
                        entry.catalogPickup.GetShoppingListKey());
                if (spawnPickup)
                {
                    if (roomAlreadyHasThisType)
                        continue;

                    PlacementExtents pickupExt = PickupExtentsForLootDefinition(entry.catalogPickup);
                    Vector3 pickupLocal = ResolveNonOverlappingPosition(
                        placedLocal,
                        pickupExt,
                        occupiedLocalBounds,
                        hasInteriorBounds,
                        localMin,
                        localMax);

                    TrySpawnCatalogPickup(roomRoot, room, pickupParent, entry, pickupLocal);
                    occupiedLocalBounds.Add(BuildLocalBounds(pickupLocal, pickupExt.HalfX, pickupExt.HalfY));
                }
                else if (HasDecorVisual(entry))
                {
                    // Not on list, or this room already has the pickup: still show decor (extra rows / duplicates).
                    SpawnDecorationSprite(
                        decorRoot.transform,
                        entry,
                        placedLocal,
                        entryScale,
                        addMovementBlockingCollider: roomType != RoomType.SportsRoom);
                    occupiedLocalBounds.Add(BuildLocalBounds(placedLocal, extents.HalfX, extents.HalfY));
                }

                continue;
            }

            if (!HasDecorVisual(entry))
                continue;

            SpawnDecorationSprite(
                decorRoot.transform,
                entry,
                placedLocal,
                entryScale,
                addMovementBlockingCollider: roomType != RoomType.SportsRoom);
            occupiedLocalBounds.Add(BuildLocalBounds(placedLocal, extents.HalfX, extents.HalfY));
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
        bool wideRoom = (right - left) >= (top - bottom);

        Sprite couchSprite = FindSpriteByName(catalog, "Couch");
        Sprite cushionSprite = FindSpriteByName(catalog, "Cushion");
        Sprite coffeeSprite = FindSpriteByName(catalog, "Coffeetable");
        Sprite remoteSprite = FindSpriteByName(catalog, "Remote");
        Sprite cabinetSprite = FindSpriteByName(catalog, "Cabinet");
        Sprite pictureSprite = FindSpriteByName(catalog, "Picture");
        Sprite curtainSprite = FindSpriteByName(catalog, "Curtain");
        Sprite plantSprite = FindSpriteByName(catalog, "Houseplant");
        Sprite monsteraSprite = FindSpriteByName(catalog, "Monstera");
        Sprite lampSprite = FindSpriteByName(catalog, "Lamp");
        Sprite cartSprite = FindSpriteByName(catalog, "Tiered_Cart");
        GameObject couchPrefab = FindPrefabByName(catalog, "Couch");
        GameObject cushionPrefab = FindPrefabByName(catalog, "Cushion");
        GameObject coffeePrefab = FindPrefabByName(catalog, "Coffeetable");
        GameObject remotePrefab = FindPrefabByName(catalog, "Remote");
        GameObject cabinetPrefab = FindPrefabByName(catalog, "Cabinet");
        GameObject picturePrefab = FindPrefabByName(catalog, "Picture");
        GameObject curtainPrefab = FindPrefabByName(catalog, "Curtain");
        GameObject plantPrefab = FindPrefabByName(catalog, "Houseplant");
        GameObject monsteraPrefab = FindPrefabByName(catalog, "Monstera");
        GameObject lampPrefab = FindPrefabByName(catalog, "Lamp");
        GameObject cartPrefab = FindPrefabByName(catalog, "Tiered_Cart");

        Vector3 couchPos = wideRoom ? new Vector3(midX, bottom + 1.45f, 0f) : new Vector3(midX, midY + 1.15f, 0f);
        Vector3 tablePos = wideRoom ? new Vector3(midX, bottom + 0.55f, 0f) : new Vector3(midX, midY - 1.15f, 0f);
        Vector3 leftCabinetPos = wideRoom ? new Vector3(left + 1.0f, top - 0.45f, 0f) : new Vector3(left + 0.85f, top - 0.65f, 0f);
        Vector3 rightCabinetPos = wideRoom ? new Vector3(right - 1.0f, top - 0.45f, 0f) : new Vector3(right - 0.85f, top - 0.65f, 0f);
        Vector3 curtainPos = wideRoom ? new Vector3(midX, top - 0.1f, 0f) : new Vector3(midX, top - 0.2f, 0f);
        Vector3 plantPos = wideRoom ? new Vector3(left + 2.85f, bottom + 0.75f, 0f) : new Vector3(left + 0.95f, midY + 0.25f, 0f);
        Vector3 lampPos = wideRoom ? new Vector3(right - 3.35f, bottom + 0.75f, 0f) : new Vector3(right - 0.95f, midY + 0.25f, 0f);
        Vector3 cartPos = new Vector3(midX, bottom + 0.8f, 0f);
        Vector3 leftMonsteraPos = wideRoom ? new Vector3(left + 2.35f, bottom + 0.78f, 0f) : new Vector3(left + 0.82f, bottom + 2.35f, 0f);
        Vector3 rightMonsteraPos = wideRoom ? new Vector3(right - 2.35f, bottom + 0.78f, 0f) : new Vector3(right - 0.82f, bottom + 2.35f, 0f);

        List<Bounds> occupied = new List<Bounds>();

        Vector3 PlaceLivingLocal(string name, GameObject prefab, Sprite sprite, Vector3 desiredLocal, float uniformScale = 1f)
        {
            var entry = new RoomDecorationCatalog.DecorationEntry
            {
                prefab = prefab,
                sprite = sprite,
                localScale = new Vector3(uniformScale, uniformScale, 1f),
                sortingLayerName = "Floor",
                sortingOrder = 8
            };
            Vector3 scale = new Vector3(Mathf.Max(0.01f, uniformScale), Mathf.Max(0.01f, uniformScale), 1f);
            PlacementExtents ext = EstimatePlacementExtents(entry, scale);
            Vector3 placed = ResolveNonOverlappingPosition(desiredLocal, ext, occupied, true, minL, maxL);
            occupied.Add(BuildLocalBounds(placed, ext.HalfX, ext.HalfY));
            return placed;
        }

        Vector3 couchPlaced = PlaceLivingLocal("Living_Couch", couchPrefab, couchSprite, couchPos);
        GameObject couch = SpawnLayoutObject(decorRoot, "Living_Couch", couchPrefab, couchSprite, couchPlaced, 8);

        if (couch != null)
        {
            Vector3 cushionPos = new Vector3(0.12f, 0.35f, 0f);
            SpawnLayoutObject(couch.transform, "Living_Cushion", cushionPrefab, cushionSprite, cushionPos, 9, 1f, false);
        }

        Vector3 tablePlaced = PlaceLivingLocal("Living_Coffeetable", coffeePrefab, coffeeSprite, tablePos);
        GameObject table = SpawnLayoutObject(decorRoot, "Living_Coffeetable", coffeePrefab, coffeeSprite, tablePlaced, 8);
        if (table != null)
        {
            Vector3 remotePos = wideRoom ? new Vector3(0.22f, -0.48f, 0f) : new Vector3(0.58f, 0.4f, 0f);
            SpawnLayoutObject(table.transform, "Living_Remote", remotePrefab, remoteSprite, remotePos, 12, 0.8f, false);
        }

        Vector3 leftCabinetPlaced = PlaceLivingLocal("Living_Cabinet_Left", cabinetPrefab, cabinetSprite, leftCabinetPos);
        GameObject leftCabinet = SpawnLayoutObject(decorRoot, "Living_Cabinet_Left", cabinetPrefab, cabinetSprite, leftCabinetPlaced, 8);
        if (leftCabinet != null)
        {
            Vector3 picturePos = wideRoom ? new Vector3(0f, -0.55f, 0f) : new Vector3(0f, -0.45f, 0f);
            SpawnLayoutObject(leftCabinet.transform, "Living_Picture", picturePrefab, pictureSprite, picturePos, 9, 1f, false);
        }

        Vector3 rightCabinetPlaced = PlaceLivingLocal("Living_Cabinet_Right", cabinetPrefab, cabinetSprite, rightCabinetPos);
        SpawnLayoutObject(decorRoot, "Living_Cabinet_Right", cabinetPrefab, cabinetSprite, rightCabinetPlaced, 8);

        Vector3 curtainPlaced = PlaceLivingLocal("Living_Curtain", curtainPrefab, curtainSprite, curtainPos);
        SpawnLayoutObject(decorRoot, "Living_Curtain", curtainPrefab, curtainSprite, curtainPlaced, 8);
        Vector3 plantPlaced = PlaceLivingLocal("Living_Houseplant", plantPrefab, plantSprite, plantPos);
        SpawnLayoutObject(decorRoot, "Living_Houseplant", plantPrefab, plantSprite, plantPlaced, 8);
        Vector3 lampPlaced = PlaceLivingLocal("Living_Lamp", lampPrefab, lampSprite, lampPos);
        SpawnLayoutObject(decorRoot, "Living_Lamp", lampPrefab, lampSprite, lampPlaced, 8);
        if (!wideRoom)
        {
            Vector3 cartPlaced = PlaceLivingLocal("Living_TieredCart", cartPrefab, cartSprite, cartPos);
            SpawnLayoutObject(decorRoot, "Living_TieredCart", cartPrefab, cartSprite, cartPlaced, 8);
        }
        Vector3 leftMonsteraPlaced = PlaceLivingLocal("Living_Monstera_Left", monsteraPrefab, monsteraSprite, leftMonsteraPos);
        SpawnLayoutObject(decorRoot, "Living_Monstera_Left", monsteraPrefab, monsteraSprite, leftMonsteraPlaced, 8);
        Vector3 rightMonsteraPlaced = PlaceLivingLocal("Living_Monstera_Right", monsteraPrefab, monsteraSprite, rightMonsteraPos);
        SpawnLayoutObject(decorRoot, "Living_Monstera_Right", monsteraPrefab, monsteraSprite, rightMonsteraPlaced, 8);
    }

    private static GameObject SpawnLayoutSprite(Transform parent, string name, Sprite sprite, Vector3 localPos, int sortingOrder, float uniformScale = 1f)
    {
        if (parent == null || sprite == null)
            return null;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        float s = Mathf.Max(0.01f, uniformScale);
        go.transform.localScale = new Vector3(s, s, 1f);
        go.layer = 0;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Floor";
        sr.sortingOrder = sortingOrder;
        AddBlockingCollider(go, sprite);
        return go;
    }
    private static RoomSpawnPrefabDefinition GetRoomSpawnDefinition(GameObject obj)
    {
        if (obj == null)
            return null;

        RoomSpawnPrefabDefinition def = obj.GetComponent<RoomSpawnPrefabDefinition>();
        if (def == null)
            def = obj.GetComponentInChildren<RoomSpawnPrefabDefinition>(true);

        return def;
    }

    private static Transform ResolveContainerForSpawnCategory(Transform roomRoot, RoomSpawnPrefabDefinition def)
    {
        if (roomRoot == null)
            return null;

        if (def == null)
            return roomRoot.Find("SpawnedObjects") ?? roomRoot;

        switch (def.spawnCategory)
        {
            case RoomSpawnCategory.Decoration:
                return roomRoot.Find("SpawnedObjects") ?? roomRoot;

            case RoomSpawnCategory.Weapon:
                return roomRoot.Find("SpawnedItems") ?? roomRoot;

            case RoomSpawnCategory.Item:
                return roomRoot.Find("SpawnedLoots")
                    ?? roomRoot.Find("SpawnedLoot")
                    ?? roomRoot;

            default:
                return roomRoot.Find("SpawnedObjects") ?? roomRoot;
        }
    }

    private static void AssignSpawnParentToAnyItemWorldSpawner(GameObject instance, Transform spawnParent)
    {
        if (instance == null)
            return;

        ItemWorldSpawner[] spawners = instance.GetComponentsInChildren<ItemWorldSpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] == null)
                continue;

            Transform finalParent = spawnParent;

            if (finalParent == null)
            {
                Room room = spawners[i].GetComponentInParent<Room>();
                if (room != null)
                {
                    finalParent =
                        room.transform.Find("SpawnedLoots") ??
                        room.transform.Find("SpawnedItems") ??
                        room.transform.Find("SpawnedObjects") ??
                        room.transform;
                }
            }

            spawners[i].SetSpawnParent(finalParent);
        }
    }
    private static GameObject SpawnLayoutObject(
    Transform parent,
    string name,
    GameObject prefab,
    Sprite fallbackSprite,
    Vector3 localPos,
    int sortingOrder,
    float uniformScale = 1f,
    bool addMovementBlockingCollider = true)
    {
        if (prefab != null && parent != null)
        {
            Transform roomRoot = parent.GetComponentInParent<Room>() != null
                ? parent.GetComponentInParent<Room>().transform
                : parent;

            RoomSpawnPrefabDefinition def = GetRoomSpawnDefinition(prefab);
            Transform targetContainer = ResolveContainerForSpawnCategory(roomRoot, def);

            Transform instantiateParent =
                def != null && def.spawnCategory == RoomSpawnCategory.Decoration
                    ? parent
                    : (targetContainer != null ? targetContainer : parent);

            GameObject go = Object.Instantiate(prefab, instantiateParent, false);

            // 关键：先关掉，别让 ItemWorldSpawner 抢跑
            go.SetActive(false);

            go.name = name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            float s = Mathf.Max(0.01f, uniformScale);
            go.transform.localScale = new Vector3(s, s, 1f);

            // 关键：在重新激活前，先把所有 ItemWorldSpawner 的 parent 塞好
            AssignSpawnParentToAnyItemWorldSpawner(
                go,
                targetContainer != null ? targetContainer : instantiateParent);

            if (addMovementBlockingCollider && go.GetComponentInChildren<Collider2D>(true) == null)
            {
                Sprite refSprite = GetPrefabReferenceSprite(prefab);
                if (refSprite != null)
                    AddBlockingCollider(go, refSprite);
            }

            // 最后再开
            go.SetActive(true);
            return go;
        }

        GameObject spawned = SpawnLayoutSprite(parent, name, fallbackSprite, localPos, sortingOrder, uniformScale);
        if (!addMovementBlockingCollider && spawned != null)
        {
            Collider2D c = spawned.GetComponent<Collider2D>();
            if (c != null)
                Object.Destroy(c);
        }
        return spawned;
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

    private static GameObject FindPrefabByName(RoomDecorationCatalog catalog, string objectName)
    {
        if (catalog == null || catalog.entries == null || string.IsNullOrEmpty(objectName))
            return null;

        for (int i = 0; i < catalog.entries.Count; i++)
        {
            RoomDecorationCatalog.DecorationEntry e = catalog.entries[i];
            if (e?.prefab == null)
                continue;

            string n = e.prefab.name;
            if (string.Equals(n, objectName, System.StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith(objectName + "_", System.StringComparison.OrdinalIgnoreCase))
            {
                return e.prefab;
            }
        }

        return null;
    }

    /// <summary>World-space max axis span of the toilet when spawned as shopping-list loot (ItemWorld scale × sprite bounds).</summary>
    private static float ComputeBathroomReferenceToiletWorldSpan(Sprite layoutToiletSprite, ItemDefinition toiletLootDef)
    {
        Sprite refSprite = toiletLootDef != null && toiletLootDef.icon != null
            ? toiletLootDef.icon
            : layoutToiletSprite;
        if (refSprite == null)
            return 1.85f;

        float span = Mathf.Max(refSprite.bounds.size.x, refSprite.bounds.size.y);
        float worldMul = ItemWorldSpawner.RoomPickupWorldScale;
        if (toiletLootDef != null && toiletLootDef.worldDropScale.sqrMagnitude > 1e-8f)
            worldMul *= toiletLootDef.worldDropScale.x;

        return span * worldMul;
    }

    private static float BathroomUniformScaleForSprite(Sprite sprite, float targetWorldSpan)
    {
        if (sprite == null)
            return 1.56f;
        float span = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        if (span < 1e-4f)
            return 1.56f;
        return Mathf.Max(0.05f, targetWorldSpan / span);
    }

    private static void PlaceBathroomLayout(Transform roomRoot, Transform decorRoot, RoomDecorationCatalog catalog)
    {
        if (roomRoot == null || decorRoot == null || catalog == null)
            return;
        if (!TryGetLargestTriggerLocalAabb(roomRoot, out Vector3 minL, out Vector3 maxL))
            return;

        Room room = roomRoot.GetComponent<Room>();
        Transform pickupParent =
    roomRoot.Find("SpawnedLoots") ??
    roomRoot.Find("SpawnedItems");

        float left = minL.x + WallPadding;
        float right = maxL.x - WallPadding;
        float top = maxL.y - WallPadding;
        float bottom = minL.y + WallPadding;
        float midX = (left + right) * 0.5f;
        float midY = (bottom + top) * 0.5f;
        float spanX = Mathf.Max(0.5f, right - left);
        float spanY = Mathf.Max(0.5f, top - bottom);

        List<Bounds> occupied = new List<Bounds>();

        Sprite counter = FindSpriteByName(catalog, "Counter_Sink");
        Sprite toiletSpr = FindSpriteByName(catalog, "Toilet");
        Sprite towel = FindSpriteByName(catalog, "Towel_Basket");
        Sprite tray = FindSpriteByName(catalog, "Bathroom_Tray");
        Sprite curtain = FindSpriteByName(catalog, "Curtain");
        Sprite monstera = FindSpriteByName(catalog, "Monstera");
        Sprite cart = FindSpriteByName(catalog, "Tiered_Cart");
        GameObject counterPrefab = FindPrefabByName(catalog, "Counter_Sink");
        GameObject toiletPrefab = FindPrefabByName(catalog, "Toilet");
        GameObject towelPrefab = FindPrefabByName(catalog, "Towel_Basket");
        GameObject trayPrefab = FindPrefabByName(catalog, "Bathroom_Tray");
        GameObject curtainPrefab = FindPrefabByName(catalog, "Curtain");
        GameObject monsteraPrefab = FindPrefabByName(catalog, "Monstera");
        GameObject cartPrefab = FindPrefabByName(catalog, "Tiered_Cart");

        ItemDefinition toiletLoot = FindLootDefinitionByKey(catalog, "Toilet", RoomType.Bathroom);
        float bathroomTargetSpan = ComputeBathroomReferenceToiletWorldSpan(toiletSpr, toiletLoot);

        // Sink reads larger than other props; towel basket smaller (not all matched to toilet span equally).
        const float bathroomSinkSpanMul = 1.24f;
        const float bathroomTowelSpanMul = 0.66f;

        bool roomHasToiletPickup =
            toiletLoot != null &&
            RoomItemWorldQuery.RoomHasDefinitionInPickupScopes(roomRoot.gameObject, toiletLoot);

        bool wantsToiletPickup =
            toiletLoot != null &&
            ShouldSpawnBathroomShoppingListPickup(toiletLoot) &&
            !roomHasToiletPickup;

        bool showDecorToilet = toiletSpr != null && !wantsToiletPickup && !roomHasToiletPickup;

        BathroomTryPlaceDecor(
            decorRoot, occupied, minL, maxL, "Bath_Counter", counterPrefab, counter, new Vector3(midX, top - 0.5f, 0f),
            BathroomUniformScaleForSprite(counter, bathroomTargetSpan * bathroomSinkSpanMul));

        float toiletX = left + Mathf.Clamp(spanX * 0.11f, 0.68f, 1.08f);
        Vector3 toiletDesired = new Vector3(toiletX, midY + 0.06f, 0f);
        Vector3 placedToilet = toiletDesired;
        if (showDecorToilet)
        {
            placedToilet = BathroomTryPlaceDecor(
                decorRoot, occupied, minL, maxL, "Bath_Toilet", toiletPrefab, toiletSpr, toiletDesired,
                BathroomUniformScaleForSprite(toiletSpr, bathroomTargetSpan));
        }

        float towelX = Mathf.Min(midX + Mathf.Clamp(spanX * 0.17f, 1.2f, 1.95f), right - 0.72f);
        BathroomTryPlaceDecor(
            decorRoot, occupied, minL, maxL, "Bath_Towel", towelPrefab, towel, new Vector3(towelX, top - 0.7f, 0f),
            BathroomUniformScaleForSprite(towel, bathroomTargetSpan * bathroomTowelSpanMul));

        float cartY = bottom + Mathf.Clamp(spanY * 0.3f, 1.0f, 1.55f);
        BathroomTryPlaceDecor(
            decorRoot, occupied, minL, maxL, "Bath_Tray", trayPrefab, tray, new Vector3(midX + 0.22f, cartY, 0f),
            BathroomUniformScaleForSprite(tray, bathroomTargetSpan));

        BathroomTryPlaceDecor(
            decorRoot, occupied, minL, maxL, "Bath_Curtain", curtainPrefab, curtain, new Vector3(right - 0.72f, top - 0.72f, 0f),
            BathroomUniformScaleForSprite(curtain, bathroomTargetSpan));

        float plantInset = Mathf.Clamp(spanX * 0.075f, 0.58f, 0.95f);
        BathroomTryPlaceDecor(
            decorRoot, occupied, minL, maxL, "Bath_MonsteraL", monsteraPrefab, monstera, new Vector3(left + plantInset - 0.35f, top - 0.82f, 0f),
            BathroomUniformScaleForSprite(monstera, bathroomTargetSpan));
        BathroomTryPlaceDecor(
            decorRoot, occupied, minL, maxL, "Bath_MonsteraR", monsteraPrefab, monstera, new Vector3(left + plantInset + 0.33f, top - 0.82f, 0f),
            BathroomUniformScaleForSprite(monstera, bathroomTargetSpan));

        if (pickupParent != null && wantsToiletPickup)
        {
            PlacementExtents pickExt = BathroomExtentsForLootDefinition(toiletLoot);
            Vector3 pickupLocal = ResolveNonOverlappingPosition(placedToilet, pickExt, occupied, true, minL, maxL);
            TrySpawnCatalogPickupForDefinition(roomRoot, room, pickupParent, toiletLoot, pickupLocal);
            occupied.Add(BuildLocalBounds(pickupLocal, pickExt.HalfX, pickExt.HalfY));
        }
    }

    private static bool ShouldSpawnBathroomShoppingListPickup(ItemDefinition def)
    {
        if (def == null)
            return false;

        RunObjectiveManager rom = RunObjectiveManager.Instance;
        if (rom == null)
            return false;

        return rom.ContainsShoppingListKey(def.GetShoppingListKey());
    }

    private static PlacementExtents BathroomExtentsForLootDefinition(ItemDefinition def)
    {
        if (def?.icon != null)
        {
            float ws = def.worldDropScale.sqrMagnitude > 1e-8f ? def.worldDropScale.x : 1f;
            ws *= ItemWorldSpawner.RoomPickupWorldScale;
            Vector2 s = def.icon.bounds.size;
            float hx = Mathf.Max(0.22f, s.x * 0.5f * ws);
            float hy = Mathf.Max(0.22f, s.y * 0.5f * ws);
            return new PlacementExtents(hx, hy, hx, hx, hy, hy);
        }

        return new PlacementExtents(0.45f, 0.45f, 0.45f, 0.45f, 0.45f, 0.45f);
    }

    private static PlacementExtents PickupExtentsForLootDefinition(ItemDefinition def)
    {
        return BathroomExtentsForLootDefinition(def);
    }

    private static Vector3 BathroomTryPlaceDecor(
        Transform decorRoot,
        List<Bounds> occupied,
        Vector3 minL,
        Vector3 maxL,
        string _objectName,
        GameObject prefab,
        Sprite sprite,
        Vector3 desiredLocal,
        float uniformScale)
    {
        if (decorRoot == null || sprite == null)
            return desiredLocal;

        var entry = new RoomDecorationCatalog.DecorationEntry
        {
            prefab = prefab,
            sprite = sprite,
            localScale = new Vector3(uniformScale, uniformScale, 1f),
            sortingLayerName = "Floor",
            sortingOrder = 8
        };

        // Bathroom props must match shopping-list toilet size; skip ResolveEntryScale MaxPropScale clamp (~1.85).
        float s = Mathf.Max(0.01f, uniformScale);
        Vector3 scale = new Vector3(s, s, 1f);
        PlacementExtents ext = EstimatePlacementExtents(entry, scale);
        Vector3 placedLocal = ResolveNonOverlappingPosition(
            desiredLocal,
            ext,
            occupied,
            true,
            minL,
            maxL);

        SpawnDecorationSprite(decorRoot, entry, placedLocal, scale);
        occupied.Add(BuildLocalBounds(placedLocal, ext.HalfX, ext.HalfY));
        return placedLocal;
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

        // Beds hug corners again; colliders stay smaller so doors remain usable.
        float x0 = Mathf.Lerp(left, right, 0.08f);
        float x1 = Mathf.Lerp(left, right, 0.92f);
        float y0 = Mathf.Lerp(bottom, top, 0.1f);
        float y1 = Mathf.Lerp(bottom, top, 0.9f);

        const float referenceBedLayoutScale = 1.16f;
        const float sideFurnitureScale = 1.0f;

        ItemDefinition bedLoot = FindLootDefinitionByKey(catalog, "Bed", RoomType.Bedroom);
        ItemDefinition lampLoot = FindLootDefinitionByKey(catalog, "Lamp", RoomType.None);
        ItemDefinition drawerLoot = FindLootDefinitionByKey(catalog, "Drawer", RoomType.None);
        ItemDefinition beePlushLoot = FindLootDefinitionByKey(catalog, "Bee_Plush", RoomType.None);
        ItemDefinition owlPlushLoot = FindLootDefinitionByKey(catalog, "Owl_Plush", RoomType.None);

        Sprite bedBee = FindSpriteByName(catalog, "Bed_Bee");
        Sprite bedBeeNo = FindSpriteByName(catalog, "Bed_Bee_No_Plush");
        Sprite bedOwl = FindSpriteByName(catalog, "Bed_Night_Owl");
        Sprite bedOwlNo = FindSpriteByName(catalog, "Bed_Night_No_Owl");
        Sprite bedCherry = FindSpriteByName(catalog, "Bed_Cherry_Blossom");
        Sprite beePlush = FindSpriteByName(catalog, "Bee_Plush");
        Sprite owlPlush = FindSpriteByName(catalog, "Owl_Plush");
        Sprite lampSprite = FindSpriteByName(catalog, "Lamp_0");
        if (lampSprite == null) lampSprite = FindSpriteByName(catalog, "Lamp");
        Sprite drawerSprite = FindSpriteByName(catalog, "Drawer (1)_0");
        if (drawerSprite == null) drawerSprite = FindSpriteByName(catalog, "Drawer");
        GameObject bedBeePrefab = FindPrefabByName(catalog, "Bed_Bee");
        GameObject bedOwlPrefab = FindPrefabByName(catalog, "Bed_Night_Owl");
        GameObject bedCherryPrefab = FindPrefabByName(catalog, "Bed_Cherry_Blossom");
        GameObject beePlushPrefab = FindPrefabByName(catalog, "Bee_Plush");
        GameObject owlPlushPrefab = FindPrefabByName(catalog, "Owl_Plush");
        GameObject lampPrefab = FindPrefabByName(catalog, "Lamp");
        GameObject drawerPrefab = FindPrefabByName(catalog, "Drawer");

        Vector3[] bedPositions = new Vector3[]
        {
            new Vector3(x0, y0, 0f),
            new Vector3(x1, y0, 0f),
            new Vector3(x0, y1, 0f),
            new Vector3(x1, y1, 0f)
        };

        Sprite referenceBedSprite = bedBee != null ? bedBee : bedOwl;
        float referenceSpan = 1.26f;
        if (referenceBedSprite != null)
        {
            referenceSpan = Mathf.Max(
                referenceBedSprite.bounds.size.x,
                referenceBedSprite.bounds.size.y);
        }

        float targetWorldSpan = referenceBedLayoutScale * referenceSpan;

        float UniformScaleForBedSprite(Sprite sprite)
        {
            if (sprite == null)
                return referenceBedLayoutScale;
            float span = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            if (span < 1e-4f)
                return referenceBedLayoutScale;
            return targetWorldSpan / span;
        }

        List<Bounds> occupiedBedroom = new List<Bounds>();
        List<GameObject> sideFallbackCandidates = new List<GameObject>();
        RunObjectiveManager rom = RunObjectiveManager.Instance;
        bool lampOnList = lampLoot != null && rom != null && rom.ContainsShoppingListKey(lampLoot.GetShoppingListKey());
        bool drawerOnList = drawerLoot != null && rom != null && rom.ContainsShoppingListKey(drawerLoot.GetShoppingListKey());
        for (int i = 0; i < bedPositions.Length; i++)
        {
            // Replace one bee corner so we still have two owl plush beds (shopping list can require two Owl_Plush).
            bool cherryCorner = i == 2 && bedCherry != null;

            if (cherryCorner)
            {
                float cherryScale = UniformScaleForBedSprite(bedCherry);
                GameObject cherryBed = SpawnLayoutObject(decorRoot, $"Bedroom_Bed_{i}", bedCherryPrefab, bedCherry, bedPositions[i], 8, cherryScale);
                if (cherryBed == null)
                    continue;

                BoxCollider2D cherryBedCol = cherryBed.AddComponent<BoxCollider2D>();
                cherryBedCol.isTrigger = false;
                cherryBedCol.size = new Vector2(0.56f * cherryScale, 0.32f * cherryScale);
                occupiedBedroom.Add(BuildLocalBounds(bedPositions[i], 0.3f * cherryScale, 0.19f * cherryScale));

                bool cherryLampSide = (i % 2 == 0);
                Sprite cherrySideSprite = cherryLampSide ? lampSprite : drawerSprite;
                float cherrySideX = (i % 2 == 0 ? 0.86f : -0.86f) * cherryScale;
                Vector3 cherrySideLocal = new Vector3(cherrySideX, -0.02f * cherryScale, 0f);
                Vector3 cherrySideDesired = bedPositions[i] + cherrySideLocal;
                var cherrySideEntry = new RoomDecorationCatalog.DecorationEntry
                {
                    sprite = cherrySideSprite,
                    localScale = new Vector3(sideFurnitureScale, sideFurnitureScale, 1f)
                };
                PlacementExtents cherrySideExt = EstimatePlacementExtents(cherrySideEntry, cherrySideEntry.localScale);
                Vector3 cherrySidePlaced = ResolveNonOverlappingPosition(
                    cherrySideDesired,
                    cherrySideExt,
                    occupiedBedroom,
                    true,
                    minL,
                    maxL);
                GameObject cherrySide = SpawnLayoutObject(
                    decorRoot,
                    cherryLampSide ? $"Bedroom_Lamp_{i}" : $"Bedroom_Drawer_{i}",
                    cherryLampSide ? lampPrefab : drawerPrefab,
                    cherrySideSprite,
                    cherrySidePlaced,
                    8,
                    sideFurnitureScale);
                if (cherrySide != null)
                {
                    BoxCollider2D cherrySideCol = cherrySide.AddComponent<BoxCollider2D>();
                    cherrySideCol.isTrigger = false;
                    cherrySideCol.size = new Vector2(0.22f * sideFurnitureScale, 0.2f * sideFurnitureScale);
                    occupiedBedroom.Add(BuildLocalBounds(cherrySidePlaced, cherrySideExt.HalfX, cherrySideExt.HalfY));

                    bool shouldForcePickup = cherryLampSide ? lampOnList : drawerOnList;
                    if (shouldForcePickup)
                    {
                        ItemDefinition def = cherryLampSide ? lampLoot : drawerLoot;
                        LivingRoomContainerPickup pickup = cherrySide.AddComponent<LivingRoomContainerPickup>();
                        pickup.Configure(def, 1, cherryLampSide ? "Lamp" : "Drawer");
                    }
                    else
                    {
                        sideFallbackCandidates.Add(cherrySide);
                    }
                }

                continue;
            }

            bool useBee = (i % 2 == 0);

            // Always start with the "with plush" bed art; after pickup, sprite swaps to the no-plush variant.
            Sprite bedSprite = useBee ? bedBee : bedOwl;
            float bedScale = UniformScaleForBedSprite(bedSprite);

            GameObject bed = SpawnLayoutObject(
                decorRoot,
                $"Bedroom_Bed_{i}",
                useBee ? bedBeePrefab : bedOwlPrefab,
                bedSprite,
                bedPositions[i],
                8,
                bedScale);
            if (bed == null)
                continue;

            BoxCollider2D bedCol = bed.AddComponent<BoxCollider2D>();
            bedCol.isTrigger = false;
            bedCol.size = new Vector2(0.56f * bedScale, 0.32f * bedScale);
            occupiedBedroom.Add(BuildLocalBounds(bedPositions[i], 0.3f * bedScale, 0.19f * bedScale));

            Sprite plushSprite = useBee ? beePlush : owlPlush;
            float plushYOffset = 0.18f * bedScale;
            SpawnLayoutObject(
                bed.transform,
                useBee ? $"BeePlush_{i}" : $"OwlPlush_{i}",
                useBee ? beePlushPrefab : owlPlushPrefab,
                plushSprite,
                new Vector3(0f, plushYOffset, 0f),
                9,
                bedScale);
            // Plush interaction is disabled for now; plush remains visual decor only.

            bool lampSide = (i % 2 == 0);
            Sprite sideSprite = lampSide ? lampSprite : drawerSprite;
            float sideX = (i % 2 == 0 ? 0.86f : -0.86f) * bedScale;
            Vector3 sideLocal = new Vector3(sideX, -0.02f * bedScale, 0f);
            Vector3 sideDesired = bedPositions[i] + sideLocal;
            var sideEntry = new RoomDecorationCatalog.DecorationEntry
            {
                sprite = sideSprite,
                localScale = new Vector3(sideFurnitureScale, sideFurnitureScale, 1f)
            };
            PlacementExtents sideExt = EstimatePlacementExtents(sideEntry, sideEntry.localScale);
            Vector3 sidePlaced = ResolveNonOverlappingPosition(
                sideDesired,
                sideExt,
                occupiedBedroom,
                true,
                minL,
                maxL);
            GameObject side = SpawnLayoutObject(
                decorRoot,
                lampSide ? $"Bedroom_Lamp_{i}" : $"Bedroom_Drawer_{i}",
                lampSide ? lampPrefab : drawerPrefab,
                sideSprite,
                sidePlaced,
                8,
                sideFurnitureScale);
            if (side != null)
            {
                BoxCollider2D sideCol = side.AddComponent<BoxCollider2D>();
                sideCol.isTrigger = false;
                sideCol.size = new Vector2(0.22f * sideFurnitureScale, 0.2f * sideFurnitureScale);
                occupiedBedroom.Add(BuildLocalBounds(sidePlaced, sideExt.HalfX, sideExt.HalfY));

                bool shouldForcePickup = lampSide ? lampOnList : drawerOnList;
                if (shouldForcePickup)
                {
                    ItemDefinition def = lampSide ? lampLoot : drawerLoot;
                    LivingRoomContainerPickup pickup = side.AddComponent<LivingRoomContainerPickup>();
                    pickup.Configure(def, 1, lampSide ? "Lamp" : "Drawer");
                }
                else
                {
                    sideFallbackCandidates.Add(side);
                }
            }
        }

        ShuffleGameObjects(sideFallbackCandidates);
        int collectibleCount = Mathf.Min(sideFallbackCandidates.Count, Random.Range(2, 5));
        for (int i = 0; i < collectibleCount; i++)
        {
            GameObject side = sideFallbackCandidates[i];
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

    private static bool HasDecorVisual(RoomDecorationCatalog.DecorationEntry entry)
    {
        return entry != null && (entry.prefab != null || entry.sprite != null);
    }

    private static void SpawnDecorationSprite(
        Transform decorRoot,
        RoomDecorationCatalog.DecorationEntry entry,
        Vector3 resolvedLocal,
        Vector3 resolvedScale,
        bool addMovementBlockingCollider = true)
    {
        if (decorRoot == null || entry == null)
            return;

        if (entry.prefab != null)
        {
            GameObject go = Object.Instantiate(entry.prefab, decorRoot, false);
            go.name = entry.prefab.name;
            go.transform.localPosition = resolvedLocal;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = resolvedScale;

            if (addMovementBlockingCollider && go.GetComponentInChildren<Collider2D>(true) == null)
            {
                Sprite refSprite = entry.sprite != null ? entry.sprite : GetPrefabReferenceSprite(entry.prefab);
                if (refSprite != null)
                    AddBlockingCollider(go, refSprite);
            }

            return;
        }

        if (entry.sprite == null)
            return;

        GameObject fallback = new GameObject(entry.sprite.name);
        fallback.transform.SetParent(decorRoot, false);
        fallback.transform.localPosition = resolvedLocal;
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = resolvedScale;
        fallback.layer = 0;

        SpriteRenderer sr = fallback.AddComponent<SpriteRenderer>();
        sr.sprite = entry.sprite;
        sr.sortingLayerName = entry.sortingLayerName;
        sr.sortingOrder = entry.sortingOrder;
        if (addMovementBlockingCollider)
            AddBlockingCollider(fallback, entry.sprite);
    }

    private static void AddBlockingCollider(GameObject target, Sprite sprite)
    {
        if (target == null || sprite == null)
            return;

        BoxCollider2D box = target.GetComponent<BoxCollider2D>();
        if (box == null)
            box = target.AddComponent<BoxCollider2D>();

        target.layer = 6;
        box.isTrigger = false;
        Vector2 spriteSize = sprite.bounds.size;
        box.size = new Vector2(
            Mathf.Max(0.08f, spriteSize.x * DecorationColliderScale),
            Mathf.Max(0.08f, spriteSize.y * DecorationColliderScale));
        box.offset = Vector2.zero;
    }

    private static Sprite GetPrefabReferenceSprite(GameObject prefab)
    {
        if (prefab == null)
            return null;

        SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr != null && sr.sprite != null)
                return sr.sprite;
        }

        return null;
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

    private static PlacementExtents EstimatePlacementExtents(RoomDecorationCatalog.DecorationEntry entry, Vector3 scale)
    {
        if (entry?.prefab != null)
        {
            Sprite prefabSprite = GetPrefabReferenceSprite(entry.prefab);
            if (prefabSprite != null)
            {
                Bounds b = prefabSprite.bounds;
                float left = Mathf.Max(0.2f, -b.min.x * scale.x);
                float right = Mathf.Max(0.2f, b.max.x * scale.x);
                float down = Mathf.Max(0.2f, -b.min.y * scale.y);
                float up = Mathf.Max(0.2f, b.max.y * scale.y);
                float hx = Mathf.Max(left, right);
                float hy = Mathf.Max(down, up);
                return new PlacementExtents(hx, hy, left, right, down, up);
            }
        }

        if (entry?.sprite != null)
        {
            Bounds b = entry.sprite.bounds;
            float left = Mathf.Max(0.2f, -b.min.x * scale.x);
            float right = Mathf.Max(0.2f, b.max.x * scale.x);
            float down = Mathf.Max(0.2f, -b.min.y * scale.y);
            float up = Mathf.Max(0.2f, b.max.y * scale.y);
            float hx = Mathf.Max(left, right);
            float hy = Mathf.Max(down, up);
            return new PlacementExtents(hx, hy, left, right, down, up);
        }

        if (entry?.catalogPickup?.icon != null)
        {
            Vector2 s = entry.catalogPickup.icon.bounds.size;
            float hx = Mathf.Max(0.2f, s.x * 0.5f);
            float hy = Mathf.Max(0.2f, s.y * 0.5f);
            return new PlacementExtents(hx, hy, hx, hx, hy, hy);
        }

        float d = 0.35f;
        return new PlacementExtents(d, d, d, d, d, d);
    }

    private static Bounds BuildLocalBounds(Vector3 center, float halfX, float halfY)
    {
        return new Bounds(
            new Vector3(center.x, center.y, 0f),
            new Vector3(halfX * 2f, halfY * 2f, 0.2f));
    }

    private static Vector3 ResolveNonOverlappingPosition(
        Vector3 desiredLocal,
        PlacementExtents extents,
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
            new Vector3(0f, -PlacementGrid * 2f, 0f),
            new Vector3(PlacementGrid * 3f, 0f, 0f),
            new Vector3(-PlacementGrid * 3f, 0f, 0f),
            new Vector3(0f, PlacementGrid * 3f, 0f),
            new Vector3(0f, -PlacementGrid * 3f, 0f),
            new Vector3(PlacementGrid * 4f, PlacementGrid, 0f),
            new Vector3(-PlacementGrid * 4f, PlacementGrid, 0f),
            new Vector3(PlacementGrid, PlacementGrid * 4f, 0f),
            new Vector3(PlacementGrid, -PlacementGrid * 4f, 0f)
        };

        Vector3 fallback = desiredLocal;
        for (int i = 0; i < candidateOffsets.Length; i++)
        {
            Vector3 candidate = desiredLocal + candidateOffsets[i];
            if (hasInteriorBounds)
                candidate = ClampInsideBounds(candidate, extents, localMin, localMax);

            Bounds b = BuildLocalBounds(candidate, extents.HalfX, extents.HalfY);
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

    private static Vector3 ClampInsideBounds(Vector3 p, PlacementExtents e, Vector3 localMin, Vector3 localMax)
    {
        float minX = localMin.x + WallPadding + e.Left;
        float maxX = localMax.x - WallPadding - e.Right;
        float minY = localMin.y + WallPadding + e.Down;
        float maxY = localMax.y - WallPadding - e.Up;

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
        Debug.Log(
            $"[DECOR ROUTE] room={roomRoot.name}, key={entry.catalogPickup?.GetShoppingListKey()}, " +
            $"pickupParent={(pickupParent != null ? pickupParent.name : "NULL")}"
        );

        if (entry?.catalogPickup == null || pickupParent == null)
            return;

        TrySpawnCatalogPickupForDefinition(roomRoot, room, pickupParent, entry.catalogPickup, resolvedLocalPosition);
    }

    private static void TrySpawnCatalogPickupForDefinition(
    Transform roomRoot,
    Room room,
    Transform pickupParent,
    ItemDefinition def,
    Vector3 resolvedLocalPosition)
    {
        Debug.Log(
            $"[DECOR SPAWN] room={roomRoot.name}, item={def.itemName}, " +
            $"pickupParent={(pickupParent != null ? pickupParent.name : "NULL")}"
        );

        if (def == null || pickupParent == null || roomRoot == null)
            return;

        Vector3 spawnScale = ItemWorldSpawner.GetWorldSpawnScale(def, pickupParent);

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
