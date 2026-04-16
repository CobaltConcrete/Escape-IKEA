using UnityEditor;
using UnityEngine;

public static class Convert3DCollidersTo2D
{
    [MenuItem("Tools/Loot/Convert Selected 3D Colliders To 2D")]
    private static void ConvertSelected()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        int convertedCount = 0;

        foreach (GameObject root in selected)
        {
            if (root == null) continue;

            Undo.RegisterFullObjectHierarchyUndo(root, "Convert 3D Colliders To 2D");
            convertedCount += ConvertHierarchy(root);
            EditorUtility.SetDirty(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Finished converting colliders. Total converted: {convertedCount}");
    }

    private static int ConvertHierarchy(GameObject root)
    {
        int count = 0;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in all)
        {
            if (t == null) continue;

            GameObject go = t.gameObject;

            // If already has any 2D collider, skip adding another one.
            Collider2D existing2D = go.GetComponent<Collider2D>();
            if (existing2D != null)
            {
                // Still remove leftover 3D colliders if any
                Collider[] legacy3D = go.GetComponents<Collider>();
                foreach (Collider c in legacy3D)
                {
                    if (c != null)
                    {
                        Undo.DestroyObjectImmediate(c);
                        count++;
                    }
                }
                continue;
            }

            // BoxCollider -> BoxCollider2D
            BoxCollider box3D = go.GetComponent<BoxCollider>();
            if (box3D != null)
            {
                BoxCollider2D box2D = Undo.AddComponent<BoxCollider2D>(go);
                box2D.isTrigger = box3D.isTrigger;
                box2D.offset = new Vector2(box3D.center.x, box3D.center.y);
                box2D.size = new Vector2(box3D.size.x, box3D.size.y);

                Undo.DestroyObjectImmediate(box3D);
                count++;
                continue;
            }

            // SphereCollider -> CircleCollider2D
            SphereCollider sphere3D = go.GetComponent<SphereCollider>();
            if (sphere3D != null)
            {
                CircleCollider2D circle2D = Undo.AddComponent<CircleCollider2D>(go);
                circle2D.isTrigger = sphere3D.isTrigger;
                circle2D.offset = new Vector2(sphere3D.center.x, sphere3D.center.y);
                circle2D.radius = sphere3D.radius;

                Undo.DestroyObjectImmediate(sphere3D);
                count++;
                continue;
            }

            // CapsuleCollider -> CapsuleCollider2D
            CapsuleCollider capsule3D = go.GetComponent<CapsuleCollider>();
            if (capsule3D != null)
            {
                CapsuleCollider2D capsule2D = Undo.AddComponent<CapsuleCollider2D>(go);
                capsule2D.isTrigger = capsule3D.isTrigger;
                capsule2D.offset = new Vector2(capsule3D.center.x, capsule3D.center.y);

                // Map X/Y size only
                capsule2D.size = new Vector2(capsule3D.radius * 2f, capsule3D.height);

                // Rough direction mapping
                if (capsule3D.direction == 0)
                {
                    capsule2D.direction = CapsuleDirection2D.Horizontal;
                    capsule2D.size = new Vector2(capsule3D.height, capsule3D.radius * 2f);
                }
                else
                {
                    capsule2D.direction = CapsuleDirection2D.Vertical;
                    capsule2D.size = new Vector2(capsule3D.radius * 2f, capsule3D.height);
                }

                Undo.DestroyObjectImmediate(capsule3D);
                count++;
                continue;
            }

            // Remove any other leftover 3D colliders if desired
            Collider[] other3D = go.GetComponents<Collider>();
            foreach (Collider c in other3D)
            {
                if (c != null)
                {
                    Debug.LogWarning($"Unconverted 3D collider on {go.name}: {c.GetType().Name}", go);
                }
            }
        }

        return count;
    }
}