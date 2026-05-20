using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SSAOEffect : MonoBehaviour
{
    [Header("SSAO")]
    [Range(0.05f, 2f)] public float sampleRadius = 0.4f;
    [Range(0.5f, 4f)] public float intensity = 1.2f;
    [Range(0f, 0.2f)] public float bias = 0.03f;

    [Header("Debug")]
    public bool showAOOnly;

    public Material Material { get; private set; }
    private Camera cam;
    private Vector4[] kernel = new Vector4[8];

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
        Material = new Material(Shader.Find("Hidden/SSAO"));

        // Pre-generate sample kernel (fix seed for stable AO)
        Random.InitState(42);
        for (int i = 0; i < 8; i++)
        {
            float theta = Random.Range(0f, Mathf.PI * 2f);
            float phi = Mathf.Acos(Random.Range(0f, 1f));
            Vector3 dir = new Vector3(
                Mathf.Sin(phi) * Mathf.Cos(theta),
                Mathf.Sin(phi) * Mathf.Sin(theta),
                Mathf.Cos(phi)
            );
            float scale = (float)i / 7f;
            scale = Mathf.Lerp(0.05f, 1f, scale * scale);
            kernel[i] = new Vector4(dir.x * scale, dir.y * scale, dir.z * scale, 0f);
        }
    }

    public void ApplyMaterialParameters()
    {
        if (Material == null) return;

        // Frustum corners for view-space position reconstruction
        float far = cam.farClipPlane;
        float halfH = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * far;
        float halfW = halfH * cam.aspect;
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(-halfW, -halfH, far);
        corners[1] = new Vector3( halfW, -halfH, far);
        corners[2] = new Vector3( halfW,  halfH, far);
        corners[3] = new Vector3(-halfW,  halfH, far);

        Matrix4x4 frustum = Matrix4x4.identity;
        frustum.SetRow(0, corners[0]);
        frustum.SetRow(1, corners[1]);
        frustum.SetRow(2, corners[2]);
        frustum.SetRow(3, corners[3]);
        Material.SetMatrix("_FrustumCorners", frustum);

        Material.SetFloat("_SampleRadius", sampleRadius);
        Material.SetFloat("_Intensity", intensity);
        Material.SetFloat("_Bias", bias);
        Material.SetVectorArray("_SampleKernel", kernel);
        Material.SetInt("_SampleCount", 8);
    }

    void OnDestroy()
    {
        if (Material != null)
            Destroy(Material);
    }
}
