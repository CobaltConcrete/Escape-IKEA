using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDefinition))]
public class ItemDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ItemDefinition itemDefinition = (ItemDefinition)target;

        // Basic Info
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stackable"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("uiScale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("glowColor"));

        EditorGUILayout.Space();

        // Category
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemCategory"));

        EditorGUILayout.Space();

        if (itemDefinition.itemCategory == ItemCategory.Normal)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("equipTag"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useEffect"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("effectValue"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("effectDuration"));
        }
        else if (itemDefinition.itemCategory == ItemCategory.Loot)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lootValue"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canAppearInShoppingList"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minRequiredAmount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRequiredAmount"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lootWorldPrefab"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowedRoomTypes"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bonusSpawnWeight"));
        }

        EditorGUILayout.Space();

        // World Display Settings
        EditorGUILayout.LabelField("World Display Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("worldDropScale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnFootprint"));

        EditorGUILayout.Space();

        // World Collider Settings
        EditorGUILayout.LabelField("World Collider Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("worldColliderType"));

        WorldColliderType type = (WorldColliderType)serializedObject.FindProperty("worldColliderType").enumValueIndex;

        switch (type)
        {
            case WorldColliderType.Box:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boxOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boxSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("boxEdgeRadius"));
                break;

            case WorldColliderType.Circle:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("circleOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("circleRadius"));
                break;
        }

        EditorGUILayout.Space();

        // World Object Settings
        EditorGUILayout.LabelField("World Object Settings", EditorStyles.boldLabel);
        itemDefinition.worldTag = EditorGUILayout.TagField("World Tag", itemDefinition.worldTag);
        itemDefinition.worldLayer = EditorGUILayout.LayerField("World Layer", itemDefinition.worldLayer);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(itemDefinition);
        }
    }
}