using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Room Generation/Prefab Spawn Catalog")]
public class RoomPrefabSpawnCatalog : ScriptableObject
{
    [Serializable]
    public class RoomPool
    {
        public RoomType roomType = RoomType.None;
        public List<GameObject> prefabs = new List<GameObject>();
    }

    [SerializeField] private List<RoomPool> pools = new List<RoomPool>();
    [Header("Folder Routing")]
    [SerializeField] private bool autoRouteFromLootFolders = true;
    [SerializeField] private string lootRootFolder = "Assets/Prefabs/Loots";
    [SerializeField] private string weaponRootFolder = "Assets/Prefabs/Weapons";

    public IEnumerable<RoomPool> Pools => pools;

    public List<GameObject> GetAllPrefabs(RoomType roomType)
    {
#if UNITY_EDITOR
        EnsureAutoBuiltInEditor();
#endif
        List<GameObject> result = new List<GameObject>();
        for (int i = 0; i < pools.Count; i++)
        {
            RoomPool pool = pools[i];
            if (pool == null || pool.roomType != roomType || pool.prefabs == null)
                continue;
            for (int j = 0; j < pool.prefabs.Count; j++)
            {
                GameObject prefab = pool.prefabs[j];
                if (prefab != null)
                    result.Add(prefab);
            }
        }

        return result;
    }

    public List<GameObject> GetPrefabs(RoomType roomType, RoomSpawnCategory category)
    {
#if UNITY_EDITOR
        EnsureAutoBuiltInEditor();
#endif
        List<GameObject> result = new List<GameObject>();
        for (int i = 0; i < pools.Count; i++)
        {
            RoomPool pool = pools[i];
            if (pool == null || pool.roomType != roomType || pool.prefabs == null)
                continue;
            for (int j = 0; j < pool.prefabs.Count; j++)
            {
                GameObject prefab = pool.prefabs[j];
                if (prefab == null)
                    continue;
                RoomSpawnPrefabDefinition def = prefab.GetComponent<RoomSpawnPrefabDefinition>();
                if (def != null && def.spawnCategory == category)
                    result.Add(prefab);
            }
        }

        return result;
    }

#if UNITY_EDITOR
    private void EnsureAutoBuiltInEditor()
    {
        if (autoRouteFromLootFolders)
            RebuildPoolsFromFolders();
    }

    private void OnValidate()
    {
        if (!autoRouteFromLootFolders)
            return;
        RebuildPoolsFromFolders();
    }

    [ContextMenu("Rebuild Pools From Loot Folders")]
    public void RebuildPoolsFromFolders()
    {
        Dictionary<RoomType, List<GameObject>> map = new Dictionary<RoomType, List<GameObject>>();
        foreach (RoomType rt in Enum.GetValues(typeof(RoomType)))
        {
            if (rt == RoomType.None)
                continue;
            map[rt] = new List<GameObject>();
        }

        List<string> roots = new List<string>();
        if (!string.IsNullOrWhiteSpace(lootRootFolder))
            roots.Add(lootRootFolder);
        if (!string.IsNullOrWhiteSpace(weaponRootFolder))
            roots.Add(weaponRootFolder);

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", roots.ToArray());
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;
            AddPrefabToMappedRooms(path, prefab, map);
        }

        pools.Clear();
        foreach (var kv in map)
        {
            RoomPool pool = new RoomPool
            {
                roomType = kv.Key,
                prefabs = kv.Value
            };
            pools.Add(pool);
        }

        EditorUtility.SetDirty(this);
    }

    private static void AddPrefabToMappedRooms(
        string assetPath,
        GameObject prefab,
        Dictionary<RoomType, List<GameObject>> map)
    {
        if (prefab == null || map == null)
            return;

        if (string.IsNullOrWhiteSpace(assetPath))
            return;

        string normalized = assetPath.Replace('\\', '/');
        string[] parts = normalized.Split('/');

        int weaponsIdx = Array.IndexOf(parts, "Weapons");
        if (weaponsIdx >= 0)
        {
            map[RoomType.SportsRoom].Add(prefab);
            return;
        }

        int idx = Array.IndexOf(parts, "Loots");
        if (idx < 0 || idx + 1 >= parts.Length)
            return;
        string folder = parts[idx + 1];

        switch (folder)
        {
            case "Bathroom":
                map[RoomType.Bathroom].Add(prefab);
                return;
            case "Bedroom":
                map[RoomType.Bedroom].Add(prefab);
                return;
            case "Cafeteria":
                map[RoomType.Cafeteria].Add(prefab);
                return;
            case "Kitchen":
                map[RoomType.Kitchen].Add(prefab);
                return;
            case "LivingRoom":
                map[RoomType.LivingRoom].Add(prefab);
                return;
            case "SportsRoom":
                map[RoomType.SportsRoom].Add(prefab);
                return;
            case "Shared":
                AddSharedPrefabToAllowedRooms(prefab, map);
                return;
            default:
                return;
        }
    }

    private static void AddSharedPrefabToAllowedRooms(
        GameObject prefab,
        Dictionary<RoomType, List<GameObject>> map)
    {
        RoomSpawnPrefabDefinition spawnDef = prefab != null ? prefab.GetComponent<RoomSpawnPrefabDefinition>() : null;
        ItemDefinition itemDef = spawnDef != null ? spawnDef.GetItemDefinition() : null;
        if (itemDef != null && itemDef.allowedRoomTypes != null && itemDef.allowedRoomTypes.Count > 0)
        {
            for (int i = 0; i < itemDef.allowedRoomTypes.Count; i++)
            {
                RoomType roomType = itemDef.allowedRoomTypes[i];
                if (map.ContainsKey(roomType))
                    map[roomType].Add(prefab);
            }
            return;
        }

        map[RoomType.Bedroom].Add(prefab);
        map[RoomType.LivingRoom].Add(prefab);
    }
#endif
}
