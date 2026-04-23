using System.Collections.Generic;
using UnityEngine;

public static class RoomPrefabStaticPlacer
{
    private const string DecorRootName = "Decorations";
    private const float DoorClearanceHalfWidth = 0.9f;
    private const float DoorClearanceDepth = 0.95f;
    private const float EdgeInset = 0.7f;
    private const float MinSeparation = 0.85f;

    public static bool PlaceRoomPrefabs(Transform roomRoot, RoomPrefabSpawnCatalog catalog, bool allowLootItems = true)
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
        Transform decorParent = roomRoot.Find("SpawnedObjects") ?? roomRoot;
        decorRoot.transform.SetParent(decorParent, false);
        decorRoot.transform.localPosition = Vector3.zero;
        decorRoot.transform.localRotation = Quaternion.identity;
        decorRoot.transform.localScale = Vector3.one;

        bool spawnedAny = false;
        List<Vector2> used = new List<Vector2>();

        if (roomType == RoomType.Bedroom)
            spawnedAny = PlaceBedroomLayout(roomRoot, decorRoot.transform, catalog, prefabs, used);
        else if (roomType == RoomType.Kitchen)
            spawnedAny = PlaceKitchenLayout(roomRoot, decorRoot.transform, prefabs, used);
        else if (roomType == RoomType.Bathroom)
            spawnedAny = PlaceBathroomLayout(roomRoot, decorRoot.transform, prefabs, used);
        else if (roomType == RoomType.LivingRoom)
            spawnedAny = PlaceLivingRoomLayout(roomRoot, decorRoot.transform, catalog, prefabs, used);
        else if (roomType == RoomType.Cafeteria)
            spawnedAny = PlaceCafeteriaLayout(roomRoot, decorRoot.transform, prefabs, used);

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
            if (roomType == RoomType.Bathroom && IsBathroomLayoutPiece(prefab))
                continue;
            if (roomType == RoomType.LivingRoom && IsLivingRoomLayoutPiece(prefab))
                continue;
            if (roomType == RoomType.Cafeteria && IsCafeteriaLayoutPiece(prefab))
                continue;

            RoomSpawnPrefabDefinition def = GetRoomSpawnDefinition(prefab);
            if (def == null)
                continue;
            if (def.spawnCategory == RoomSpawnCategory.Weapon)
                continue;

            Vector3 position = ResolvePlacement(roomRoot, roomType, prefab, i, used);

