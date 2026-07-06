using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcadia.EditorTools
{
    internal static class CreateSparseRealtimeCeilingLights
    {
        private const string RequestFilePath =
            "Temp/CodexSparseRealtimeCeilingLights.request";
        private const string ResultFilePath =
            "Temp/CodexSparseRealtimeCeilingLights.result.txt";
        private const string OfficeRootName = "office 1";
        private const string AreaArrayRootName = "Ceiling_Area_Light_Array";
        private const string RealtimeRootName = "Ceiling_Realtime_Light";
        private const string SparseRootName = "Ceiling_Sparse_Realtime_Light_Array";
        private const float Density = 0.2f;
        private const float SpotAngle = 120f;
        private const float InnerSpotAngle = 96f;
        private const float LightIntensity = 2.8f;
        private const float LightRange = 12f;

        private static bool requestQueued;
        private static bool updateHookInstalled;

        private readonly struct CeilingCandidate
        {
            public readonly MeshFilter MeshFilter;
            public readonly Renderer Renderer;
            public readonly Vector3[] Centers;

            public CeilingCandidate(MeshFilter meshFilter, Renderer renderer, Vector3[] centers)
            {
                MeshFilter = meshFilter;
                Renderer = renderer;
                Centers = centers;
            }
        }

        [InitializeOnLoadMethod]
        private static void QueueRequestedCreation()
        {
            if (!updateHookInstalled)
            {
                EditorApplication.update += WatchForRequestedCreation;
                updateHookInstalled = true;
            }

            WatchForRequestedCreation();
        }

        private static void WatchForRequestedCreation()
        {
            if (requestQueued ||
                !File.Exists(RequestFilePath) ||
                EditorApplication.isCompiling)
            {
                return;
            }

            requestQueued = true;
            EditorApplication.delayCall += RunRequestedCreation;
        }

        [MenuItem("Tools/Office/Create Sparse Realtime Ceiling Lights")]
        private static void CreateForActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                throw new InvalidOperationException(
                    "Open a saved scene before creating sparse realtime ceiling lights.");
            }

            GameObject[] roots = scene.GetRootGameObjects();
            GameObject officeRoot = roots.FirstOrDefault(root => root.name == OfficeRootName);
            if (officeRoot == null)
            {
                throw new InvalidOperationException(
                    $"Scene root '{OfficeRootName}' was not found.");
            }

            CeilingCandidate ceiling = FindCeilingCandidate(officeRoot);
            if (ceiling.Centers == null || ceiling.Centers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Could not find ceiling recess centers for sparse light placement.");
            }

            int totalCount = ceiling.Centers.Length;
            int keepCount = Mathf.Max(1, Mathf.RoundToInt(totalCount * Density));
            Vector3[] keptCenters = SelectEvenlyDistributedCenters(
                ceiling.Centers,
                keepCount);

            float lightY = ceiling.Renderer.bounds.min.y - 0.08f;

            Undo.SetCurrentGroupName("Create sparse realtime ceiling lights");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject root in scene.GetRootGameObjects()
                         .Where(root =>
                             root.name == AreaArrayRootName ||
                             root.name == RealtimeRootName ||
                             root.name == SparseRootName)
                         .ToArray())
            {
                Undo.DestroyObjectImmediate(root);
            }

            GameObject sparseRoot = new GameObject(SparseRootName);
            Undo.RegisterCreatedObjectUndo(
                sparseRoot,
                "Create sparse realtime ceiling light root");
            SceneManager.MoveGameObjectToScene(sparseRoot, scene);
            sparseRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (Vector3 center in keptCenters)
            {
                GameObject lightObject = new GameObject(
                    $"SparseRealtimeLight_{center.x:F2}_{center.z:F2}");
                Undo.RegisterCreatedObjectUndo(
                    lightObject,
                    "Create sparse realtime ceiling light");
                lightObject.transform.SetParent(sparseRoot.transform, false);
                lightObject.transform.SetPositionAndRotation(
                    new Vector3(center.x, lightY, center.z),
                    Quaternion.LookRotation(Vector3.down, Vector3.forward));

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.lightmapBakeType = LightmapBakeType.Realtime;
                light.color = Color.white;
                light.intensity = LightIntensity;
                light.range = LightRange;
                light.spotAngle = SpotAngle;
                light.innerSpotAngle = InnerSpotAngle;
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.ForcePixel;
                light.cullingMask = -1;
                light.useColorTemperature = true;
                light.colorTemperature = 4200f;
            }

            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.Cancel();
            Lightmapping.Clear();

            Selection.activeGameObject = sparseRoot;
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Failed to save scene: {scene.path}");
            }

            Debug.Log(
                $"[CreateSparseRealtimeCeilingLights] Created {keepCount} of {totalCount} " +
                $"ceiling spot lights at evenly distributed recess centers. " +
                $"Density {Density:P0}, spot angle {SpotAngle:F0}, downward.");
        }

        private static Vector3[] SelectEvenlyDistributedCenters(
            IReadOnlyList<Vector3> centers,
            int keepCount)
        {
            if (keepCount >= centers.Count)
            {
                return centers.ToArray();
            }

            Vector3 centroid = new Vector3(
                centers.Average(center => center.x),
                centers.Average(center => center.y),
                centers.Average(center => center.z));

            int firstIndex = Enumerable.Range(0, centers.Count)
                .OrderBy(index => HorizontalDistanceSquared(centers[index], centroid))
                .First();

            var selected = new List<Vector3>(keepCount) { centers[firstIndex] };
            var remaining = centers
                .Where((_, index) => index != firstIndex)
                .ToList();

            while (selected.Count < keepCount && remaining.Count > 0)
            {
                int bestIndex = 0;
                float bestDistance = float.MinValue;
                for (int index = 0; index < remaining.Count; index++)
                {
                    Vector3 candidate = remaining[index];
                    float nearestSelectedDistance = selected
                        .Min(selectedCenter =>
                            HorizontalDistanceSquared(candidate, selectedCenter));
                    if (nearestSelectedDistance > bestDistance)
                    {
                        bestDistance = nearestSelectedDistance;
                        bestIndex = index;
                    }
                }

                selected.Add(remaining[bestIndex]);
                remaining.RemoveAt(bestIndex);
            }

            return selected
                .OrderBy(center => center.z)
                .ThenBy(center => center.x)
                .ToArray();
        }

        private static CeilingCandidate FindCeilingCandidate(GameObject officeRoot)
        {
            CeilingCandidate best = default;
            int bestCount = 0;
            float bestMaxY = float.MinValue;

            foreach (MeshFilter meshFilter in officeRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Vector3[] centers = ExtractRecessCenters(meshFilter).ToArray();
                if (centers.Length < 450 || centers.Length > 550)
                {
                    continue;
                }

                Renderer renderer = meshFilter.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                float maxY = renderer.bounds.max.y;
                if (centers.Length > bestCount ||
                    (centers.Length == bestCount && maxY > bestMaxY))
                {
                    best = new CeilingCandidate(meshFilter, renderer, centers);
                    bestCount = centers.Length;
                    bestMaxY = maxY;
                }
            }

            return best;
        }

        private static float HorizontalDistanceSquared(Vector3 left, Vector3 right)
        {
            float deltaX = left.x - right.x;
            float deltaZ = left.z - right.z;
            return deltaX * deltaX + deltaZ * deltaZ;
        }

        private static List<Vector3> ExtractRecessCenters(MeshFilter ceilingMeshFilter)
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

        private static void RunRequestedCreation()
        {
            try
            {
                if (File.Exists(RequestFilePath))
                {
                    File.Delete(RequestFilePath);
                }

                CreateForActiveScene();
                File.WriteAllText(
                    ResultFilePath,
                    $"SUCCESS {DateTime.Now:O}{Environment.NewLine}");
            }
            catch (Exception exception)
            {
                File.WriteAllText(
                    ResultFilePath,
                    $"FAIL {DateTime.Now:O}{Environment.NewLine}{exception}{Environment.NewLine}");
                Debug.LogException(exception);
            }
            finally
            {
                requestQueued = false;
            }
        }
    }
}
