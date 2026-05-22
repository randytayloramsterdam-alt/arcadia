using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class EndlessOfficeGenerator : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Endless Direction")]
    public float chunkLength = 18f;
    public int chunksAhead = 18;
    public int chunksBehind = 3;
    public int seed = 1987;

    [Header("Office Layout")]
    public float officeWidth = 34f;
    public float ceilingHeight = 3.15f;
    public int deskColumns = 6;
    public int deskRowsPerChunk = 3;

    [Header("Horror Mood")]
    public bool applyFogAndAmbient = true;
    public Color fogColor = new Color(0.46f, 0.42f, 0.25f, 1f);
    [Range(0.005f, 0.08f)] public float fogDensity = 0.014f;
    [Range(0f, 1f)] public float glowingScreenChance = 0.25f;
    [Range(0f, 1f)] public float missingChairChance = 0.08f;

    [Header("Distance Haze")]
    public bool buildDistanceFogPlanes = false;
    public int fogPlaneCount = 13;
    public float fogStartDistance = 42f;
    public float fogSpacing = 18f;
    [Range(0f, 0.35f)] public float fogPlaneMaxAlpha = 0.18f;
    public float fogPlaneWidthPadding = 5f;

    readonly Dictionary<int, GameObject> chunks = new Dictionary<int, GameObject>();
    readonly List<int> stale = new List<int>();
    OfficePalette palette;

    void Awake()
    {
        palette = OfficePalette.Create();
        ClearGeneratedChunks();

        if (applyFogAndAmbient)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientLight = new Color(0.18f, 0.17f, 0.11f, 1f);
        }
    }

    void Start()
    {
        if (player == null && Camera.main != null)
        {
            player = Camera.main.transform.root;
        }

        RefreshChunks();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            RebuildPreview();
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        RefreshChunks();
    }

    [ContextMenu("Rebuild Preview")]
    public void RebuildPreview()
    {
        palette = OfficePalette.Create();
        ClearGeneratedChunks();
        chunks.Clear();

        int center = 0;
        if (player != null)
        {
            center = Mathf.FloorToInt(player.position.z / chunkLength);
        }

        for (int i = center - chunksBehind; i <= center + chunksAhead; i++)
        {
            chunks[i] = BuildChunk(i);
        }
    }

    void RefreshChunks()
    {
        if (player == null)
        {
            return;
        }

        int center = Mathf.FloorToInt(player.position.z / chunkLength);
        int min = center - chunksBehind;
        int max = center + chunksAhead;

        for (int i = min; i <= max; i++)
        {
            if (!chunks.ContainsKey(i))
            {
                chunks[i] = BuildChunk(i);
            }
        }

        stale.Clear();
        foreach (int key in chunks.Keys)
        {
            if (key < min || key > max)
            {
                stale.Add(key);
            }
        }

        for (int i = 0; i < stale.Count; i++)
        {
            int key = stale[i];
            Destroy(chunks[key]);
            chunks.Remove(key);
        }
    }

    GameObject BuildChunk(int chunkIndex)
    {
        var rng = new System.Random(seed + chunkIndex * 92837111);
        float startZ = chunkIndex * chunkLength;
        float centerZ = startZ + chunkLength * 0.5f;

        var root = new GameObject("Endless Office Chunk " + chunkIndex.ToString("0000"));
        root.transform.SetParent(transform, true);

        Box("polished stained floor", root.transform, new Vector3(0f, -0.06f, centerZ), new Vector3(officeWidth, 0.12f, chunkLength), palette.floor);
        Box("drop ceiling slab", root.transform, new Vector3(0f, ceilingHeight + 0.035f, centerZ), new Vector3(officeWidth, 0.07f, chunkLength), palette.ceiling);
        BuildWalls(root.transform, centerZ);
        BuildCeilingGrid(root.transform, startZ);
        BuildFluorescents(root.transform, startZ, rng, chunkIndex);
        BuildColumns(root.transform, startZ, rng);
        BuildWorkstations(root.transform, startZ, rng);
        BuildDistanceFog(parent: root.transform, startZ);

        return root;
    }

    void BuildDistanceFog(Transform parent, float startZ)
    {
        if (!buildDistanceFogPlanes)
        {
            return;
        }

        float chunkEnd = startZ + chunkLength;
        for (int i = 0; i < fogPlaneCount; i++)
        {
            float z = startZ + fogStartDistance + i * fogSpacing;
            if (z < startZ || z > chunkEnd)
            {
                continue;
            }

            float t = (float)(i + 1) / fogPlaneCount;
            Color color = new Color(fogColor.r, fogColor.g, fogColor.b, Mathf.Lerp(0.025f, fogPlaneMaxAlpha, t));
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = "soft volumetric distance haze";
            plane.transform.SetParent(parent, true);
            plane.transform.position = new Vector3(0f, ceilingHeight * 0.5f, z);
            plane.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            plane.transform.localScale = new Vector3(officeWidth + fogPlaneWidthPadding, ceilingHeight * 1.2f, 1f);
            DestroyCollider(plane);
            plane.GetComponent<MeshRenderer>().sharedMaterial = OfficePalette.MakeTransparent("distance haze", color);
        }
    }

    void BuildWalls(Transform parent, float centerZ)
    {
        float wallX = officeWidth * 0.5f + 0.08f;
        Box("left stained wallpaper wall", parent, new Vector3(-wallX, ceilingHeight * 0.5f, centerZ), new Vector3(0.16f, ceilingHeight, chunkLength), palette.wall);
        Box("right stained wallpaper wall", parent, new Vector3(wallX, ceilingHeight * 0.5f, centerZ), new Vector3(0.16f, ceilingHeight, chunkLength), palette.wall);

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = centerZ - chunkLength * 0.45f + i * chunkLength * 0.225f;
                Box("faint wallpaper seam", parent, new Vector3(side * (officeWidth * 0.5f + 0.166f), ceilingHeight * 0.5f, z), new Vector3(0.015f, ceilingHeight, 0.035f), palette.wallTrim);
            }
        }
    }

    void BuildCeilingGrid(Transform parent, float startZ)
    {
        float panelSize = 2f;
        float halfWidth = officeWidth * 0.5f;

        for (float x = -halfWidth; x <= halfWidth + 0.01f; x += panelSize)
        {
            Box("ceiling grid cross rail", parent, new Vector3(x, ceilingHeight - 0.015f, startZ + chunkLength * 0.5f), new Vector3(0.025f, 0.035f, chunkLength), palette.ceilingRail);
        }

        for (float z = startZ; z <= startZ + chunkLength + 0.01f; z += panelSize)
        {
            Box("ceiling grid long rail", parent, new Vector3(0f, ceilingHeight - 0.01f, z), new Vector3(officeWidth, 0.03f, 0.025f), palette.ceilingRail);
        }
    }

    void BuildFluorescents(Transform parent, float startZ, System.Random rng, int chunkIndex)
    {
        float[] xs = { -officeWidth * 0.31f, -officeWidth * 0.1f, officeWidth * 0.1f, officeWidth * 0.31f };
        float[] zs = { startZ + chunkLength * 0.22f, startZ + chunkLength * 0.55f, startZ + chunkLength * 0.88f };

        for (int x = 0; x < xs.Length; x++)
        {
            for (int z = 0; z < zs.Length; z++)
            {
                bool dead = rng.NextDouble() < 0.08;
                Vector3 pos = new Vector3(xs[x], ceilingHeight - 0.11f, zs[z]);
                Box("buzzing fluorescent fixture", parent, pos, new Vector3(3.2f, 0.07f, 0.24f), dead ? palette.deadLight : palette.fluorescent);

                if (dead)
                {
                    continue;
                }

                var lightObject = new GameObject("fluorescent spill");
                lightObject.transform.SetParent(parent, true);
                lightObject.transform.position = pos + Vector3.down * 0.2f;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.91f, 0.58f);
                light.range = 7f;
                light.intensity = 0.55f;
                light.shadows = LightShadows.None;

                var flicker = lightObject.AddComponent<OfficeLightFlicker>();
                flicker.seedOffset = chunkIndex * 19.31f + x * 2.7f + z * 0.49f;
                flicker.baseIntensity = 0.55f;
                flicker.minIntensity = 0.2f;
            }
        }
    }

    void BuildColumns(Transform parent, float startZ, System.Random rng)
    {
        float[] columnXs = { -officeWidth * 0.34f, 0f, officeWidth * 0.34f };
        for (int i = 0; i < columnXs.Length; i++)
        {
            if (rng.NextDouble() < 0.15f)
            {
                continue;
            }

            float z = startZ + chunkLength * (0.28f + 0.34f * i);
            Box("water damaged concrete column", parent, new Vector3(columnXs[i], ceilingHeight * 0.5f, z), new Vector3(0.88f, ceilingHeight, 0.88f), palette.column);
            Box("rust stain at column base", parent, new Vector3(columnXs[i], 0.16f, z - 0.46f), new Vector3(0.92f, 0.32f, 0.08f), palette.rust);
        }
    }

    void BuildWorkstations(Transform parent, float startZ, System.Random rng)
    {
        float usableWidth = officeWidth - 5.2f;
        float columnStep = usableWidth / Mathf.Max(1, deskColumns - 1);
        float rowStep = chunkLength / (deskRowsPerChunk + 1);

        for (int row = 0; row < deskRowsPerChunk; row++)
        {
            float z = startZ + rowStep * (row + 1);
            int facing = row % 2 == 0 ? 1 : -1;

            for (int col = 0; col < deskColumns; col++)
            {
                if (rng.NextDouble() < 0.04f)
                {
                    continue;
                }

                float x = -usableWidth * 0.5f + columnStep * col + Range(rng, -0.18f, 0.18f);
                float deskZ = z + Range(rng, -0.2f, 0.2f);
                BuildDeskStation(parent, new Vector3(x, 0f, deskZ), facing, rng);
            }
        }
    }

    void BuildDeskStation(Transform parent, Vector3 basePosition, int facing, System.Random rng)
    {
        var station = new GameObject("abandoned workstation");
        station.transform.SetParent(parent, true);
        station.transform.position = basePosition;
        station.transform.rotation = Quaternion.Euler(0f, (facing > 0 ? 0f : 180f) + Range(rng, -1.6f, 1.6f), 0f);

        Transform t = station.transform;
        Box("desk top", t, Local(t, 0f, 0.72f, 0f), new Vector3(2.35f, 0.16f, 1.18f), palette.desk);
        Box("left drawer stack", t, Local(t, -0.82f, 0.36f, -0.25f), new Vector3(0.42f, 0.66f, 0.52f), palette.drawer);
        Box("right drawer stack", t, Local(t, 0.82f, 0.36f, -0.25f), new Vector3(0.42f, 0.66f, 0.52f), palette.drawer);
        Box("dusty modesty panel", t, Local(t, 0f, 0.44f, -0.54f), new Vector3(1.8f, 0.58f, 0.08f), palette.deskDark);
        BuildComputer(t, rng);

        if (rng.NextDouble() > missingChairChance)
        {
            BuildChair(t, rng);
        }

        if (rng.NextDouble() < 0.28f)
        {
            Box("paper stack", t, Local(t, Range(rng, -0.7f, 0.7f), 0.84f, Range(rng, -0.25f, 0.25f)), new Vector3(0.38f, 0.035f, 0.28f), palette.paper);
        }
    }

    void BuildComputer(Transform station, System.Random rng)
    {
        float monitorX = Range(rng, -0.22f, 0.22f);
        Box("beige CRT monitor body", station, Local(station, monitorX, 1.08f, 0.17f), new Vector3(0.66f, 0.48f, 0.52f), palette.computer);
        Box("black dead screen", station, Local(station, monitorX, 1.1f, -0.1f), new Vector3(0.48f, 0.3f, 0.035f), rng.NextDouble() < glowingScreenChance ? palette.screenGlow : palette.screenBlack);
        Box("monitor neck", station, Local(station, monitorX, 0.83f, 0.18f), new Vector3(0.24f, 0.18f, 0.22f), palette.computerDark);
        Box("keyboard", station, Local(station, monitorX, 0.84f, -0.34f), new Vector3(0.72f, 0.045f, 0.22f), palette.keyboard);
        Box("mouse", station, Local(station, monitorX + 0.55f, 0.845f, -0.28f), new Vector3(0.17f, 0.045f, 0.22f), palette.keyboard);
        Box("computer tower", station, Local(station, 0.62f, 0.39f, 0.36f), new Vector3(0.28f, 0.55f, 0.5f), palette.computer);
    }

    void BuildChair(Transform station, System.Random rng)
    {
        var chair = new GameObject("grey office chair");
        chair.transform.SetParent(station, true);
        chair.transform.position = Local(station, Range(rng, -0.08f, 0.08f), 0f, -1.05f + Range(rng, -0.08f, 0.08f));
        chair.transform.rotation = station.rotation * Quaternion.Euler(0f, Range(rng, -7f, 7f), 0f);

        Transform t = chair.transform;
        Box("chair seat", t, Local(t, 0f, 0.48f, 0f), new Vector3(0.62f, 0.12f, 0.58f), palette.chair);
        Box("chair back", t, Local(t, 0f, 0.88f, -0.25f), new Vector3(0.66f, 0.72f, 0.12f), palette.chair);
        Box("chair post", t, Local(t, 0f, 0.25f, 0f), new Vector3(0.12f, 0.5f, 0.12f), palette.chairDark);
        Box("chair base", t, Local(t, 0f, 0.08f, 0f), new Vector3(0.9f, 0.08f, 0.18f), palette.chairDark);
        Box("chair base cross", t, Local(t, 0f, 0.081f, 0f), new Vector3(0.18f, 0.08f, 0.9f), palette.chairDark);
    }

    GameObject Box(string name, Transform parent, Vector3 worldPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, true);
        obj.transform.position = worldPosition;
        obj.transform.rotation = parent.rotation;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }

    static void DestroyCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(collider);
        }
        else
        {
            DestroyImmediate(collider);
        }
    }

    void ClearGeneratedChunks()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith("Endless Office Chunk", StringComparison.Ordinal))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    static Vector3 Local(Transform transform, float x, float y, float z)
    {
        return transform.TransformPoint(new Vector3(x, y, z));
    }

    static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}

