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
    public float centerAisleWidth = 3.2f;

    [Header("Performance")]
    public int fullDetailChunkRadius = 7;
    public int litChunkInterval = 2;
    public int realtimeLightsPerLitChunk = 2;
    public bool addDeskColliders = true;

    [Header("Horror Mood")]
    public bool applyFogAndAmbient = true;
    public Color fogColor = new Color(0.46f, 0.42f, 0.25f, 1f);
    [Range(0.005f, 0.08f)] public float fogDensity = 0.014f;
    [Range(0f, 1f)] public float glowingScreenChance = 0.25f;
    [Range(0f, 1f)] public float missingChairChance = 0.08f;

    readonly Dictionary<int, GameObject> chunks = new Dictionary<int, GameObject>();
    readonly List<int> stale = new List<int>();
    OfficePalette palette;

    void Awake()
    {
        palette = OfficePalette.Create();
        ClearGeneratedChunks();
        ApplyAtmosphere();
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
        ApplyAtmosphere();

        int center = player != null ? Mathf.FloorToInt(player.position.z / chunkLength) : 0;
        for (int i = center - chunksBehind; i <= center + chunksAhead; i++)
        {
            chunks[i] = BuildChunk(i, Mathf.Abs(i - center));
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
                chunks[i] = BuildChunk(i, Mathf.Abs(i - center));
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

    void ApplyAtmosphere()
    {
        if (!applyFogAndAmbient)
        {
            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.17f, 0.11f, 1f);
        RenderSettings.ambientIntensity = 0.42f;
        RenderSettings.reflectionIntensity = 0.12f;
    }

    GameObject BuildChunk(int chunkIndex, int distanceFromPlayerChunk)
    {
        var rng = new System.Random(seed + chunkIndex * 92837111);
        float startZ = chunkIndex * chunkLength;
        float centerZ = startZ + chunkLength * 0.5f;
        bool fullDetail = distanceFromPlayerChunk <= fullDetailChunkRadius;

        var root = new GameObject("Endless Office Chunk " + chunkIndex.ToString("0000"));
        root.transform.SetParent(transform, true);

        ChunkMesh chunk = new ChunkMesh(root.transform);
        BuildRoomShell(chunk, root, centerZ, startZ);
        BuildCeilingGrid(chunk, startZ);
        BuildFluorescents(chunk, root.transform, startZ, rng, chunkIndex);
        BuildColumns(chunk, root, startZ, rng);
        BuildWorkstations(chunk, root, startZ, rng, fullDetail);
        BuildArcadiaStoryDetails(chunk, root.transform, startZ, rng, chunkIndex, fullDetail);
        chunk.Flush();

        return root;
    }

    void BuildRoomShell(ChunkMesh chunk, GameObject root, float centerZ, float startZ)
    {
        float wallX = officeWidth * 0.5f + 0.08f;
        chunk.Box(palette.carpet, "carpet", new Vector3(0f, -0.035f, centerZ), new Vector3(officeWidth, 0.07f, chunkLength));
        chunk.Box(palette.ceiling, "ceiling", new Vector3(0f, ceilingHeight + 0.035f, centerZ), new Vector3(officeWidth, 0.07f, chunkLength));
        chunk.Box(palette.wall, "wall", new Vector3(-wallX, ceilingHeight * 0.5f, centerZ), new Vector3(0.16f, ceilingHeight, chunkLength));
        chunk.Box(palette.wall, "wall", new Vector3(wallX, ceilingHeight * 0.5f, centerZ), new Vector3(0.16f, ceilingHeight, chunkLength));

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = startZ + i * chunkLength * 0.25f;
                chunk.Box(palette.wallTrim, "wall trim", new Vector3(side * (officeWidth * 0.5f + 0.166f), ceilingHeight * 0.5f, z), new Vector3(0.015f, ceilingHeight, 0.035f));
            }
        }

        AddBoxCollider(root, new Vector3(0f, -0.08f, centerZ), new Vector3(officeWidth, 0.12f, chunkLength));
        AddBoxCollider(root, new Vector3(-wallX, ceilingHeight * 0.5f, centerZ), new Vector3(0.16f, ceilingHeight, chunkLength));
        AddBoxCollider(root, new Vector3(wallX, ceilingHeight * 0.5f, centerZ), new Vector3(0.16f, ceilingHeight, chunkLength));
    }

    void BuildCeilingGrid(ChunkMesh chunk, float startZ)
    {
        float panelSize = 2f;
        float halfWidth = officeWidth * 0.5f;

        for (float x = -halfWidth; x <= halfWidth + 0.01f; x += panelSize)
        {
            chunk.Box(palette.ceilingRail, "ceiling rail", new Vector3(x, ceilingHeight - 0.015f, startZ + chunkLength * 0.5f), new Vector3(0.025f, 0.035f, chunkLength));
        }

        for (float z = startZ; z <= startZ + chunkLength + 0.01f; z += panelSize)
        {
            chunk.Box(palette.ceilingRail, "ceiling rail", new Vector3(0f, ceilingHeight - 0.01f, z), new Vector3(officeWidth, 0.03f, 0.025f));
        }
    }

    void BuildFluorescents(ChunkMesh chunk, Transform parent, float startZ, System.Random rng, int chunkIndex)
    {
        float[] xs = { -officeWidth * 0.33f, -officeWidth * 0.17f, 0f, officeWidth * 0.17f, officeWidth * 0.33f };
        float[] zs = { startZ + chunkLength * 0.18f, startZ + chunkLength * 0.5f, startZ + chunkLength * 0.82f };
        int liveLights = 0;
        bool canUseRealtimeLights = litChunkInterval <= 1 || Mathf.Abs(chunkIndex) % litChunkInterval == 0;

        for (int x = 0; x < xs.Length; x++)
        {
            for (int z = 0; z < zs.Length; z++)
            {
                bool dead = rng.NextDouble() < 0.09;
                Vector3 pos = new Vector3(xs[x], ceilingHeight - 0.105f, zs[z]);
                chunk.Box(dead ? palette.deadLight : palette.fluorescent, "fluorescent", pos, new Vector3(2.7f, 0.055f, 0.42f));
                chunk.Box(palette.ceilingRail, "fixture frame", pos + Vector3.up * 0.025f, new Vector3(2.95f, 0.035f, 0.5f));

                if (dead || !canUseRealtimeLights || liveLights >= realtimeLightsPerLitChunk)
                {
                    continue;
                }

                if ((x + z + chunkIndex) % 5 != 0)
                {
                    continue;
                }

                liveLights++;
                var lightObject = new GameObject("cheap fluorescent spill");
                lightObject.transform.SetParent(parent, true);
                lightObject.transform.position = pos + Vector3.down * 0.28f;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.91f, 0.58f);
                light.range = 9f;
                light.intensity = 0.78f;
                light.shadows = LightShadows.None;

                var flicker = lightObject.AddComponent<OfficeLightFlicker>();
                flicker.seedOffset = chunkIndex * 19.31f + x * 2.7f + z * 0.49f;
                flicker.baseIntensity = 0.78f;
                flicker.minIntensity = 0.28f;
            }
        }
    }

    void BuildColumns(ChunkMesh chunk, GameObject root, float startZ, System.Random rng)
    {
        float[] columnXs = { -officeWidth * 0.36f, -officeWidth * 0.18f, officeWidth * 0.18f, officeWidth * 0.36f };
        for (int i = 0; i < columnXs.Length; i++)
        {
            if (rng.NextDouble() < 0.1f)
            {
                continue;
            }

            float z = startZ + chunkLength * (0.18f + 0.22f * i);
            Vector3 pos = new Vector3(columnXs[i], ceilingHeight * 0.5f, z);
            chunk.Box(palette.column, "concrete columns", pos, new Vector3(0.82f, ceilingHeight, 0.82f));
            chunk.Box(palette.rust, "column stains", new Vector3(columnXs[i], 0.18f, z - 0.42f), new Vector3(0.86f, 0.36f, 0.08f));
            AddBoxCollider(root, pos, new Vector3(0.82f, ceilingHeight, 0.82f));
        }
    }

    void BuildWorkstations(ChunkMesh chunk, GameObject root, float startZ, System.Random rng, bool fullDetail)
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
                float x = -usableWidth * 0.5f + columnStep * col;
                if (Mathf.Abs(x) < centerAisleWidth * 0.55f)
                {
                    continue;
                }

                if (rng.NextDouble() < 0.035f)
                {
                    continue;
                }

                Vector3 pos = new Vector3(x + Range(rng, -0.12f, 0.12f), 0f, z + Range(rng, -0.14f, 0.14f));
                Quaternion rot = Quaternion.Euler(0f, (facing > 0 ? 0f : 180f) + Range(rng, -1.4f, 1.4f), 0f);
                BuildDeskStation(chunk, pos, rot, rng, fullDetail);

                if (addDeskColliders && fullDetail)
                {
                    AddBoxCollider(root, pos + rot * new Vector3(0f, 0.52f, -0.08f), new Vector3(2.45f, 1.05f, 1.35f), rot);
                }
            }
        }
    }

    void BuildDeskStation(ChunkMesh chunk, Vector3 basePosition, Quaternion rotation, System.Random rng, bool fullDetail)
    {
        BoxLocal(chunk, palette.desk, basePosition, rotation, new Vector3(0f, 0.72f, 0f), new Vector3(2.35f, 0.16f, 1.18f));
        BoxLocal(chunk, palette.drawer, basePosition, rotation, new Vector3(-0.82f, 0.36f, -0.25f), new Vector3(0.42f, 0.66f, 0.52f));
        BoxLocal(chunk, palette.drawer, basePosition, rotation, new Vector3(0.82f, 0.36f, -0.25f), new Vector3(0.42f, 0.66f, 0.52f));
        BoxLocal(chunk, palette.deskDark, basePosition, rotation, new Vector3(0f, 0.44f, -0.54f), new Vector3(1.8f, 0.58f, 0.08f));

        float monitorX = Range(rng, -0.22f, 0.22f);
        BoxLocal(chunk, palette.computer, basePosition, rotation, new Vector3(monitorX, 1.08f, 0.17f), new Vector3(0.66f, 0.48f, 0.52f));
        BoxLocal(chunk, rng.NextDouble() < glowingScreenChance ? palette.screenGlow : palette.screenBlack, basePosition, rotation, new Vector3(monitorX, 1.1f, -0.1f), new Vector3(0.48f, 0.3f, 0.035f));
        BoxLocal(chunk, palette.computerDark, basePosition, rotation, new Vector3(monitorX, 0.83f, 0.18f), new Vector3(0.24f, 0.18f, 0.22f));
        BoxLocal(chunk, palette.keyboard, basePosition, rotation, new Vector3(monitorX, 0.84f, -0.34f), new Vector3(0.72f, 0.045f, 0.22f));

        if (!fullDetail)
        {
            return;
        }

        BoxLocal(chunk, palette.keyboard, basePosition, rotation, new Vector3(monitorX + 0.55f, 0.845f, -0.28f), new Vector3(0.17f, 0.045f, 0.22f));
        BoxLocal(chunk, palette.computer, basePosition, rotation, new Vector3(0.62f, 0.39f, 0.36f), new Vector3(0.28f, 0.55f, 0.5f));
        BuildDeskClutter(chunk, basePosition, rotation, rng);

        if (rng.NextDouble() > missingChairChance)
        {
            BuildChair(chunk, basePosition, rotation * Quaternion.Euler(0f, Range(rng, -7f, 7f), 0f), rng);
        }
    }

    void BuildDeskClutter(ChunkMesh chunk, Vector3 basePosition, Quaternion rotation, System.Random rng)
    {
        if (rng.NextDouble() < 0.55f)
        {
            BoxLocal(chunk, palette.paper, basePosition, rotation, new Vector3(Range(rng, -0.7f, 0.7f), 0.84f, Range(rng, -0.24f, 0.2f)), new Vector3(0.45f, 0.035f, 0.3f));
        }

        if (rng.NextDouble() < 0.4f)
        {
            BoxLocal(chunk, palette.binderBlue, basePosition, rotation, new Vector3(Range(rng, -0.95f, -0.35f), 0.89f, 0.12f), new Vector3(0.18f, 0.48f, 0.54f));
            BoxLocal(chunk, palette.binderGreen, basePosition, rotation, new Vector3(Range(rng, -0.75f, -0.15f), 0.9f, 0.15f), new Vector3(0.16f, 0.42f, 0.52f));
        }

        if (rng.NextDouble() < 0.3f)
        {
            BoxLocal(chunk, palette.phone, basePosition, rotation, new Vector3(-0.76f, 0.86f, -0.28f), new Vector3(0.42f, 0.09f, 0.28f));
            BoxLocal(chunk, palette.phone, basePosition, rotation, new Vector3(-0.92f, 0.92f, -0.28f), new Vector3(0.14f, 0.08f, 0.34f));
        }

        if (rng.NextDouble() < 0.22f)
        {
            BoxLocal(chunk, palette.mug, basePosition, rotation, new Vector3(0.9f, 0.88f, -0.16f), new Vector3(0.16f, 0.18f, 0.16f));
        }
    }

    void BuildChair(ChunkMesh chunk, Vector3 stationBase, Quaternion stationRotation, System.Random rng)
    {
        Vector3 chairBase = stationBase + stationRotation * new Vector3(Range(rng, -0.08f, 0.08f), 0f, -1.05f + Range(rng, -0.08f, 0.08f));
        Quaternion rot = stationRotation;

        BoxLocal(chunk, palette.chair, chairBase, rot, new Vector3(0f, 0.48f, 0f), new Vector3(0.62f, 0.12f, 0.58f));
        BoxLocal(chunk, palette.chair, chairBase, rot, new Vector3(0f, 0.88f, -0.25f), new Vector3(0.66f, 0.72f, 0.12f));
        BoxLocal(chunk, palette.chairDark, chairBase, rot, new Vector3(0f, 0.25f, 0f), new Vector3(0.12f, 0.5f, 0.12f));
        BoxLocal(chunk, palette.chairDark, chairBase, rot, new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.08f, 0.18f));
        BoxLocal(chunk, palette.chairDark, chairBase, rot, new Vector3(0f, 0.081f, 0f), new Vector3(0.18f, 0.08f, 0.9f));
    }

    void BuildArcadiaStoryDetails(ChunkMesh chunk, Transform parent, float startZ, System.Random rng, int chunkIndex, bool fullDetail)
    {
        if (!fullDetail)
        {
            return;
        }

        if (chunkIndex % 3 == 0)
        {
            AddWallPoster(chunk, parent, -1, startZ + chunkLength * 0.42f, "ARCADIA\nLIFE SCIENCES", "VITAM EX PROFUNDIS", palette.posterBlue, palette.signText);
        }

        if (chunkIndex % 4 == 1)
        {
            AddWallPoster(chunk, parent, 1, startZ + chunkLength * 0.66f, "OBSERVE\nRECORD\nDESCEND", "Deep Well Orientation", palette.posterDark, palette.warningText);
        }

        if (rng.NextDouble() < 0.45f)
        {
            float x = rng.NextDouble() < 0.5 ? -officeWidth * 0.47f : officeWidth * 0.47f;
            float z = startZ + Range(rng, 3f, chunkLength - 3f);
            chunk.Box(palette.trash, "trash bins", new Vector3(x, 0.32f, z), new Vector3(0.42f, 0.64f, 0.42f));
        }

        if (rng.NextDouble() < 0.22f)
        {
            Vector3 fanPos = new Vector3(officeWidth * 0.38f, 0.95f, startZ + chunkLength * 0.52f);
            chunk.Box(palette.fanMetal, "desk fan", fanPos, new Vector3(0.5f, 0.5f, 0.045f));
            chunk.Box(palette.fanMetal, "desk fan", fanPos + Vector3.down * 0.42f, new Vector3(0.08f, 0.82f, 0.08f));
            chunk.Box(palette.fanMetal, "desk fan", fanPos + Vector3.down * 0.84f, new Vector3(0.55f, 0.07f, 0.32f));
        }
    }

    void AddWallPoster(ChunkMesh chunk, Transform parent, int side, float z, string title, string subtitle, Material board, Material textMaterial)
    {
        float x = side * (officeWidth * 0.5f + 0.176f);
        Quaternion rotation = Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);
        Vector3 center = new Vector3(x, 1.72f, z);
        chunk.Box(board, "arcadia posters", center, new Vector3(0.035f, 1.05f, 1.45f), rotation);
        AddWorldText(parent, title, center + rotation * new Vector3(0f, 0.2f, -0.036f), rotation, 0.16f, textMaterial.color);
        AddWorldText(parent, subtitle, center + rotation * new Vector3(0f, -0.28f, -0.037f), rotation, 0.075f, textMaterial.color);
    }

    void AddWorldText(Transform parent, string text, Vector3 position, Quaternion rotation, float size, Color color)
    {
        var go = new GameObject("Arcadia poster text");
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.rotation = rotation;
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 48;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
    }

    void BoxLocal(ChunkMesh chunk, Material material, Vector3 origin, Quaternion rotation, Vector3 localCenter, Vector3 size)
    {
        chunk.Box(material, material.name, origin + rotation * localCenter, size, rotation);
    }

    void AddBoxCollider(GameObject root, Vector3 center, Vector3 size)
    {
        AddBoxCollider(root, center, size, Quaternion.identity);
    }

    void AddBoxCollider(GameObject root, Vector3 center, Vector3 size, Quaternion rotation)
    {
        var colliderObject = new GameObject("simple collision");
        colliderObject.transform.SetParent(root.transform, true);
        colliderObject.transform.position = center;
        colliderObject.transform.rotation = rotation;
        var collider = colliderObject.AddComponent<BoxCollider>();
        collider.size = size;
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

    static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}

