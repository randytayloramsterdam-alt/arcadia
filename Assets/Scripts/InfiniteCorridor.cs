using System.Collections;
using UnityEngine;

public class InfiniteCorridor : MonoBehaviour
{
    [Header("Dimensions")]
    [Tooltip("Length of one repeating segment.")]
    public float segmentLength = 10f;
    public float corridorWidth = 3f;
    public float corridorHeight = 3.5f;
    [Tooltip("How many segments to keep alive (odd number works best).")]
    public int segmentCount = 5;

    [Header("Fog / Visibility")]
    [Tooltip("Distance where fog becomes fully opaque. Must be less than (segmentLength * 2).")]
    public float fogFullDistance = 12f;
    public Color fogColor = new Color(0.015f, 0.015f, 0.02f);
    [Tooltip("How many semi-transparent fog planes to stack at each end.")]
    public int fogPlaneCount = 6;

    [Header("Materials")]
    public Material wallMaterial;
    public Material floorMaterial;
    public Material ceilingMaterial;

    // ── private state ──
    private GameObject[] segments;
    private Transform player;
    private float virtualDistance;
    private Vector3 origin;
    private Vector3 forward;
    private bool isInside;
    private Transform[] fogPlanesFront;
    private Transform[] fogPlanesBack;

    void Awake()
    {
        player = Camera.main.transform;

        if (wallMaterial == null) wallMaterial = Mat(new Color(0.28f, 0.24f, 0.20f));
        if (floorMaterial == null) floorMaterial = Mat(new Color(0.12f, 0.10f, 0.08f));
        if (ceilingMaterial == null) ceilingMaterial = Mat(new Color(0.16f, 0.15f, 0.14f));

        origin = transform.position;
        forward = transform.forward;
    }

    void Start()
    {
        BuildSegments();
        BuildFogPlanes();
    }

    // ═══════════════════════════════════════════
    //  BUILDING
    // ═══════════════════════════════════════════

    void BuildSegments()
    {
        segments = new GameObject[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            segments[i] = BuildSegment();
            PlaceSegment(i, i);
        }
    }

    void PlaceSegment(int index, float segmentIndex)
    {
        segments[index].transform.position = origin + forward * (segmentIndex * segmentLength);
        segments[index].transform.rotation = transform.rotation;
        segments[index].transform.SetParent(transform);
    }

    GameObject BuildSegment()
    {
        var root = new GameObject("Seg");
        float sl = segmentLength;
        float cw = corridorWidth;
        float ch = corridorHeight;

        // Floor
        Box(root, "Floor", new Vector3(cw, 0.08f, sl), new Vector3(0, 0, sl * 0.5f), floorMaterial);
        // Ceiling
        Box(root, "Ceil", new Vector3(cw, 0.08f, sl), new Vector3(0, ch, sl * 0.5f), ceilingMaterial);
        // Walls
        Box(root, "WallL", new Vector3(0.12f, ch, sl), new Vector3(-cw * 0.5f, ch * 0.5f, sl * 0.5f), wallMaterial);
        Box(root, "WallR", new Vector3(0.12f, ch, sl), new Vector3( cw * 0.5f, ch * 0.5f, sl * 0.5f), wallMaterial);

        // Ceiling light every segment
        var lgo = new GameObject("Light");
        lgo.transform.SetParent(root.transform);
        lgo.transform.localPosition = new Vector3(0, ch - 0.25f, sl * 0.5f);
        var lt = lgo.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = new Color(1f, 0.88f, 0.72f);
        lt.intensity = 1f;
        lt.range = 12f;
        lt.shadows = LightShadows.None;

        return root;
    }

    void BuildFogPlanes()
    {
        var fogMat = new Material(Shader.Find("Standard"));
        fogMat.color = fogColor;
        fogMat.SetFloat("_Mode", 2);
        fogMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fogMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fogMat.SetInt("_ZWrite", 0);
        fogMat.renderQueue = 3000;

        fogPlanesFront = new Transform[fogPlaneCount];
        fogPlanesBack  = new Transform[fogPlaneCount];

        float spacing = fogFullDistance / fogPlaneCount;

        for (int i = 0; i < fogPlaneCount; i++)
        {
            float t = (float)(i + 1) / (fogPlaneCount + 1);
            Color c = Color.Lerp(new Color(fogColor.r, fogColor.g, fogColor.b, 0.1f),
                                 new Color(fogColor.r, fogColor.g, fogColor.b, 1f), t);

            fogPlanesFront[i] = MakeFogPlane("FogF_" + i, 0f, c, fogMat);
            fogPlanesBack[i]  = MakeFogPlane("FogB_" + i, 0f, c, fogMat);
        }
    }