            bool makeLootItemDecorOnly = !allowLootItems && def.spawnCategory == RoomSpawnCategory.Item;
            Transform targetContainer = ResolveContainerForSpawnCategory(roomRoot, def);
            Transform instantiateParent = (def.spawnCategory == RoomSpawnCategory.Decoration || makeLootItemDecorOnly)
                ? decorRoot.transform
                : (targetContainer != null ? targetContainer : roomRoot);

            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity, instantiateParent);
            instance.SetActive(false);

            StripLegacySpawnerPath(instance);
            NormalizeSpawnedVisuals(instance);
            ClampInstanceInsideRoom(instance, roomRoot);
            EnsureSolidPropCollider(instance);

            // If this prefab still contains ItemWorldSpawner, force the spawned ItemWorld
            // into the correct room-local container based on spawn category.
            AssignSpawnParentToAnyItemWorldSpawner(instance, targetContainer != null ? targetContainer : instantiateParent);

            if (makeLootItemDecorOnly)
                MakeLootItemDecorOnly(instance);
            else
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
        GameObject tablePrefab = FindByToken(kitchenPrefabs, "table");
        GameObject drawerPrefab = FindByToken(kitchenPrefabs, "drawer");
        GameObject mittensPrefab = FindByToken(kitchenPrefabs, "mittens");
        GameObject apronPrefab = FindByToken(kitchenPrefabs, "apron");
        GameObject knifePrefab = FindByToken(kitchenPrefabs, "knife");
        GameObject whiskPrefab = FindByToken(kitchenPrefabs, "whisk");
        GameObject ladlePrefab = FindByToken(kitchenPrefabs, "ladle");
        GameObject fryingPanPrefab = FindByToken(kitchenPrefabs, "frying") ?? FindByToken(kitchenPrefabs, "pan");

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
                RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopCenter, new Vector3(-2.85f, -0.82f, 0f)),
                RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopCenter, new Vector3(2.85f, -0.82f, 0f)),
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
                new Vector3(0f, -0.86f, 0f));
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
                new Vector3(0f, -0.2f, 0f));
            GameObject sink = SpawnConfiguredPrefab(sinkPrefab, sinkPos, parent, roomRoot);
            if (sink != null)
            {
                used.Add(sink.transform.position);
                spawnedAny = true;
            }
        }

        if (drawerPrefab != null)
        {
            Vector3 leftSinkDrawerPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                new Vector3(-3.05f, -0.2f, 0f));
            GameObject leftSinkDrawer = SpawnConfiguredPrefab(drawerPrefab, leftSinkDrawerPos, parent, roomRoot);
            if (leftSinkDrawer != null)
            {
                SetItemSortingOrder(leftSinkDrawer, 10);
                used.Add(leftSinkDrawer.transform.position);
                spawnedAny = true;
                SpawnKitchenSurfaceItem(knifePrefab, leftSinkDrawer.transform.position + new Vector3(-0.3f, 1.08f, 0f), parent, roomRoot, 0.85f);
                SpawnKitchenSurfaceItem(whiskPrefab, leftSinkDrawer.transform.position + new Vector3(0.28f, 1.1f, 0f), parent, roomRoot, 0.85f);
            }

            Vector3 rightSinkDrawerPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                new Vector3(3.05f, -0.2f, 0f));
            GameObject rightSinkDrawer = SpawnConfiguredPrefab(drawerPrefab, rightSinkDrawerPos, parent, roomRoot);
            if (rightSinkDrawer != null)
            {
                SetItemSortingOrder(rightSinkDrawer, 10);
                used.Add(rightSinkDrawer.transform.position);
                spawnedAny = true;
                SpawnKitchenSurfaceItem(ladlePrefab, rightSinkDrawer.transform.position + new Vector3(0.02f, 1.12f, 0f), parent, roomRoot, 0.85f);
            }

            Vector3 originalTableDrawerPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomCenter,
                new Vector3(-3.65f, 1.05f, 0f));
            GameObject originalTableDrawer = SpawnConfiguredPrefab(drawerPrefab, originalTableDrawerPos, parent, roomRoot);
            if (originalTableDrawer != null)
            {
                SetItemSortingOrder(originalTableDrawer, 10);
                used.Add(originalTableDrawer.transform.position);
                spawnedAny = true;
                SpawnKitchenSurfaceItem(mittensPrefab, originalTableDrawer.transform.position + new Vector3(-0.26f, 1.05f, 0f), parent, roomRoot, 1f);

                Vector3 apronDrawerPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                    roomRoot,
                    RoomDecorInteriorAnchor.InteriorBottomCenter,
                    new Vector3(1.65f, 1.05f, 0f));
                GameObject apronDrawer = SpawnConfiguredPrefab(drawerPrefab, apronDrawerPos, parent, roomRoot);
                if (apronDrawer != null)
                {
                    SetItemSortingOrder(apronDrawer, 10);
                    used.Add(apronDrawer.transform.position);
                    spawnedAny = true;
                    SpawnKitchenSurfaceItem(apronPrefab, apronDrawer.transform.position + new Vector3(0.06f, 1.05f, 0f), parent, roomRoot, 0.72f);
                }
            }
        }

        if (dishwasherPrefab != null)
        {
            Vector3 dishwasherPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomRight,
                new Vector3(-0.45f, 0.72f, 0f));
            GameObject dishwasher = SpawnConfiguredPrefab(dishwasherPrefab, dishwasherPos, parent, roomRoot);
            if (dishwasher != null)
            {
                used.Add(dishwasher.transform.position);
                spawnedAny = true;
            }
        }

        if (tablePrefab != null)
        {
            Vector3 tablePos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomCenter,
                new Vector3(-1.0f, 1.05f, 0f));
            GameObject table = SpawnConfiguredPrefab(tablePrefab, tablePos, parent, roomRoot);
            if (table != null)
            {
                used.Add(table.transform.position);
                spawnedAny = true;
                SpawnKitchenSurfaceItem(fryingPanPrefab, table.transform.position + new Vector3(0.02f, 0.88f, 0f), parent, roomRoot, 0.75f);
            }
        }

        return spawnedAny;
    }

    private static bool PlaceBathroomLayout(
        Transform roomRoot,
        Transform parent,
        List<GameObject> bathroomPrefabs,
        List<Vector2> used)
    {
        GameObject sinkPrefab = FindByToken(bathroomPrefabs, "sink");
        GameObject towelBasketPrefab = FindByToken(bathroomPrefabs, "towel_basket") ?? FindByToken(bathroomPrefabs, "towel basket");
        GameObject trayPrefab = FindByToken(bathroomPrefabs, "tray");
        GameObject curtainPrefab = FindByToken(bathroomPrefabs, "curtain");
        GameObject cabinetPrefab = FindByToken(bathroomPrefabs, "cabinet") ?? FindByToken(bathroomPrefabs, "cupboard");
        GameObject monsteraPrefab = FindByToken(bathroomPrefabs, "monstera");
        bool spawnedAny = false;

        if (sinkPrefab != null)
        {
            Vector3 sinkPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorTopCenter,
                new Vector3(-1.15f, -0.74f, 0f));
            GameObject sink = SpawnConfiguredPrefab(sinkPrefab, sinkPos, parent, roomRoot);
            if (sink != null)
            {
                used.Add(sink.transform.position);
                spawnedAny = true;
            }
        }

        if (towelBasketPrefab != null)
        {
            Vector3 basketPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorTopCenter,
                new Vector3(1.1f, -1.05f, 0f));
            GameObject basket = SpawnConfiguredPrefab(towelBasketPrefab, basketPos, parent, roomRoot);
            if (basket != null)
            {
                used.Add(basket.transform.position);
                spawnedAny = true;
            }
        }

        if (trayPrefab != null)
        {
            Vector3 trayPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomCenter,
                new Vector3(0f, 0.92f, 0f));
            GameObject tray = SpawnConfiguredPrefab(trayPrefab, trayPos, parent, roomRoot);
            if (tray != null)
            {
                used.Add(tray.transform.position);
                spawnedAny = true;
            }
        }

        if (curtainPrefab != null)
        {
            Vector3 curtainPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorTopRight,
                new Vector3(-0.72f, -0.72f, 0f));
            GameObject curtain = SpawnConfiguredPrefab(curtainPrefab, curtainPos, parent, roomRoot);
            if (curtain != null)
            {
                used.Add(curtain.transform.position);
                spawnedAny = true;
            }
        }

        if (monsteraPrefab != null)
        {
            Vector3 monsteraPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorTopLeft,
                new Vector3(0.55f, -0.78f, 0f));
            GameObject monstera = SpawnConfiguredPrefab(monsteraPrefab, monsteraPos, parent, roomRoot);
            if (monstera != null)
            {
                used.Add(monstera.transform.position);
                spawnedAny = true;
            }
        }

        if (cabinetPrefab != null)
        {
            Vector3[] cabinetSlots =
            {
                RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomLeft, new Vector3(0.85f, 0.62f, 0f)),
                RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomRight, new Vector3(-0.85f, 0.62f, 0f)),
            };

            for (int i = 0; i < cabinetSlots.Length; i++)
            {
                GameObject cabinet = SpawnConfiguredPrefab(cabinetPrefab, cabinetSlots[i], parent, roomRoot);
                if (cabinet == null)
                    continue;
                used.Add(cabinet.transform.position);
                spawnedAny = true;
            }
        }

        return spawnedAny;
    }

    private static bool PlaceCafeteriaLayout(
        Transform roomRoot,
        Transform parent,
        List<GameObject> cafeteriaPrefabs,
        List<Vector2> used)
    {
        List<GameObject> tablePrefabs = new List<GameObject>();
        List<GameObject> chairPrefabs = new List<GameObject>();
        List<GameObject> trashPrefabs = new List<GameObject>();
        for (int i = 0; i < cafeteriaPrefabs.Count; i++)
        {
            GameObject prefab = cafeteriaPrefabs[i];
            if (prefab == null)
                continue;

            string name = prefab.name;
            if (HasToken(name, "table"))
                tablePrefabs.Add(prefab);
            else if (HasToken(name, "chair"))
                chairPrefabs.Add(prefab);
            else if (HasToken(name, "trash"))
                trashPrefabs.Add(prefab);
        }

        bool spawnedAny = false;
        Vector3[] trashSlots =
        {
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(3.65f, -0.55f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopRight, new Vector3(-3.65f, -0.55f, 0f)),
        };

        for (int i = 0; i < Mathf.Min(trashSlots.Length, trashPrefabs.Count > 0 ? trashSlots.Length : 0); i++)
        {
            GameObject trashPrefab = trashPrefabs[i % trashPrefabs.Count];
            GameObject trash = SpawnConfiguredPrefab(trashPrefab, trashSlots[i], parent, roomRoot, skipShoppingListPickup: true, makeDecorOnly: true);
            if (trash == null)
                continue;
            used.Add(trash.transform.position);
            spawnedAny = true;
        }

        Vector3[] tableSlots =
        {
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(1.25f, -1.05f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopRight, new Vector3(-1.25f, -1.05f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomLeft, new Vector3(1.25f, 1.05f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomRight, new Vector3(-1.25f, 1.05f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorMiddleCenter, Vector3.zero),
        };

        for (int i = 0; i < Mathf.Min(tableSlots.Length, tablePrefabs.Count > 0 ? tableSlots.Length : 0); i++)
        {
            GameObject tablePrefab = tablePrefabs[i % tablePrefabs.Count];
            GameObject table = SpawnConfiguredPrefab(tablePrefab, tableSlots[i], parent, roomRoot, skipShoppingListPickup: true, makeDecorOnly: true);
            if (table == null)
                continue;
            RebuildSolidPropCollider(table, 0.82f, 0.62f);
            used.Add(table.transform.position);
            spawnedAny = true;
        }

        Vector3[] chairSlots =
        {
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(2.55f, -0.95f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopLeft, new Vector3(0.25f, -0.95f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopRight, new Vector3(-2.55f, -0.95f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorTopRight, new Vector3(-0.25f, -0.95f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomLeft, new Vector3(2.55f, 0.95f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomLeft, new Vector3(0.25f, 0.95f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomRight, new Vector3(-2.55f, 0.95f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorBottomRight, new Vector3(-0.25f, 0.95f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorMiddleLeft, new Vector3(0.9f, 0f, 0f)),
            RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorMiddleRight, new Vector3(-0.9f, 0f, 0f)),
        };

        for (int i = 0; i < Mathf.Min(chairSlots.Length, chairPrefabs.Count > 0 ? chairSlots.Length : 0); i++)
        {
            GameObject chairPrefab = chairPrefabs[i % chairPrefabs.Count];
            GameObject chair = SpawnConfiguredPrefab(chairPrefab, chairSlots[i], parent, roomRoot, skipShoppingListPickup: true, makeDecorOnly: true);
            if (chair == null)
                continue;
            RebuildSolidPropCollider(chair, 0.7f, 0.7f);
            used.Add(chair.transform.position);
            spawnedAny = true;
        }

        return spawnedAny;
    }

    private static bool PlaceLivingRoomLayout(
        Transform roomRoot,
        Transform parent,
        RoomPrefabSpawnCatalog catalog,
        List<GameObject> livingRoomPrefabs,
        List<Vector2> used)
    {
        GameObject couchPrefab = FindByToken(livingRoomPrefabs, "couch") ?? FindByToken(livingRoomPrefabs, "sofa");
        GameObject coffeeTablePrefab = FindByToken(livingRoomPrefabs, "coffee") ?? FindByToken(livingRoomPrefabs, "coffeetable");
        GameObject remotePrefab = FindByToken(livingRoomPrefabs, "remote");
        GameObject cabinetPrefab = FindByToken(livingRoomPrefabs, "cabinet");
        GameObject lampPrefab = FindByToken(livingRoomPrefabs, "lamp");
        GameObject housePlantPrefab = FindByToken(livingRoomPrefabs, "houseplant") ?? FindByToken(livingRoomPrefabs, "plant");
        GameObject tieredCartPrefab = FindByToken(livingRoomPrefabs, "tiered") ?? FindByToken(livingRoomPrefabs, "cart");
        GameObject monsteraPrefab = FindByToken(livingRoomPrefabs, "monstera");
        if (monsteraPrefab == null && catalog != null)
            monsteraPrefab = FindByToken(catalog.GetAllPrefabs(RoomType.Bathroom), "monstera");
        bool wideRoom = IsRoomWiderThanTall(roomRoot);
        bool spawnedAny = false;

        Vector3 tableOffset = wideRoom ? new Vector3(0f, -2.75f, 0f) : new Vector3(0f, -1.35f, 0f);
        Vector3 remoteOffset = wideRoom ? new Vector3(0.42f, -2.22f, 0f) : new Vector3(1.28f, -0.55f, 0f);
        Vector3 leftCabinetOffset = wideRoom ? new Vector3(1.55f, -0.72f, 0f) : new Vector3(0.85f, -0.72f, 0f);
        Vector3 rightCabinetOffset = wideRoom ? new Vector3(-1.55f, -0.72f, 0f) : new Vector3(-0.85f, -0.72f, 0f);
        RoomDecorInteriorAnchor rightCabinetAnchor = RoomDecorInteriorAnchor.InteriorTopRight;
        Vector3 plantOffset = wideRoom ? new Vector3(2.75f, -0.75f, 0f) : new Vector3(1.08f, 0.62f, 0f);
        Vector3 lampOffset = wideRoom ? new Vector3(-3.35f, -0.75f, 0f) : new Vector3(-1.08f, 0.62f, 0f);
        Vector3 couchOffset = wideRoom ? new Vector3(0f, -0.15f, 0f) : new Vector3(-0.2f, 2.05f, 0f);

        if (coffeeTablePrefab != null)
        {
            Vector3 tablePos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                tableOffset);
            GameObject table = SpawnConfiguredPrefab(coffeeTablePrefab, tablePos, parent, roomRoot);
            if (table != null)
            {
                SetItemSortingOrder(table, 10);
                used.Add(table.transform.position);
                spawnedAny = true;
            }
        }

        if (remotePrefab != null)
        {
            Vector3 remotePos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                remoteOffset);
            GameObject remote = SpawnConfiguredPrefab(remotePrefab, remotePos, parent, roomRoot);
            if (remote != null)
            {
                ConfigureRemoteOnCoffeeTable(remote);
                used.Add(remote.transform.position);
                spawnedAny = true;
            }
        }

        if (cabinetPrefab != null)
        {
            Vector3 leftCabinetPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorTopLeft,
                leftCabinetOffset);
            GameObject leftCabinet = SpawnConfiguredPrefab(cabinetPrefab, leftCabinetPos, parent, roomRoot);
            if (leftCabinet != null)
            {
                used.Add(leftCabinet.transform.position);
                spawnedAny = true;
            }

            Vector3 rightCabinetPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                rightCabinetAnchor,
                rightCabinetOffset);
            GameObject rightCabinet = SpawnConfiguredPrefab(cabinetPrefab, rightCabinetPos, parent, roomRoot);
            if (rightCabinet != null)
            {
                used.Add(rightCabinet.transform.position);
                spawnedAny = true;
            }
        }

        if (housePlantPrefab != null)
        {
            Vector3 plantPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleLeft,
                plantOffset);
            GameObject plant = SpawnConfiguredPrefab(housePlantPrefab, plantPos, parent, roomRoot);
            if (plant != null)
            {
                used.Add(plant.transform.position);
                spawnedAny = true;
            }
        }

        if (lampPrefab != null)
        {
            Vector3 lampPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleRight,
                lampOffset);
            GameObject lamp = SpawnConfiguredPrefab(lampPrefab, lampPos, parent, roomRoot);
            if (lamp != null)
            {
                used.Add(lamp.transform.position);
                spawnedAny = true;
            }
        }


        if (tieredCartPrefab != null)
        {
            Vector3 cartPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomCenter,
                wideRoom ? new Vector3(0f, 0.3f, 0f) : new Vector3(0f, 0.8f, 0f));
            GameObject cart = SpawnConfiguredPrefab(tieredCartPrefab, cartPos, parent, roomRoot);
            if (cart != null)
            {
                used.Add(cart.transform.position);
                spawnedAny = true;
            }
        }

        if (monsteraPrefab != null)
        {
            Vector3 leftMonsteraPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomLeft,
                wideRoom ? new Vector3(2.35f, 0.78f, 0f) : new Vector3(0.82f, 2.35f, 0f));
            GameObject leftMonstera = SpawnConfiguredPrefab(monsteraPrefab, leftMonsteraPos, parent, roomRoot);
            if (leftMonstera != null)
            {
                used.Add(leftMonstera.transform.position);
                spawnedAny = true;
            }

            Vector3 rightMonsteraPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomRight,
                wideRoom ? new Vector3(-2.35f, 0.78f, 0f) : new Vector3(-0.82f, 2.35f, 0f));
            GameObject rightMonstera = SpawnConfiguredPrefab(monsteraPrefab, rightMonsteraPos, parent, roomRoot);
            if (rightMonstera != null)
            {
                used.Add(rightMonstera.transform.position);
                spawnedAny = true;
            }
        }

        if (couchPrefab != null)
        {
            Vector3 topCouchPos = RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                couchOffset);
            GameObject topCouch = SpawnConfiguredPrefab(couchPrefab, topCouchPos, Quaternion.identity, parent, roomRoot);
            if (topCouch != null)
            {
                used.Add(topCouch.transform.position);
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

        List<GameObject> orderedBeds = BuildOrderedBedroomBeds(bedroomPrefabs);
        if (orderedBeds.Count > 0)
            bedPrefabs = orderedBeds;
        else if (preferredBeds.Count > 0)
            bedPrefabs = preferredBeds;

        if (bedPrefabs.Count == 0)
            return false;

        GameObject lampPrefab = FindByToken(bedroomPrefabs, "lamp");
        GameObject drawerPrefab = FindByToken(bedroomPrefabs, "drawer");
        List<GameObject> plushPrefabs = FindBedroomPlushPrefabs(bedroomPrefabs);
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
            new Vector3(1.48f, -0.92f, 0f),
            new Vector3(-1.48f, -0.92f, 0f),
            new Vector3(1.48f, 0.92f, 0f),
            new Vector3(-1.48f, 0.92f, 0f),
        };

        float[] bedScaleVariants = new[] { 0.72f, 0.72f, 0.72f, 0.72f };
        Vector3[] lampOffsets = new[]
        {
            new Vector3(-1.28f, -0.18f, 0f),
            new Vector3(1.28f, -0.18f, 0f),
            new Vector3(-1.28f, 0.18f, 0f),
            new Vector3(1.28f, 0.18f, 0f)
        };
        Vector3[] drawerOffsets = new[]
        {
            new Vector3(1.48f, 0f, 0f),
            new Vector3(-1.48f, 0f, 0f),
            new Vector3(1.48f, 0f, 0f),
            new Vector3(-1.48f, 0f, 0f)
        };
        bool spawnedAny = false;

        for (int i = 0; i < 4; i++)
        {
            int slot = i;
            GameObject bedPrefab = bedPrefabs[i % bedPrefabs.Count];
            Vector3 bedPos = RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, anchors[slot], bedOffsets[slot]);
            bedPos = ResolveBedroomLocalSlot(bedPos, used);
            GameObject bed = Object.Instantiate(bedPrefab, bedPos, Quaternion.identity, parent);
            bed.SetActive(false);
            StripLegacySpawnerPath(bed);
            NormalizeSpawnedVisuals(bed);
            ClampInstanceInsideRoom(bed, roomRoot);

            float s = bedScaleVariants[i % bedScaleVariants.Length];
            bed.transform.localScale = Vector3.Scale(bed.transform.localScale, new Vector3(s, s, 1f));
            EnsureSolidPropCollider(bed);
            SpriteRenderer bedRenderer = bed.GetComponentInChildren<SpriteRenderer>();
            if (bedRenderer != null)
                bedRenderer.flipX = (slot % 2) == 1;

            RoomSpawnPrefabDefinition bedDef = GetRoomSpawnDefinition(bedPrefab);
            EnsureShoppingListPickupComponent(bed, bedDef);
            bed.SetActive(true);
            used.Add(bed.transform.position);
            spawnedAny = true;

            Vector3 sideA = ResolveBedroomLocalSlot(bed.transform.position + lampOffsets[slot], used);
            Vector3 sideB = ResolveBedroomLocalSlot(bed.transform.position + drawerOffsets[slot], used);

            if (lampPrefab != null)
            {
                GameObject lamp = Object.Instantiate(lampPrefab, sideA, Quaternion.identity, parent);
                lamp.SetActive(false);
                StripLegacySpawnerPath(lamp);
                NormalizeSpawnedVisuals(lamp);
                ClampInstanceInsideRoom(lamp, roomRoot);
                EnsureSolidPropCollider(lamp);
                RoomSpawnPrefabDefinition lampDef = GetRoomSpawnDefinition(lampPrefab);
                EnsureShoppingListPickupComponent(lamp, lampDef);
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
                SetItemSortingOrder(drawer, 10);
                EnsureSolidPropCollider(drawer);
                RoomSpawnPrefabDefinition drawerDef = GetRoomSpawnDefinition(drawerPrefab);
                EnsureShoppingListPickupComponent(drawer, drawerDef);
                drawer.SetActive(true);
                used.Add(drawer.transform.position);
                spawnedAny = true;

                GameObject plushPrefab = FindBedroomPlushForBed(bedPrefab, plushPrefabs);
                if (plushPrefab != null)
                {
                    Vector3 plushPos = drawer.transform.position + GetPlushOnDrawerOffset(slot);
                    GameObject plush = Object.Instantiate(plushPrefab, plushPos, Quaternion.identity, parent);
                    plush.SetActive(false);
                    StripLegacySpawnerPath(plush);
                    NormalizeSpawnedVisuals(plush);
                    ConfigurePlushOnDrawer(plush);
                    RoomSpawnPrefabDefinition plushDef = GetRoomSpawnDefinition(plushPrefab);
                    EnsureShoppingListPickupComponent(plush, plushDef);
                    plush.SetActive(true);
                    spawnedAny = true;
                }
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
        else if (roomType == RoomType.Kitchen)
        {
            Vector3 kitchen = GetKitchenPinnedPosition(roomRoot, prefab);
            if (kitchen != Vector3.zero)
                return kitchen;
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

    private static bool IsRoomWiderThanTall(Transform roomRoot)
    {
        if (roomRoot == null)
            return true;

        Collider2D best = null;
        float bestArea = 0f;
        Collider2D[] colliders = roomRoot.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D c = colliders[i];
            if (c == null || !c.isTrigger)
                continue;
            float area = c.bounds.size.x * c.bounds.size.y;
            if (area > bestArea)
            {
                bestArea = area;
                best = c;
            }
        }

        return best == null || best.bounds.size.x >= best.bounds.size.y;
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
        if (name.Contains("tiered") || name.Contains("cart"))
            return RoomDecorationPlacer.GetAnchoredWorldPosition(roomRoot, RoomDecorInteriorAnchor.InteriorMiddleCenter, new Vector3(0f, -1.85f, 0f));
        return Vector3.zero;
    }

    private static Vector3 GetLivingRoomPinnedPosition(Transform roomRoot, GameObject prefab)
    {
        if (roomRoot == null || prefab == null)
            return Vector3.zero;

        string name = prefab.name.ToLowerInvariant();
        if (name.Contains("couch") || name.Contains("sofa"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                IsRoomWiderThanTall(roomRoot) ? new Vector3(0f, 2.85f, 0f) : new Vector3(0f, 2.05f, 0f));
        }

        if (name.Contains("coffee") && name.Contains("table"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleCenter,
                IsRoomWiderThanTall(roomRoot) ? new Vector3(0f, -2.45f, 0f) : new Vector3(0f, -1.65f, 0f));
        }

        if (name.Contains("remote"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomRight,
                new Vector3(-1.15f, 0.9f, 0f));
        }

        if (name.Contains("picture"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorTopCenter,
                IsRoomWiderThanTall(roomRoot) ? new Vector3(0f, -0.95f, 0f) : new Vector3(0f, -1.45f, 0f));
        }

        if (name.Contains("houseplant"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleLeft,
                IsRoomWiderThanTall(roomRoot) ? new Vector3(0.9f, -0.15f, 0f) : new Vector3(1.15f, -0.15f, 0f));
        }

        return Vector3.zero;
    }

    private static Vector3 GetKitchenPinnedPosition(Transform roomRoot, GameObject prefab)
    {
        if (roomRoot == null || prefab == null)
            return Vector3.zero;

        string name = prefab.name.ToLowerInvariant();
        if (name.Contains("frying") && name.Contains("pan"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorBottomLeft,
                new Vector3(1.05f, 0.82f, 0f));
        }

        if (name.Contains("apron"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleLeft,
                new Vector3(0.6f, 0.15f, 0f));
        }

        if (name.Contains("whisk"))
        {
            return RoomDecorationPlacer.GetAnchoredWorldPosition(
                roomRoot,
                RoomDecorInteriorAnchor.InteriorMiddleLeft,
                new Vector3(0.9f, 0.25f, 0f));
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
    private static RoomSpawnPrefabDefinition GetRoomSpawnDefinition(GameObject obj)
    {
        if (obj == null)
            return null;

        RoomSpawnPrefabDefinition def = obj.GetComponent<RoomSpawnPrefabDefinition>();
        if (def == null)
            def = obj.GetComponentInChildren<RoomSpawnPrefabDefinition>(true);

        return def;
    }

    private static bool IsBedroomLayoutPiece(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return HasToken(prefab.name, "bed") ||
               HasToken(prefab.name, "lamp") ||
               HasToken(prefab.name, "drawer") ||
               HasToken(prefab.name, "plush");
    }

    private static bool IsKitchenLayoutPiece(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return HasToken(prefab.name, "microwave") ||
               HasToken(prefab.name, "sink") ||
               HasToken(prefab.name, "dishwasher") ||
               HasToken(prefab.name, "table") ||
               HasToken(prefab.name, "drawer") ||
               HasToken(prefab.name, "mittens") ||
               HasToken(prefab.name, "apron") ||
               HasToken(prefab.name, "knife") ||
               HasToken(prefab.name, "whisk") ||
               HasToken(prefab.name, "ladle") ||
               HasToken(prefab.name, "frying") ||
               HasToken(prefab.name, "pan") ||
               HasToken(prefab.name, "cupboard") ||
               HasToken(prefab.name, "cabinet");
    }

    private static bool IsBathroomLayoutPiece(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return HasToken(prefab.name, "sink") ||
               HasToken(prefab.name, "towel_basket") ||
               HasToken(prefab.name, "towel basket") ||
               HasToken(prefab.name, "curtain") ||
               HasToken(prefab.name, "tray") ||
               HasToken(prefab.name, "monstera") ||
               HasToken(prefab.name, "tiered") ||
               HasToken(prefab.name, "cart") ||
               HasToken(prefab.name, "cupboard") ||
               HasToken(prefab.name, "cabinet");
    }

    private static bool IsCafeteriaLayoutPiece(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return HasToken(prefab.name, "table") || HasToken(prefab.name, "chair") ||
               HasToken(prefab.name, "trash");
    }

    private static bool IsLivingRoomLayoutPiece(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return HasToken(prefab.name, "couch") ||
               HasToken(prefab.name, "sofa") ||
               HasToken(prefab.name, "coffee") ||
               HasToken(prefab.name, "coffeetable") ||
               HasToken(prefab.name, "remote") ||
               HasToken(prefab.name, "cabinet") ||
               HasToken(prefab.name, "curtain") ||
               HasToken(prefab.name, "lamp") ||
               HasToken(prefab.name, "houseplant") ||
               HasToken(prefab.name, "plant") ||
               HasToken(prefab.name, "monstera") ||
               HasToken(prefab.name, "tiered") ||
               HasToken(prefab.name, "cart");
    }

    private static bool IsPreferredBedroomBed(GameObject prefab)
    {
        if (prefab == null)
            return false;

        string n = prefab.name;
        return HasToken(n, "bed bee") ||
               HasToken(n, "bed cherry") ||
               HasToken(n, "bed night owl") ||
               HasToken(n, "bed spider");
    }

    private static List<GameObject> FindBedroomPlushPrefabs(List<GameObject> prefabs)
    {
        List<GameObject> result = new List<GameObject>();
        if (prefabs == null)
            return result;

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab != null && HasToken(prefab.name, "plush"))
                result.Add(prefab);
        }

        return result;
    }

    private static GameObject FindBedroomPlushForBed(GameObject bedPrefab, List<GameObject> plushPrefabs)
    {
        if (bedPrefab == null || plushPrefabs == null)
            return null;

        string bedName = bedPrefab.name;
        if (HasToken(bedName, "spider"))
            return FindByToken(plushPrefabs, "spider");
        if (HasToken(bedName, "night owl") || HasToken(bedName, "owl"))
            return FindByToken(plushPrefabs, "owl");
        if (HasToken(bedName, "cherry"))
            return FindByToken(plushPrefabs, "bird");
        if (HasToken(bedName, "bee"))
            return FindByToken(plushPrefabs, "bee");

        return null;
    }

    private static Vector3 GetPlushOnDrawerOffset(int slot)
    {
        float x = (slot % 2) == 0 ? -0.02f : 0.02f;
        return new Vector3(x, 0.78f, 0f);
    }

    private static void ConfigurePlushOnDrawer(GameObject plush)
    {
        if (plush == null)
            return;

        SpriteRenderer[] renderers = plush.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            renderers[i].sortingLayerName = "Item";
            renderers[i].sortingOrder = Mathf.Max(renderers[i].sortingOrder, 50);
        }

        EnsureSolidPropCollider(plush);
    }

    private static List<GameObject> BuildOrderedBedroomBeds(List<GameObject> prefabs)
    {
        List<GameObject> result = new List<GameObject>();
        AddFirstBedByToken(result, prefabs, "bed spider");
        AddFirstBedByToken(result, prefabs, "bed night owl");
        AddFirstBedByToken(result, prefabs, "bed bee");
        AddFirstBedByToken(result, prefabs, "bed cherry");
        return result;
    }

    private static void AddFirstBedByToken(List<GameObject> result, List<GameObject> prefabs, string token)
    {
        if (result == null || prefabs == null)
            return;

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null || IsLegacyBedroomBed(prefab) || !HasToken(prefab.name, token))
                continue;
            if (result.Contains(prefab))
                continue;

            result.Add(prefab);
            return;
        }
    }

    private static bool IsLegacyBedroomBed(GameObject prefab)
    {
        if (prefab == null)
            return false;

        string n = prefab.name == null ? string.Empty : prefab.name.Trim();
        return string.Equals(n, "Bed", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void ShuffleGameObjectList(List<GameObject> list)
    {
        if (list == null || list.Count < 2)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void ShuffleIntArray(int[] values)
    {
        if (values == null || values.Length < 2)
            return;

        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private static Vector3 ResolveBedroomLocalSlot(Vector3 preferred, List<Vector2> used)
    {
        if (!OverlapsUsed(preferred, used))
            return preferred;

        Vector3[] offsets =
        {
            new Vector3(0f, 0.62f, 0f),
            new Vector3(0f, -0.62f, 0f),
            new Vector3(0.58f, 0f, 0f),
            new Vector3(-0.58f, 0f, 0f),
            new Vector3(0.58f, 0.62f, 0f),
            new Vector3(-0.58f, -0.62f, 0f)
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

    private static void MakeLootItemDecorOnly(GameObject instance)
    {
        if (instance == null)
            return;

        RoomGeneratedPickup pickup = instance.GetComponent<RoomGeneratedPickup>();
        if (pickup != null)
            Object.DestroyImmediate(pickup);

        ItemWorldSpawner[] spawners = instance.GetComponentsInChildren<ItemWorldSpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
                Object.DestroyImmediate(spawners[i]);
        }
    }

    private static void EnsureShoppingListPickupComponent(GameObject instance, RoomSpawnPrefabDefinition def)
    {
        if (instance == null)
            return;

        if (def == null)
            def = instance.GetComponent<RoomSpawnPrefabDefinition>();

        if (def == null)
            def = instance.GetComponentInChildren<RoomSpawnPrefabDefinition>(true);

        if (def == null)
            return;

        if (!def.isPickable)
            return;

        if (def.spawnCategory != RoomSpawnCategory.Item)
            return;

        ItemWorldSpawner spawner = instance.GetComponent<ItemWorldSpawner>();
        if (spawner == null)
            spawner = instance.GetComponentInChildren<ItemWorldSpawner>(true);

        if (spawner == null)
            return;

        ItemDefinition itemDef = spawner.ItemDefinition;
        if (itemDef == null)
            return;

        if (!itemDef.IsLoot())
            return;

        if (!itemDef.canAppearInShoppingList)
            return;

        string key = itemDef.GetShoppingListKey();
        if (string.IsNullOrWhiteSpace(key))
            return;

        RoomGeneratedPickup pickup = instance.GetComponent<RoomGeneratedPickup>();
        if (pickup == null)
            pickup = instance.AddComponent<RoomGeneratedPickup>();
    }

    private static void StripLegacySpawnerPath(GameObject instance)
    {
        //if (instance == null)
        //    return;

        //ItemWorldSpawner[] spawners = instance.GetComponentsInChildren<ItemWorldSpawner>(true);
        //for (int i = 0; i < spawners.Length; i++)
        //{
        //    if (spawners[i] != null)
        //        Object.DestroyImmediate(spawners[i]);
        //}
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

    private static void SetItemSortingOrder(GameObject instance, int sortingOrder)
    {
        if (instance == null)
            return;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            renderers[i].sortingLayerName = "Item";
            renderers[i].sortingOrder = sortingOrder;
        }
    }

    private static void ConfigureRemoteOnCoffeeTable(GameObject remote)
    {
        if (remote == null)
            return;

        remote.transform.localScale = new Vector3(0.62f, 0.62f, remote.transform.localScale.z);
        SpriteRenderer[] renderers = remote.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            renderers[i].sortingLayerName = "Item";
            renderers[i].sortingOrder = Mathf.Max(renderers[i].sortingOrder, 120);
        }

        RebuildSolidPropCollider(remote);
    }

    private static GameObject SpawnKitchenSurfaceItem(
        GameObject prefab,
        Vector3 position,
        Transform parent,
        Transform roomRoot,
        float scaleMultiplier = 1f)
    {
        GameObject item = SpawnConfiguredPrefab(prefab, position, parent, roomRoot);
        if (item == null)
            return null;

        if (Mathf.Abs(scaleMultiplier - 1f) > 0.001f)
        {
            item.transform.localScale = Vector3.Scale(
                item.transform.localScale,
                new Vector3(scaleMultiplier, scaleMultiplier, 1f));
        }

        SetItemSortingOrder(item, 80);
        RebuildSolidPropCollider(item);
        return item;
    }

    private static void RebuildSolidPropCollider(GameObject instance)
    {
        RebuildSolidPropCollider(instance, 0.3f, 0.25f);
    }

    private static void RebuildSolidPropCollider(GameObject instance, float widthMultiplier, float heightMultiplier)
    {
        if (instance == null)
            return;

        Transform existing = instance.transform.Find("SolidCollider");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        EnsureSolidPropCollider(instance, widthMultiplier, heightMultiplier);
    }

    private static void EnsureSolidPropCollider(GameObject instance)
    {
        EnsureSolidPropCollider(instance, 0.3f, 0.25f);
    }

    private static void EnsureSolidPropCollider(GameObject instance, float widthMultiplier, float heightMultiplier)
    {
        if (instance == null)
            return;
        if (instance.transform.Find("SolidCollider") != null)
            return;

        SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null || sr.sprite == null)
            return;

        Bounds spriteBounds = sr.sprite.bounds;
        Vector3 worldCenter = sr.transform.TransformPoint(spriteBounds.center);

        GameObject solid = new GameObject("SolidCollider");
        solid.layer = 6;
        solid.transform.SetParent(instance.transform, false);
        solid.transform.position = worldCenter;
        solid.transform.rotation = Quaternion.identity;

        Vector3 localSize = spriteBounds.size;
        Vector3 spriteScale = sr.transform.lossyScale;
        Vector3 solidScale = solid.transform.lossyScale;
        float width = Mathf.Abs(localSize.x * spriteScale.x / Mathf.Max(0.0001f, Mathf.Abs(solidScale.x)));
        float height = Mathf.Abs(localSize.y * spriteScale.y / Mathf.Max(0.0001f, Mathf.Abs(solidScale.y)));

        BoxCollider2D box = solid.AddComponent<BoxCollider2D>();
        box.isTrigger = false;
        box.offset = Vector2.zero;
        box.size = new Vector2(
            Mathf.Max(0.09f, width * widthMultiplier),
            Mathf.Max(0.09f, height * heightMultiplier));

        Rigidbody2D body = solid.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;
    }

    private static GameObject SpawnConfiguredPrefab(
    GameObject prefab,
    Vector3 position,
    Quaternion rotation,
    Transform parent,
    Transform roomRoot,
    bool makePushableProp = false,
    bool skipShoppingListPickup = false,
    bool makeDecorOnly = false)
    {
        if (prefab == null)
            return null;

        RoomSpawnPrefabDefinition def = GetRoomSpawnDefinition(prefab);

        Transform targetContainer = ResolveContainerForSpawnCategory(roomRoot, def);
        Transform instantiateParent =
            def != null && def.spawnCategory == RoomSpawnCategory.Decoration
                ? (parent != null ? parent : targetContainer)
                : (targetContainer != null ? targetContainer : parent);

        if (instantiateParent == null)
            instantiateParent = roomRoot;

        GameObject instance = Object.Instantiate(prefab, position, rotation, instantiateParent);
        instance.SetActive(false);

        StripLegacySpawnerPath(instance);
        NormalizeSpawnedVisuals(instance);
        ClampInstanceInsideRoom(instance, roomRoot);

        // Critical: if this prefab contains ItemWorldSpawner, force its real spawned ItemWorld
        // into the correct room-local container instead of scene root.
        AssignSpawnParentToAnyItemWorldSpawner(
            instance,
            targetContainer != null ? targetContainer : instantiateParent);

        if (makeDecorOnly)
            MakeLootItemDecorOnly(instance);

        if (makePushableProp)
            ConfigurePushablePropPhysics(instance);
        else
            EnsureSolidPropCollider(instance);

        if (!skipShoppingListPickup && !makeDecorOnly)
        {
            EnsureShoppingListPickupComponent(instance, def);
        }

        instance.SetActive(true);
        return instance;
    }

    private static GameObject SpawnConfiguredPrefab(
        GameObject prefab,
        Vector3 position,
        Transform parent,
        Transform roomRoot,
        bool makePushableProp = false,
        bool skipShoppingListPickup = false,
        bool makeDecorOnly = false)
    {
        return SpawnConfiguredPrefab(
            prefab,
            position,
            Quaternion.identity,
            parent,
            roomRoot,
            makePushableProp,
            skipShoppingListPickup,
            makeDecorOnly);
    }

    private static void ConfigurePushablePropPhysics(GameObject instance)
    {
        if (instance == null)
            return;

        // Use the same solid-collision layer as room walls so the player cannot ghost through cafeteria props.
        instance.layer = 6;
        Transform[] children = instance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null)
                children[i].gameObject.layer = 6;
        }

        Collider[] legacy3d = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < legacy3d.Length; i++)
        {
            if (legacy3d[i] != null)
                Object.DestroyImmediate(legacy3d[i]);
        }

        BoxCollider2D box = instance.GetComponent<BoxCollider2D>();
        if (box == null)
            box = instance.AddComponent<BoxCollider2D>();

        SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null && sr.sprite != null)
        {
            Vector2 size = sr.sprite.bounds.size;
            box.size = new Vector2(Mathf.Max(0.45f, size.x * 0.8f), Mathf.Max(0.45f, size.y * 0.8f));
            box.offset = (Vector2)sr.transform.localPosition;
        }
        box.isTrigger = false;

        Rigidbody2D body = instance.GetComponent<Rigidbody2D>();
        if (body == null)
            body = instance.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.linearDamping = 6f;
        body.angularDamping = 12f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        RoomGeneratedPickup pickup = instance.GetComponent<RoomGeneratedPickup>();
        if (pickup != null)
            Object.DestroyImmediate(pickup);
    }
    //private static Transform ResolveRoomLootRoot(Transform roomRoot)
    //{
    //    if (roomRoot == null)
    //        return null;

    //    Transform root = roomRoot.Find("SpawnedLoots");
    //    if (root != null)
    //        return root;

    //    //root = roomRoot.Find("SpawnedLoot");
    //    //if (root != null)
    //    //    return root;

    //    GameObject created = new GameObject("SpawnedLoots");
    //    created.transform.SetParent(roomRoot, false);
    //    created.transform.localPosition = Vector3.zero;
    //    created.transform.localRotation = Quaternion.identity;
    //    created.transform.localScale = Vector3.one;
    //    return created.transform;
    //}

    private static void AssignSpawnParentToAnyItemWorldSpawner(GameObject instance, Transform container)
    {
        if (instance == null)
            return;

        ItemWorldSpawner[] spawners = instance.GetComponentsInChildren<ItemWorldSpawner>(true);

        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;

            Transform finalParent = container;

            // 关键兜底（就算 container 传进来是 null）
            if (finalParent == null)
            {
                Room room = spawner.GetComponentInParent<Room>();
                if (room != null)
                {
                    finalParent =
                        room.transform.Find("SpawnedLoots") ??
                        room.transform.Find("SpawnedItems") ??
                        room.transform.Find("SpawnedObjects") ??
                        room.transform;
                }
            }

            spawner.SetSpawnParent(finalParent);

        }
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
}