sealed class ChunkMesh
{
    readonly Transform root;
    readonly Dictionary<Material, MeshBatch> batches = new Dictionary<Material, MeshBatch>();

    public ChunkMesh(Transform root)
    {
        this.root = root;
    }

    public void Box(Material material, string name, Vector3 center, Vector3 size)
    {
        Box(material, name, center, size, Quaternion.identity);
    }

    public void Box(Material material, string name, Vector3 center, Vector3 size, Quaternion rotation)
    {
        if (!batches.TryGetValue(material, out MeshBatch batch))
        {
            batch = new MeshBatch(name);
            batches.Add(material, batch);
        }

        batch.AddBox(center, size, rotation);
    }

    public void Flush()
    {
        foreach (KeyValuePair<Material, MeshBatch> pair in batches)
        {
            Mesh mesh = pair.Value.CreateMesh();
            var go = new GameObject(pair.Value.name);
            go.transform.SetParent(root, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = pair.Key;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}

sealed class MeshBatch
{
    public readonly string name;
    readonly List<Vector3> vertices = new List<Vector3>(2048);
    readonly List<Vector3> normals = new List<Vector3>(2048);
    readonly List<Vector2> uvs = new List<Vector2>(2048);
    readonly List<int> triangles = new List<int>(4096);

    static readonly Vector3[] cubeCorners =
    {
        new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
    };

    static readonly int[,] faces =
    {
        {0, 1, 2, 3}, {5, 4, 7, 6}, {4, 0, 3, 7},
        {1, 5, 6, 2}, {3, 2, 6, 7}, {4, 5, 1, 0}
    };

    static readonly Vector3[] faceNormals =
    {
        Vector3.back, Vector3.forward, Vector3.left,
        Vector3.right, Vector3.up, Vector3.down
    };

    public MeshBatch(string name)
    {
        this.name = name;
    }

    public void AddBox(Vector3 center, Vector3 size, Quaternion rotation)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(center, rotation, size);
        for (int f = 0; f < 6; f++)
        {
            int start = vertices.Count;
            for (int i = 0; i < 4; i++)
            {
                vertices.Add(matrix.MultiplyPoint3x4(cubeCorners[faces[f, i]]));
                normals.Add(rotation * faceNormals[f]);
            }

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }
    }

    public Mesh CreateMesh()
    {
        var mesh = new Mesh();
        mesh.name = name + " mesh";
        if (vertices.Count > 65000)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}

sealed class OfficePalette
{
    public Material carpet;
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
    public Material binderBlue;
    public Material binderGreen;
    public Material phone;
    public Material mug;
    public Material trash;
    public Material fanMetal;
    public Material posterBlue;
    public Material posterDark;
    public Material signText;
    public Material warningText;

    public static OfficePalette Create()
    {
        return new OfficePalette
        {
            carpet = Make("olive acoustic carpet", new Color(0.29f, 0.31f, 0.2f), 0f, 0.08f, TextureKind.Carpet),
            ceiling = Make("stained acoustic ceiling tile", new Color(0.48f, 0.45f, 0.28f), 0f, 0f, TextureKind.Ceiling),
            ceilingRail = Make("dark ceiling rail", new Color(0.16f, 0.15f, 0.1f)),
            wall = Make("aged yellow wallpaper", new Color(0.43f, 0.39f, 0.2f), 0f, 0f, TextureKind.Wall),
            wallTrim = Make("faint wall seam trim", new Color(0.25f, 0.23f, 0.12f)),
            fluorescent = Make("sick fluorescent panel", new Color(0.95f, 0.94f, 0.82f), 1.35f, 0.05f, TextureKind.Light),
            deadLight = Make("dead fluorescent panel", new Color(0.32f, 0.31f, 0.24f)),
            desk = Make("laminate office desk", new Color(0.52f, 0.49f, 0.36f), 0f, 0.18f, TextureKind.Laminate),
            deskDark = Make("dark desk underside", new Color(0.26f, 0.24f, 0.17f)),
            drawer = Make("metal drawer cabinet", new Color(0.41f, 0.39f, 0.28f), 0f, 0.2f),
            computer = Make("aged beige plastic", new Color(0.68f, 0.62f, 0.42f)),
            computerDark = Make("shadowed beige plastic", new Color(0.42f, 0.38f, 0.27f)),
            screenBlack = Make("dead black monitor glass", new Color(0.015f, 0.018f, 0.014f), 0f, 0.05f),
            screenGlow = Make("unsettling green monitor glow", new Color(0.27f, 0.85f, 0.38f), 0.75f),
            keyboard = Make("yellowed keyboard", new Color(0.52f, 0.49f, 0.36f)),
            chair = Make("muted office chair fabric", new Color(0.12f, 0.16f, 0.18f), 0f, 0.05f),
            chairDark = Make("dark chair metal", new Color(0.08f, 0.09f, 0.08f)),
            column = Make("dirty concrete column", new Color(0.42f, 0.39f, 0.31f), 0f, 0f, TextureKind.Concrete),
            rust = Make("rust and old leak stain", new Color(0.34f, 0.12f, 0.04f)),
            paper = Make("abandoned paperwork", new Color(0.72f, 0.68f, 0.51f)),
            binderBlue = Make("arcadia blue binder", new Color(0.02f, 0.16f, 0.28f)),
            binderGreen = Make("deep well green binder", new Color(0.08f, 0.3f, 0.18f)),
            phone = Make("corded office phone", new Color(0.08f, 0.08f, 0.065f)),
            mug = Make("stained coffee mug", new Color(0.74f, 0.68f, 0.5f)),
            trash = Make("black plastic waste bin", new Color(0.035f, 0.035f, 0.032f), 0f, 0.2f),
            fanMetal = Make("dusty fan metal", new Color(0.55f, 0.55f, 0.48f), 0f, 0.35f),
            posterBlue = Make("arcadia brushed steel sign", new Color(0.62f, 0.68f, 0.68f), 0f, 0.25f, TextureKind.MetalSign),
            posterDark = Make("dark compliance poster", new Color(0.05f, 0.07f, 0.065f), 0f, 0.08f),
            signText = MakeUnlit("arcadia sign text", new Color(0.02f, 0.17f, 0.28f)),
            warningText = MakeUnlit("warning poster text", new Color(0.83f, 0.82f, 0.68f))
        };
    }

    static Material Make(string name, Color color, float emission = 0f, float smoothness = 0f, TextureKind textureKind = TextureKind.None)
    {
        var material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        material.SetFloat("_Glossiness", smoothness);

        if (textureKind != TextureKind.None)
        {
            material.mainTexture = ProceduralTexture(textureKind, color);
        }

        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }

        return material;
    }

    static Material MakeUnlit(string name, Color color)
    {
        Material material = new Material(Shader.Find("Unlit/Color"));
        material.name = name;
        material.color = color;
        return material;
    }

    static Texture2D ProceduralTexture(TextureKind kind, Color baseColor)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = kind + " texture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise((x + (int)kind * 17) * 0.17f, (y + (int)kind * 31) * 0.17f);
                float stripe = 0f;
                if (kind == TextureKind.Ceiling)
                {
                    stripe = x % 16 == 0 || y % 16 == 0 ? -0.18f : 0f;
                }
                else if (kind == TextureKind.Carpet)
                {
                    stripe = ((x + y) % 7 == 0) ? 0.08f : 0f;
                }
                else if (kind == TextureKind.Light)
                {
                    stripe = x % 10 < 2 ? 0.18f : 0f;
                }
                else if (kind == TextureKind.MetalSign)
                {
                    stripe = x % 3 == 0 ? 0.07f : 0f;
                }

                float value = Mathf.Clamp01(0.86f + (n - 0.5f) * 0.22f + stripe);
                texture.SetPixel(x, y, baseColor * value);
            }
        }

        texture.Apply();
        return texture;
    }

    enum TextureKind
    {
        None,
        Carpet,
        Ceiling,
        Wall,
        Light,
        Laminate,
        Concrete,
        MetalSign
    }
}
