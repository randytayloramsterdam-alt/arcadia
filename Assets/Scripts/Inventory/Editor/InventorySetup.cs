using UnityEditor;
using UnityEngine;

public class InventorySetup : EditorWindow
{
    [MenuItem("Tools/Setup Inventory System")]
    static void Setup()
    {
        // ── 1. Ensure "Inspect" layer exists ──
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        bool hasInspect = false;
        int inspectIndex = -1;
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == "Inspect")
            { hasInspect = true; inspectIndex = i; break; }
        }
        if (!hasInspect)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue = "Inspect";
                    tagManager.ApplyModifiedProperties();
                    inspectIndex = i;
                    Debug.Log("Added 'Inspect' layer at index " + i);
                    break;
                }
            }
        }

        int inspectLayer = LayerMask.NameToLayer("Inspect");

        // ── 2. Create Card prefab from FBX if needed ──
        string fbxPath = "Assets/Models/card/card.fbx";
        string prefabPath = "Assets/Inventory/Card.prefab";

        GameObject cardFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (cardFbx == null)
        {
            Debug.LogError("Card FBX not found at " + fbxPath);
            return;
        }

        GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (cardPrefab == null)
        {
            GameObject instance = Instantiate(cardFbx);
            instance.name = "Card";
            SetLayerRecursive(instance, inspectLayer);
            cardPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            DestroyImmediate(instance);
            Debug.Log("Created Card prefab at " + prefabPath);
        }

        // ── 3. Assign prefab to InventoryItem (Card.asset) if empty ──
        InventoryItem cardItem = AssetDatabase.LoadAssetAtPath<InventoryItem>(
            "Assets/Inventory/Items/Card.asset");
        if (cardItem != null && cardItem.inspectPrefab == null)
        {
            cardItem.inspectPrefab = cardPrefab;
            EditorUtility.SetDirty(cardItem);
            AssetDatabase.SaveAssets();
            Debug.Log("Assigned Card prefab to InventoryItem.");
        }

        // ── 4. Place Card in scene if not already there ──
        GameObject sceneCard = GameObject.Find("Card");
        if (sceneCard == null)
        {
            sceneCard = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
            sceneCard.name = "Card";
            sceneCard.transform.position = new Vector3(-2.5f, 1f, 4f);

            // Add CollectibleItem (which inherits from InteractableObject)
            CollectibleItem ci = sceneCard.GetComponent<CollectibleItem>();
            if (ci == null) ci = sceneCard.AddComponent<CollectibleItem>();
            ci.itemData = cardItem;
            ci.description = "A mysterious card with strange markings.";

            // Ensure collider exists for raycast
            BoxCollider col = sceneCard.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = sceneCard.AddComponent<BoxCollider>();
                col.size = new Vector3(0.5f, 0.7f, 0.05f);
            }

            Debug.Log("Placed Card in scene at " + sceneCard.transform.position);
        }
        else
        {
            // Update existing card's references
            CollectibleItem ci = sceneCard.GetComponent<CollectibleItem>();
            if (ci != null && ci.itemData == null)
            {
                ci.itemData = cardItem;
                EditorUtility.SetDirty(ci);
            }
        }

        // ── 5. Create InventorySystem GameObject if missing ──
        GameObject invSys = GameObject.Find("InventorySystem");
        if (invSys == null)
        {
            invSys = new GameObject("InventorySystem");
            invSys.AddComponent<InventoryManager>();
            invSys.AddComponent<InventoryUI>();
            Debug.Log("Created InventorySystem GameObject.");
        }

        // ── 6. Add PS2 renderer to main camera ──
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<PS2Renderer>() == null)
        {
            mainCam.gameObject.AddComponent<PS2Renderer>();
            Debug.Log("Added PS2Renderer to Main Camera.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("=== Inventory system setup complete! ===");
    }

    static void SetLayerRecursive(GameObject obj, int layer)
    {
        if (layer < 0) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
