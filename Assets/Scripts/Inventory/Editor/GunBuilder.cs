using UnityEditor;
using UnityEngine;

public class GunBuilder
{
    [MenuItem("Tools/Build Low-Poly Pistol")]
    public static void Build()
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.15f, 0.15f, 0.17f);

        var root = new GameObject("Pistol");

        // Slide — top block
        AddBox(root, "Slide", new Vector3(0.18f, 0.035f, 0.025f), new Vector3(0f, 0.065f, 0.01f), Quaternion.identity, mat);
        // Barrel — forward protrusion
        AddBox(root, "Barrel", new Vector3(0.055f, 0.018f, 0.018f), new Vector3(0f, 0.055f, 0.10f), Quaternion.identity, mat);
        // Frame — main body below slide
        AddBox(root, "Frame", new Vector3(0.10f, 0.028f, 0.025f), new Vector3(0f, 0.042f, 0.005f), Quaternion.identity, mat);
        // Trigger guard — small loop
        AddBox(root, "TriggerGuard", new Vector3(0.020f, 0.018f, 0.010f), new Vector3(0f, 0.032f, 0.025f), Quaternion.identity, mat);
        // Grip — angled down
        AddBox(root, "Grip", new Vector3(0.032f, 0.065f, 0.028f), new Vector3(0f, 0.008f, -0.015f), Quaternion.Euler(12f, 0f, 0f), mat);
        // Magazine base
        AddBox(root, "Mag", new Vector3(0.022f, 0.015f, 0.020f), new Vector3(0f, -0.025f, -0.010f), Quaternion.identity, mat);
        // Sights — front
        AddBox(root, "FrontSight", new Vector3(0.006f, 0.008f, 0.004f), new Vector3(0f, 0.088f, 0.100f), Quaternion.identity, mat);
        // Sights — rear
        AddBox(root, "RearSight", new Vector3(0.015f, 0.006f, 0.005f), new Vector3(0f, 0.085f, 0.005f), Quaternion.identity, mat);

        // Combine into single mesh
        var filters = root.GetComponentsInChildren<MeshFilter>();
        var combine = new CombineInstance[filters.Length];
        for (int i = 0; i < filters.Length; i++)
        {
            combine[i].mesh = filters[i].sharedMesh;
            combine[i].transform = filters[i].transform.localToWorldMatrix;
        }

        var combined = new Mesh();
        combined.CombineMeshes(combine, true);
        combined.RecalculateNormals();
        combined.RecalculateBounds();

        // Destroy children
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        root.AddComponent<MeshFilter>().sharedMesh = combined;
        root.AddComponent<MeshRenderer>().sharedMaterial = mat;

        // Save mesh asset
        AssetDatabase.CreateAsset(combined, "Assets/Models/Gun/PistolMesh.asset");
        AssetDatabase.SaveAssets();

        // Save prefab
        if (!AssetDatabase.IsValidFolder("Assets/Models/Gun"))
            AssetDatabase.CreateFolder("Assets/Models", "Gun");
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Models/Gun/Pistol.prefab");
        Object.DestroyImmediate(root);

        Debug.Log("[GunBuilder] Pistol prefab created at Assets/Models/Gun/Pistol.prefab");
    }

    static void AddBox(GameObject parent, string name, Vector3 size, Vector3 pos, Quaternion rot, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localScale = size;
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }
}
