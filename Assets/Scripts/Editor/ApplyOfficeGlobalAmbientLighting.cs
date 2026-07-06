using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Arcadia.EditorTools
{
    internal static class ApplyOfficeGlobalAmbientLighting
    {
        private const string RequestFilePath =
            "Temp/CodexApplyOfficeGlobalAmbientLighting.request";
        private const string ResultFilePath =
            "Temp/CodexApplyOfficeGlobalAmbientLighting.result.txt";
        private const string AreaArrayRootName = "Ceiling_Area_Light_Array";
        private const string RealtimeRootName = "Ceiling_Realtime_Light";
        private const string SparseRootName = "Ceiling_Sparse_Realtime_Light_Array";
        private const string GlobalLightName = "Global_Fill_Light";

        private static bool requestQueued;
        private static bool updateHookInstalled;

        [InitializeOnLoadMethod]
        private static void QueueRequestedApply()
        {
            if (!updateHookInstalled)
            {
                EditorApplication.update += WatchForRequestedApply;
                updateHookInstalled = true;
            }

            WatchForRequestedApply();
        }

        private static void WatchForRequestedApply()
        {
            if (requestQueued ||
                !File.Exists(RequestFilePath) ||
                EditorApplication.isCompiling)
            {
                return;
            }

            requestQueued = true;
            EditorApplication.delayCall += RunRequestedApply;
        }

        [MenuItem("Tools/Office/Apply Global Ambient Lighting")]
        private static void ApplyToActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                throw new InvalidOperationException(
                    "Open a saved scene before applying global ambient lighting.");
            }

            Undo.SetCurrentGroupName("Apply office global ambient lighting");
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

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.64f, 1f);
            RenderSettings.ambientIntensity = 2f;
            RenderSettings.reflectionIntensity = 1.2f;
            RenderSettings.fog = false;

            Light globalLight = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Light>(true))
                .FirstOrDefault(light =>
                    light.type == LightType.Directional ||
                    light.gameObject.name == "Light" ||
                    light.gameObject.name == GlobalLightName);

            if (globalLight == null)
            {
                GameObject lightObject = new GameObject(GlobalLightName);
                Undo.RegisterCreatedObjectUndo(
                    lightObject,
                    "Create global fill light");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                lightObject.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.Euler(50f, -30f, 0f));
                globalLight = lightObject.AddComponent<Light>();
            }

            globalLight.gameObject.name = GlobalLightName;
            globalLight.type = LightType.Directional;
            globalLight.lightmapBakeType = LightmapBakeType.Realtime;
            globalLight.color = Color.white;
            globalLight.intensity = 1.35f;
            globalLight.shadows = LightShadows.None;
            globalLight.renderMode = LightRenderMode.ForcePixel;
            globalLight.useColorTemperature = true;
            globalLight.colorTemperature = 5200f;
            globalLight.bounceIntensity = 1f;
            globalLight.cullingMask = -1;
            globalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.Cancel();
            Lightmapping.Clear();
            DynamicGI.UpdateEnvironment();

            Selection.activeGameObject = globalLight.gameObject;
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Failed to save scene: {scene.path}");
            }

            Debug.Log(
                "[ApplyOfficeGlobalAmbientLighting] Removed ceiling lights, " +
                "enabled bright flat ambient lighting, and configured one " +
                "realtime directional fill light.");
        }

        private static void RunRequestedApply()
        {
            try
            {
                if (File.Exists(RequestFilePath))
                {
                    File.Delete(RequestFilePath);
                }

                ApplyToActiveScene();
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
