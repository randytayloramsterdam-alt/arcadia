using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupPistolInScene
{
    [MenuItem("Tools/Place Pistol Collectible In Scene")]
    public static void PlacePistol()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[SetupPistol] No active scene. Open a scene first.");
            return;
        }

        // Find pistol asset
        var guids = AssetDatabase.FindAssets("t:InventoryItem Pistol");
        if (guids.Length == 0)
        {
            Debug.LogError("[SetupPistol] Pistol.asset not found. Make sure Assets/Inventory/Items/Pistol.asset exists.");
            return;
        }
        var pistolPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var pistolAsset = AssetDatabase.LoadAssetAtPath<InventoryItem>(pistolPath);
        if (pistolAsset == null) return;

        // Remove existing pistol collectibles
        var existing = Object.FindObjectsOfType<CollectibleItem>();
        foreach (var c in existing)
        {
            if (c.itemData != null && c.itemData.isWeapon)
            {
                Object.DestroyImmediate(c.gameObject);
                Debug.Log("[SetupPistol] Removed existing pistol collectible.");
            }
        }

        // Try to place in front of player start, otherwise near origin
        Vector3 spawnPos = new Vector3(0f, 1.2f, 1.5f);

        // Look for the waking room or player spawn
        var player = Object.FindObjectOfType<FirstPersonController>();
        if (player != null)
            spawnPos = player.transform.position + player.transform.forward * 2f + Vector3.up * 0.6f;

        var go = new GameObject("Pistol_Collectible");
        go.transform.position = spawnPos;

        // Build low-poly pistol model for world view
        var dark = new Material(Shader.Find("Standard"));
        dark.color = new Color(0.08f, 0.08f, 0.10f);

        void AddBox(string n, Vector3 s, Vector3 p, Quaternion r)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = n;
            b.transform.SetParent(go.transform);
            b.transform.localScale = s;
            b.transform.localPosition = p;
            b.transform.localRotation = r;
            b.GetComponent<MeshRenderer>().sharedMaterial = dark;
            Object.DestroyImmediate(b.GetComponent<Collider>());
        }

        AddBox("Slide",      new Vector3(0.36f, 0.07f, 0.05f),  new Vector3(0f, 0.13f, 0.02f),  Quaternion.identity);
        AddBox("Barrel",     new Vector3(0.11f, 0.036f, 0.036f), new Vector3(0f, 0.11f, 0.20f),  Quaternion.identity);
        AddBox("Frame",      new Vector3(0.20f, 0.056f, 0.05f),  new Vector3(0f, 0.084f, 0.01f), Quaternion.identity);
        AddBox("TrigGuard",  new Vector3(0.04f, 0.036f, 0.02f),  new Vector3(0f, 0.064f, 0.05f),  Quaternion.identity);
        AddBox("Grip",       new Vector3(0.064f, 0.13f, 0.056f), new Vector3(0f, 0.016f, -0.03f), Quaternion.Euler(12f, 0f, 0f));
        AddBox("Mag",        new Vector3(0.04f, 0.03f, 0.04f),   new Vector3(0f, -0.04f, -0.02f), Quaternion.identity);
        AddBox("FrontSight", new Vector3(0.012f, 0.016f, 0.008f),new Vector3(0f, 0.175f, 0.20f),  Quaternion.identity);
        AddBox("RearSight",  new Vector3(0.03f, 0.012f, 0.01f),  new Vector3(0f, 0.168f, 0.02f),  Quaternion.identity);

        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.085f, 0.03f);
        col.size = new Vector3(0.16f, 0.18f, 0.28f);

        var ci = go.AddComponent<CollectibleItem>();
        ci.itemData = pistolAsset;
        ci.description = "";

        // Try load collect sound
        var sguids = AssetDatabase.FindAssets("t:AudioClip ui");
        if (sguids.Length > 0)
        {
            var spath = AssetDatabase.GUIDToAssetPath(sguids[0]);
            ci.collectSound = AssetDatabase.LoadAssetAtPath<AudioClip>(spath);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[SetupPistol] Pistol collectible placed at {spawnPos}. Save the scene to keep it.");
    }
}