    Transform MakeFogPlane(string name, float dist, Color color, Material template)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.rotation = Quaternion.LookRotation(forward);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = CreateQuad();
        var mr = go.AddComponent<MeshRenderer>();
        var mat = new Material(template);
        mat.color = color;
        mr.sharedMaterial = mat;
        go.transform.localScale = new Vector3(corridorWidth + 0.3f, corridorHeight, 1f);
        return go.transform;
    }

    Mesh CreateQuad()
    {
        var m = new Mesh();
        m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
                             new Vector3(-0.5f,  0.5f, 0), new Vector3(0.5f,  0.5f, 0) };
        m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        m.RecalculateNormals();
        return m;
    }

    // ═══════════════════════════════════════════
    //  RUNTIME — TREADMILL
    // ═══════════════════════════════════════════

    void Update()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - origin;
        float along = Vector3.Dot(toPlayer, forward);
        float lateral = Vector3.Dot(toPlayer, transform.right);
        float vertical = toPlayer.y - origin.y;

        bool inBounds =
            Mathf.Abs(lateral) < corridorWidth * 0.75f &&
            Mathf.Abs(vertical) < corridorHeight * 0.85f &&
            along > -1f;

        if (!inBounds)
        {
            isInside = false;
            return;
        }

        if (!isInside)
        {
            isInside = true;
            virtualDistance = Mathf.Max(0f, along);
        }

        // Update virtual distance from physical movement
        float prevVD = virtualDistance;
        virtualDistance += Vector3.Dot(player.position - (origin + forward * virtualDistance), forward);
        virtualDistance = Mathf.Max(0f, virtualDistance); // can't go behind entrance
        float delta = virtualDistance - prevVD;

        // ── Treadmill: keep player physically near the middle ──
        int midIdx = segmentCount / 2;
        float physicalTarget = midIdx * segmentLength + segmentLength * 0.5f;
        float offset = virtualDistance - physicalTarget;

        if (Mathf.Abs(offset) > segmentLength * 0.5f)
        {
            // Shift player and segments
            float shift = Mathf.Round(offset / segmentLength) * segmentLength;
            virtualDistance -= shift;
            ShiftPlayer(-shift);
            RepositionAllSegments();
        }

        // ── Move fog planes with the virtual camera ──
        UpdateFogPlanes();
    }

    void ShiftPlayer(float amount)
    {
        var cc = player.GetComponentInParent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            cc.transform.position += forward * amount;
            cc.enabled = true;
        }
        else
        {
            player.position += forward * amount;
        }
    }

    void RepositionAllSegments()
    {
        // Which "world segment index" does virtualDistance fall into?
        int baseIdx = Mathf.FloorToInt(virtualDistance / segmentLength) - segmentCount / 2;
        for (int i = 0; i < segmentCount; i++)
        {
            PlaceSegment(i, baseIdx + i);
        }
    }

    void UpdateFogPlanes()
    {
        // Front fog: obstacle ahead the player can never reach
        float frontBase = virtualDistance + fogFullDistance * 0.5f;
        for (int i = 0; i < fogPlaneCount; i++)
        {
            float d = frontBase + (float)i / fogPlaneCount * fogFullDistance;
            fogPlanesFront[i].position = origin + forward * d;
        }

        // Back fog: behind the player, obscures the entrance from far away
        float backBase = virtualDistance - fogFullDistance;
        for (int i = 0; i < fogPlaneCount; i++)
        {
            float d = backBase + (float)i / fogPlaneCount * fogFullDistance;
            fogPlanesBack[i].position = origin + forward * d;
        }
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    void Box(GameObject parent, string name, Vector3 size, Vector3 localPos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localScale = size;
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }

    public Vector3 GetEntrancePosition() => origin;
    public bool IsPlayerInside() => isInside;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 fwd = transform.forward;
        float totalLen = segmentLength * segmentCount;
        Vector3 start = transform.position;
        Gizmos.DrawLine(start, start + fwd * totalLen);
        Gizmos.DrawWireCube(start + fwd * totalLen * 0.5f + Vector3.up * corridorHeight * 0.5f,
                           new Vector3(corridorWidth, corridorHeight, totalLen));
    }
}
