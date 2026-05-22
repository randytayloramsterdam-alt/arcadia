using UnityEngine;

[ExecuteAlways]
public class OfficeVolumetricFog : MonoBehaviour
{
    public Color fogColor = new Color(0.46f, 0.42f, 0.25f, 1f);
    public int layerCount = 16;
    public float startDistance = 34f;
    public float layerSpacing = 16f;
    public float layerWidth = 70f;
    public float layerHeight = 12f;
    public float frustumCoverage = 1.18f;
    [Range(0f, 0.18f)] public float maxLayerAlpha = 0.085f;

    GameObject root;
    Material[] materials;

    void OnEnable()
    {
        Rebuild();
    }

    void OnValidate()
    {
        Rebuild();
    }

    void OnDisable()
    {
        Clear();
    }

    [ContextMenu("Rebuild Fog Layers")]
    public void Rebuild()
    {
        Clear();

        if (layerCount <= 0)
        {
            return;
        }

        root = new GameObject("Camera Volumetric Fog Layers");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        materials = new Material[layerCount];
        Mesh mesh = CreateDoubleSidedQuad();
        Camera camera = GetComponent<Camera>();

        for (int i = 0; i < layerCount; i++)
        {
            float t = (float)(i + 1) / layerCount;
            float distance = startDistance + i * layerSpacing;
            float alpha = Mathf.Lerp(0.012f, maxLayerAlpha, t * t);
            Color color = new Color(fogColor.r, fogColor.g, fogColor.b, alpha);
            Vector2 layerSize = GetLayerSize(camera, distance);

            var layer = new GameObject("fog volume slice " + i.ToString("00"));
            layer.transform.SetParent(root.transform, false);
            layer.transform.localPosition = new Vector3(0f, 0f, distance);
            layer.transform.localRotation = Quaternion.identity;
            layer.transform.localScale = new Vector3(layerSize.x, layerSize.y, 1f);

            MeshFilter filter = layer.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
            materials[i] = MakeFogMaterial(color);
            renderer.sharedMaterial = materials[i];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    void Clear()
    {
        if (root != null)
        {
            if (Application.isPlaying)
            {
                Destroy(root);
            }
            else
            {
                DestroyImmediate(root);
            }

            root = null;
        }

        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(materials[i]);
            }
            else
            {
                DestroyImmediate(materials[i]);
            }
        }

        materials = null;
    }

    static Mesh CreateDoubleSidedQuad()
    {
        var mesh = new Mesh();
        mesh.name = "Double Sided Fog Quad";
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1, 1, 2, 0, 1, 3, 2 };
        mesh.RecalculateNormals();
        return mesh;
    }

    Vector2 GetLayerSize(Camera camera, float distance)
    {
        if (camera == null)
        {
            return new Vector2(layerWidth, layerHeight);
        }

        float height = 2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
        float width = height * Mathf.Max(0.1f, camera.aspect);
        return new Vector2(
            Mathf.Max(layerWidth, width * frustumCoverage),
            Mathf.Max(layerHeight, height * frustumCoverage)
        );
    }

    static Material MakeFogMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Unlit/Transparent"));
        material.name = "camera volumetric fog";
        material.color = color;
        material.renderQueue = 3050;
        return material;
    }
}