class OfficePalette
{
    public Material floor;
    public Material ceiling;
    public Material ceilingRail;
    public Material wall;
    public Material wallTrim;
    public Material fluorescent;
    public Material deadLight;
    public Material desk;
    public Material deskDark;
    public Material drawer;
    public Material computer;
    public Material computerDark;
    public Material screenBlack;
    public Material screenGlow;
    public Material keyboard;
    public Material chair;
    public Material chairDark;
    public Material column;
    public Material rust;
    public Material paper;

    public static OfficePalette Create()
    {
        return new OfficePalette
        {
            floor = Make("wet yellow concrete floor", new Color(0.36f, 0.32f, 0.18f), 0f, 0.25f),
            ceiling = Make("stained acoustic ceiling tile", new Color(0.45f, 0.42f, 0.25f)),
            ceilingRail = Make("dark ceiling rail", new Color(0.13f, 0.12f, 0.08f)),
            wall = Make("aged yellow wallpaper", new Color(0.43f, 0.39f, 0.2f)),
            wallTrim = Make("faint wall seam trim", new Color(0.25f, 0.23f, 0.12f)),
            fluorescent = Make("sick fluorescent tube", new Color(1f, 0.93f, 0.62f), 1.45f),
            deadLight = Make("dead fluorescent tube", new Color(0.32f, 0.31f, 0.24f)),
            desk = Make("old laminate desk", new Color(0.48f, 0.34f, 0.18f)),
            deskDark = Make("dark wood panel", new Color(0.27f, 0.18f, 0.1f)),
            drawer = Make("scratched drawer front", new Color(0.39f, 0.27f, 0.15f)),
            computer = Make("aged beige plastic", new Color(0.68f, 0.62f, 0.42f)),
            computerDark = Make("shadowed beige plastic", new Color(0.42f, 0.38f, 0.27f)),
            screenBlack = Make("dead black monitor glass", new Color(0.015f, 0.018f, 0.014f), 0f, 0.05f),
            screenGlow = Make("unsettling green monitor glow", new Color(0.27f, 0.85f, 0.38f), 0.75f),
            keyboard = Make("yellowed keyboard", new Color(0.52f, 0.49f, 0.36f)),
            chair = Make("dusty grey chair fabric", new Color(0.38f, 0.39f, 0.34f)),
            chairDark = Make("dark chair metal", new Color(0.12f, 0.13f, 0.11f)),
            column = Make("dirty concrete column", new Color(0.42f, 0.39f, 0.31f)),
            rust = Make("rust and old leak stain", new Color(0.34f, 0.12f, 0.04f)),
            paper = Make("abandoned paperwork", new Color(0.72f, 0.68f, 0.51f))
        };
    }

    static Material Make(string name, Color color, float emission = 0f, float smoothness = 0f)
    {
        var material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        material.SetFloat("_Glossiness", smoothness);

        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }

        return material;
    }

    public static Material MakeTransparent(string name, Color color)
    {
        var material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        material.SetFloat("_Mode", 2f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }
}
