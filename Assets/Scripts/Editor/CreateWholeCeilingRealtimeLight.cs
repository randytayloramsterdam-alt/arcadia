using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcadia.EditorTools
{
    internal static class CreateWholeCeilingRealtimeLight
    {
        private const string RequestFilePath =
            "Temp/CodexWholeCeilingRealtimeLight.request";
        private const string ResultFilePath =
            "Temp/CodexWholeCeilingRealtimeLight.result.txt";
        private const string OfficeRootName = "office 1";
        private const string AreaArrayRootName = "Ceiling_Area_Light_Array";
        private const string RealtimeRootName = "Ceiling_Realtime_Light";

        private static bool requestQueued;
        private static bool updateHookInstalled;

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

        [MenuItem("Tools/Office/Create Whole Ceiling Realtime Light")]
        private static void CreateForActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                throw new InvalidOperationException(
                    "Open a saved scene before creating the realtime ceiling light.");
            }

            GameObject[] roots = scene.GetRootGameObjects();
            GameObject officeRoot = roots.FirstOrDefault(root => root.name == OfficeRootName);
            if (officeRoot == null)
            {
                throw new InvalidOperationException(
                    $"Scene root '{OfficeRootName}' was not found.");
            }

            Renderer[] officeRenderers = officeRoot.GetComponentsInChildren<Renderer>(true);
            if (officeRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"'{OfficeRootName}' contains no renderers.");
            }

            Bounds officeBounds = officeRenderers[0].bounds;
            for (int index = 1; index < officeRenderers.Length; index++)
            {
                officeBounds.Encapsulate(officeRenderers[index].bounds);
            }

            Vector3 lightCenter;
            Vector3 coverageSize;
            float ceilingY;

            GameObject areaArrayRoot = roots.FirstOrDefault(root => root.name == AreaArrayRootName);
            if (areaArrayRoot != null && areaArrayRoot.transform.childCount > 0)
            {
                Transform[] lightTransforms = areaArrayRoot.transform
                    .Cast<Transform>()
                    .ToArray();
                Bounds lightBounds = new Bounds(
                    lightTransforms[0].position,
                    Vector3.zero);
                foreach (Transform child in lightTransforms.Skip(1))
                {
                    lightBounds.Encapsulate(child.position);
                }

                lightCenter = lightBounds.center;
                coverageSize = new Vector3(
                    Mathf.Max(lightBounds.size.x, 1f),
                    0f,
                    Mathf.Max(lightBounds.size.z, 1f));
                ceilingY = lightTransforms.Average(child => child.position.y) + 0.02f;
            }
            else
            {
                lightCenter = new Vector3(
                    officeBounds.center.x,
                    0f,
                    officeBounds.center.z);
                coverageSize = new Vector3(
                    Mathf.Max(officeBounds.size.x * 0.9f, 1f),
                    0f,
                    Mathf.Max(officeBounds.size.z * 0.9f, 1f));
                ceilingY = officeBounds.max.y - 0.2f;
            }

            float floorY = officeBounds.min.y;
            float lightY = Mathf.Max(ceilingY - 0.08f, floorY + 1.5f);
            lightCenter.y = lightY;

            float halfWidth = coverageSize.x * 0.5f;
            float halfDepth = coverageSize.z * 0.5f;
            float maxHorizontalDistance = Mathf.Sqrt(
                halfWidth * halfWidth + halfDepth * halfDepth);
            float verticalDistance = Mathf.Max(lightY - floorY, 1f);
            float spotAngle = Mathf.Clamp(
                2f * Mathf.Rad2Deg * Mathf.Atan2(
                    maxHorizontalDistance * 1.05f,
                    verticalDistance),
                100f,
                179f);
            float range = Mathf.Sqrt(
                maxHorizontalDistance * maxHorizontalDistance +
                verticalDistance * verticalDistance) + 4f;

            Undo.SetCurrentGroupName("Create whole ceiling realtime light");
            int undoGroup = Undo.GetCurrentGroup();

            if (areaArrayRoot != null)
            {
                Undo.DestroyObjectImmediate(areaArrayRoot);
            }

            GameObject existingRealtimeRoot =
                scene.GetRootGameObjects()
                    .FirstOrDefault(root => root.name == RealtimeRootName);
            if (existingRealtimeRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRealtimeRoot);
            }

            GameObject realtimeRoot = new GameObject(RealtimeRootName);
            Undo.RegisterCreatedObjectUndo(
                realtimeRoot,
                "Create realtime ceiling light root");
            SceneManager.MoveGameObjectToScene(realtimeRoot, scene);
            realtimeRoot.transform.SetPositionAndRotation(
                lightCenter,
                Quaternion.identity);

            Light light = realtimeRoot.AddComponent<Light>();
            light.type = LightType.Spot;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = Color.white;
            light.intensity = 4f;
            light.range = range;
            light.spotAngle = spotAngle;
            light.innerSpotAngle = Mathf.Clamp(spotAngle * 0.82f, 80f, spotAngle);
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            light.cullingMask = -1;
            light.useColorTemperature = true;
            light.colorTemperature = 4200f;
            light.bounceIntensity = 1f;

            realtimeRoot.transform.rotation =
                Quaternion.LookRotation(Vector3.down, Vector3.forward);

            Lightmapping.Clear();
            Selection.activeGameObject = realtimeRoot;
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Failed to save scene: {scene.path}");
            }

            Debug.Log(
                $"[CreateWholeCeilingRealtimeLight] Created one realtime spot light " +
                $"at ({lightCenter.x:F3}, {lightCenter.y:F3}, {lightCenter.z:F3}) " +
                $"with range {range:F2}, spot angle {spotAngle:F1}, coverage " +
                $"{coverageSize.x:F2}m x {coverageSize.z:F2}m.");
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
