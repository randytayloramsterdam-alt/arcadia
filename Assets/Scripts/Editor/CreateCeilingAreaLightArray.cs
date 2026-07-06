using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcadia.EditorTools
{
    internal static class CreateCeilingAreaLightArray
    {
        private const string OfficeRootName = "office 1";
        private const string CeilingName = "天花板";
        private const string ArrayRootName = "Ceiling_Area_Light_Array";
        private const float FixtureSize = 0.95f;
        private const float EdgeGap = 2f;
        private const float CenterPitch = FixtureSize + EdgeGap;
        private const float CeilingOffset = 0.03f;

        private readonly struct ProjectedTriangle
        {
            public readonly Vector2 A;
            public readonly Vector2 B;
            public readonly Vector2 C;

            public ProjectedTriangle(Vector2 a, Vector2 b, Vector2 c)
            {
                A = a;
                B = b;
                C = c;
            }
        }

        [MenuItem("Tools/Office/Create Ceiling Area Light Array")]
        private static void CreateArray()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject officeRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == OfficeRootName);
            if (officeRoot == null)
            {
                throw new InvalidOperationException(
                    $"Scene root '{OfficeRootName}' was not found.");
            }

            Transform ceiling = officeRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == CeilingName);
            if (ceiling == null)
            {
                throw new InvalidOperationException(
                    $"Ceiling object '{CeilingName}' was not found.");
            }

            if (scene.GetRootGameObjects().Any(root => root.name == ArrayRootName))
            {
                throw new InvalidOperationException(
                    $"'{ArrayRootName}' already exists. The array was not created twice.");
            }

            Renderer[] ceilingRenderers = ceiling.GetComponentsInChildren<Renderer>(true);
            if (ceilingRenderers.Length == 0)
            {
                throw new InvalidOperationException("The ceiling contains no renderers.");
            }

            Bounds ceilingBounds = ceilingRenderers[0].bounds;
            for (int index = 1; index < ceilingRenderers.Length; index++)
            {
                ceilingBounds.Encapsulate(ceilingRenderers[index].bounds);
            }

            List<ProjectedTriangle> ceilingTriangles = BuildProjectedTriangles(ceiling);
            if (ceilingTriangles.Count == 0)
            {
                throw new InvalidOperationException(
                    "No horizontal ceiling triangles were found.");
            }

            int columnCount = Mathf.FloorToInt(
                (ceilingBounds.size.x + EdgeGap) / CenterPitch);
            int rowCount = Mathf.FloorToInt(
                (ceilingBounds.size.z + EdgeGap) / CenterPitch);
            if (columnCount < 1 || rowCount < 1)
            {
                throw new InvalidOperationException("The ceiling is too small for the array.");
            }

            float arrayWidth =
                columnCount * FixtureSize + (columnCount - 1) * EdgeGap;
            float arrayDepth =
                rowCount * FixtureSize + (rowCount - 1) * EdgeGap;
            float startX =
                ceilingBounds.min.x + (ceilingBounds.size.x - arrayWidth) * 0.5f +
                FixtureSize * 0.5f;
            float startZ =
                ceilingBounds.min.z + (ceilingBounds.size.z - arrayDepth) * 0.5f +
                FixtureSize * 0.5f;
            float lightY = ceilingBounds.min.y - CeilingOffset;

            Undo.SetCurrentGroupName("Create ceiling area light array");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject arrayRoot = new GameObject(ArrayRootName);
            Undo.RegisterCreatedObjectUndo(arrayRoot, "Create ceiling light array root");
            SceneManager.MoveGameObjectToScene(arrayRoot, scene);
            arrayRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            arrayRoot.transform.localScale = Vector3.one;

            int createdCount = 0;
            int omittedCount = 0;
            float halfSize = FixtureSize * 0.5f;
            for (int row = 0; row < rowCount; row++)
            {
                float z = startZ + row * CenterPitch;
                for (int column = 0; column < columnCount; column++)
                {
                    float x = startX + column * CenterPitch;
                    Vector2 center = new Vector2(x, z);
                    if (!FixtureFitsCeiling(center, halfSize, ceilingTriangles))
                    {
                        omittedCount++;
                        continue;
                    }

                    GameObject lightObject = new GameObject(
                        $"AreaLight_R{row + 1:00}_C{column + 1:00}");
                    Undo.RegisterCreatedObjectUndo(lightObject, "Create ceiling area light");
                    lightObject.transform.SetParent(arrayRoot.transform, false);
                    lightObject.transform.SetPositionAndRotation(
                        new Vector3(x, lightY, z),
                        Quaternion.LookRotation(Vector3.down, Vector3.forward));

                    Light light = lightObject.AddComponent<Light>();
                    light.type = LightType.Area;
                    light.areaSize = new Vector2(FixtureSize, FixtureSize);
                    light.color = Color.white;
                    light.useColorTemperature = true;
                    light.colorTemperature = 4200f;
                    light.intensity = 3f;
                    light.bounceIntensity = 1f;
                    light.range = 6f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.85f;
                    light.shadowBias = 0.05f;
                    light.shadowNormalBias = 0.4f;
                    light.lightmapBakeType = LightmapBakeType.Baked;
                    light.cullingMask = -1;
                    createdCount++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = arrayRoot;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save scene: {scene.path}");
            }

            Debug.Log(
                $"[CreateCeilingAreaLightArray] Created {createdCount} baked area lights. " +
                $"Fixture {FixtureSize:F2}m, edge gap {EdgeGap:F2}m, center pitch " +
                $"{CenterPitch:F2}m, grid {columnCount} x {rowCount}, " +
                $"{omittedCount} cells omitted outside the L-shaped ceiling, " +
                $"height Y={lightY:F3}, 4200K, intensity 3.0.");
        }

        [MenuItem("Tools/Office/Snap Area Lights To Ceiling Recesses")]
        private static void SnapLightsToRecesses()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject officeRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == OfficeRootName);
            GameObject arrayRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == ArrayRootName);
            if (officeRoot == null || arrayRoot == null)
            {
                throw new InvalidOperationException(
                    "The office root or ceiling light array root is missing.");
            }

            Transform ceiling = officeRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == CeilingName);
            Renderer ceilingRenderer = ceiling != null
                ? ceiling.GetComponentInChildren<Renderer>(true)
                : null;
            if (ceilingRenderer == null)
            {
                throw new InvalidOperationException("The ceiling renderer is missing.");
            }

            MeshFilter ceilingMeshFilter =
                ceiling.GetComponentInChildren<MeshFilter>(true);
            if (ceilingMeshFilter == null ||
                ceilingMeshFilter.sharedMesh == null)
            {
                throw new InvalidOperationException(
                    "The ceiling mesh is missing.");
            }

            Vector3[] recessCenters =
                ExtractRecessCenters(ceilingMeshFilter)
                    .OrderBy(center => center.z)
                    .ThenBy(center => center.x)
                .ToArray();
            if (recessCenters.Length < 450 || recessCenters.Length > 550)
            {
                throw new InvalidOperationException(
                    $"Extracted {recessCenters.Length} ceiling recess centers; " +
                    "expected roughly 500. No lights were changed.");
            }

            Undo.SetCurrentGroupName("Snap area lights to ceiling recesses");
            int undoGroup = Undo.GetCurrentGroup();
            foreach (Transform child in
                     arrayRoot.transform.Cast<Transform>().ToArray())
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }

            float lightY = ceilingRenderer.bounds.min.y - 0.08f;
            for (int index = 0; index < recessCenters.Length; index++)
            {
                Vector3 center = recessCenters[index];
                GameObject lightObject =
                    new GameObject($"AreaLight_Recess_{index + 1:000}");
                Undo.RegisterCreatedObjectUndo(
                    lightObject, "Create aligned ceiling area light");
                lightObject.transform.SetParent(arrayRoot.transform, true);
                lightObject.transform.SetPositionAndRotation(
                    new Vector3(center.x, lightY, center.z),
                    Quaternion.LookRotation(Vector3.down, Vector3.forward));

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Area;
                light.areaSize = new Vector2(FixtureSize, FixtureSize);
                light.color = Color.white;
                light.useColorTemperature = true;
                light.colorTemperature = 4200f;
                light.intensity = 3f;
                light.bounceIntensity = 1f;
                light.range = 6f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.85f;
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.cullingMask = -1;
            }

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = arrayRoot;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save scene: {scene.path}");
            }

            Debug.Log(
                $"[CreateCeilingAreaLightArray] Rebuilt {recessCenters.Length} " +
                $"lights at the exact centers extracted from the ceiling mesh. " +
                $"Fixture {FixtureSize:F2}m, light Y={lightY:F3}, 0.08m below " +
                $"the ceiling underside.");
        }

        [MenuItem("Tools/Office/Report Ceiling Recess Geometry")]
        private static void ReportCeilingRecessGeometry()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject officeRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == OfficeRootName);
            Transform ceiling = officeRoot != null
                ? officeRoot.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == CeilingName)
                : null;
            Renderer ceilingRenderer = ceiling != null
                ? ceiling.GetComponentInChildren<Renderer>(true)
                : null;
            if (officeRoot == null || ceilingRenderer == null)
            {
                throw new InvalidOperationException(
                    "The office root or ceiling renderer is missing.");
            }

            Renderer[] cubes = officeRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.gameObject.name == "Cube" ||
                    renderer.gameObject.name.StartsWith(
                        "Cube.", StringComparison.Ordinal))
                .ToArray();
            Renderer[] allRenderers =
                officeRoot.GetComponentsInChildren<Renderer>(true);
            string[] nameGroups = allRenderers
                .GroupBy(renderer =>
                {
                    string name = renderer.gameObject.name;
                    int suffixSeparator = name.LastIndexOf('.');
                    return suffixSeparator > 0 &&
                           int.TryParse(
                               name.Substring(suffixSeparator + 1),
                               out _)
                        ? name.Substring(0, suffixSeparator)
                        : name;
                })
                .OrderByDescending(group => group.Count())
                .Take(20)
                .Select(group => $"{group.Key}={group.Count()}")
                .ToArray();
            string[] sizeGroups = allRenderers
                .GroupBy(renderer =>
                {
                    Vector3 size = renderer.bounds.size;
                    return
                        $"{size.x:F2}x{size.y:F2}x{size.z:F2}";
                })
                .OrderByDescending(group => group.Count())
                .Take(20)
                .Select(group =>
                {
                    string samples = string.Join(
                        ", ",
                        group.Take(3).Select(renderer =>
                            renderer.gameObject.name));
                    return $"{group.Key}={group.Count()} [{samples}]";
                })
                .ToArray();
            Renderer[] nearest = cubes
                .OrderBy(renderer =>
                    Mathf.Abs(
                        renderer.bounds.center.y -
                        ceilingRenderer.bounds.min.y))
                .Take(24)
                .ToArray();
            string[] details = nearest
                .Select(renderer =>
                {
                    Bounds bounds = renderer.bounds;
                    return
                        $"{renderer.gameObject.name}: centerY={bounds.center.y:F3}, " +
                        $"size=({bounds.size.x:F3},{bounds.size.y:F3},{bounds.size.z:F3})";
                })
                .ToArray();

            Debug.Log(
                $"[CreateCeilingAreaLightArray] Ceiling minY=" +
                $"{ceilingRenderer.bounds.min.y:F3}; all renderers=" +
                $"{allRenderers.Length}; Cube renderers={cubes.Length}; " +
                $"name groups: {string.Join(" | ", nameGroups)}; " +
                $"size groups: {string.Join(" | ", sizeGroups)}; " +
                $"nearest cubes: {string.Join(" | ", details)}");
        }

        private static float HorizontalDistanceSquared(Vector3 left, Vector3 right)
        {
            float x = left.x - right.x;
            float z = left.z - right.z;
            return x * x + z * z;
        }

        private static List<Vector3> ExtractRecessCenters(
            MeshFilter ceilingMeshFilter)
        {
            Mesh mesh = ceilingMeshFilter.sharedMesh;
            Vector3[] localVertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3[] worldVertices = localVertices
                .Select(ceilingMeshFilter.transform.TransformPoint)
                .ToArray();
            float topY = worldVertices.Max(vertex => vertex.y);
            const float layerTolerance = 0.005f;
            const float positionTolerance = 0.001f;

            string PositionKey(Vector3 point)
            {
                int x = Mathf.RoundToInt(point.x / positionTolerance);
                int y = Mathf.RoundToInt(point.y / positionTolerance);
                int z = Mathf.RoundToInt(point.z / positionTolerance);
                return $"{x}:{y}:{z}";
            }

            var pointByKey = new Dictionary<string, Vector3>();
            var adjacency = new Dictionary<string, HashSet<string>>();
            for (int index = 0; index < worldVertices.Length; index++)
            {
                Vector3 vertex = worldVertices[index];
                if (Mathf.Abs(vertex.y - topY) > layerTolerance)
                {
                    continue;
                }

                string key = PositionKey(vertex);
                pointByKey[key] = vertex;
                if (!adjacency.ContainsKey(key))
                {
                    adjacency[key] = new HashSet<string>();
                }
            }

            for (int index = 0; index + 2 < triangles.Length; index += 3)
            {
                int[] triangle =
                {
                    triangles[index],
                    triangles[index + 1],
                    triangles[index + 2]
                };
                for (int edge = 0; edge < 3; edge++)
                {
                    Vector3 first = worldVertices[triangle[edge]];
                    Vector3 second = worldVertices[triangle[(edge + 1) % 3]];
                    if (Mathf.Abs(first.y - topY) > layerTolerance ||
                        Mathf.Abs(second.y - topY) > layerTolerance)
                    {
                        continue;
                    }

                    string firstKey = PositionKey(first);
                    string secondKey = PositionKey(second);
                    adjacency[firstKey].Add(secondKey);
                    adjacency[secondKey].Add(firstKey);
                }
            }

            var visited = new HashSet<string>();
            var centers = new List<Vector3>();
            foreach (string start in adjacency.Keys)
            {
                if (!visited.Add(start))
                {
                    continue;
                }

                var pending = new Stack<string>();
                var component = new List<Vector3>();
                pending.Push(start);
                while (pending.Count > 0)
                {
                    string current = pending.Pop();
                    component.Add(pointByKey[current]);
                    foreach (string neighbor in adjacency[current])
                    {
                        if (visited.Add(neighbor))
                        {
                            pending.Push(neighbor);
                        }
                    }
                }

                float width =
                    component.Max(point => point.x) -
                    component.Min(point => point.x);
                float depth =
                    component.Max(point => point.z) -
                    component.Min(point => point.z);
                bool fixtureSized =
                    width <= 1.2f && depth <= 1.2f &&
                    (width >= 0.5f || depth >= 0.5f);
                if (component.Count >= 2 &&
                    component.Count <= 8 &&
                    fixtureSized)
                {
                    centers.Add(
                        new Vector3(
                            component.Average(point => point.x),
                            component.Average(point => point.y),
                            component.Average(point => point.z)));
                }
            }

            return centers;
        }

        private static List<ProjectedTriangle> BuildProjectedTriangles(Transform ceiling)
        {
            var triangles = new List<ProjectedTriangle>();
            foreach (MeshFilter meshFilter in
                     ceiling.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                using (Mesh.MeshDataArray meshDataArray =
                       Mesh.AcquireReadOnlyMeshData(mesh))
                {
                    Mesh.MeshData meshData = meshDataArray[0];
                    using (var vertices =
                           new NativeArray<Vector3>(
                               meshData.vertexCount, Allocator.Temp))
                    {
                        meshData.GetVertices(vertices);
                        for (int subMesh = 0;
                             subMesh < meshData.subMeshCount;
                             subMesh++)
                        {
                            int indexCount =
                                (int)meshData.GetSubMesh(subMesh).indexCount;
                            using (var indices =
                                   new NativeArray<int>(
                                       indexCount, Allocator.Temp))
                            {
                                meshData.GetIndices(indices, subMesh);
                                for (int index = 0;
                                     index + 2 < indices.Length;
                                     index += 3)
                                {
                                    Vector3 worldA =
                                        meshFilter.transform.TransformPoint(
                                            vertices[indices[index]]);
                                    Vector3 worldB =
                                        meshFilter.transform.TransformPoint(
                                            vertices[indices[index + 1]]);
                                    Vector3 worldC =
                                        meshFilter.transform.TransformPoint(
                                            vertices[indices[index + 2]]);
                                    Vector2 a = new Vector2(worldA.x, worldA.z);
                                    Vector2 b = new Vector2(worldB.x, worldB.z);
                                    Vector2 c = new Vector2(worldC.x, worldC.z);
                                    if (Mathf.Abs(Cross(b - a, c - a)) > 0.0001f)
                                    {
                                        triangles.Add(
                                            new ProjectedTriangle(a, b, c));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return triangles;
        }

        private static bool FixtureFitsCeiling(
            Vector2 center,
            float halfSize,
            IReadOnlyList<ProjectedTriangle> triangles)
        {
            Vector2[] samples =
            {
                center,
                center + new Vector2(-halfSize, -halfSize),
                center + new Vector2(-halfSize, halfSize),
                center + new Vector2(halfSize, -halfSize),
                center + new Vector2(halfSize, halfSize)
            };
            return samples.All(sample => IsPointInside(sample, triangles));
        }

        private static bool IsPointInside(
            Vector2 point,
            IReadOnlyList<ProjectedTriangle> triangles)
        {
            const float tolerance = 0.0001f;
            foreach (ProjectedTriangle triangle in triangles)
            {
                float edgeAB = Cross(triangle.B - triangle.A, point - triangle.A);
                float edgeBC = Cross(triangle.C - triangle.B, point - triangle.B);
                float edgeCA = Cross(triangle.A - triangle.C, point - triangle.C);
                bool hasNegative =
                    edgeAB < -tolerance || edgeBC < -tolerance ||
                    edgeCA < -tolerance;
                bool hasPositive =
                    edgeAB > tolerance || edgeBC > tolerance ||
                    edgeCA > tolerance;
                if (!(hasNegative && hasPositive))
                {
                    return true;
                }
            }

            return false;
        }

        private static float Cross(Vector2 left, Vector2 right)
        {
            return left.x * right.y - left.y * right.x;
        }
    }
}
