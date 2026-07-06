using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcadia.EditorTools
{
    internal static class BakeOfficeLighting
    {
        // Script reload is also used as a reliable trigger for Codex bake requests.
        private const string RequestFilePath =
            "Temp/CodexOfficeLightingBake.request";
        private const string ResultFilePath =
            "Temp/CodexOfficeLightingBake.result.txt";

        private static bool requestQueued;
        private static bool updateHookInstalled;

        [InitializeOnLoadMethod]
        private static void QueueRequestedBake()
        {
            if (!updateHookInstalled)
            {
                EditorApplication.update += WatchForRequestedBake;
                updateHookInstalled = true;
            }

            WatchForRequestedBake();
        }

        private static void WatchForRequestedBake()
        {
            if (requestQueued || !File.Exists(RequestFilePath) || EditorApplication.isCompiling)
            {
                return;
            }

            requestQueued = true;
            EditorApplication.delayCall += RunRequestedBake;
        }

        [MenuItem("Tools/Office/Prepare Lighting Preview")]
        private static void PrepareLightingPreview()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                Debug.LogWarning(
                "[BakeOfficeLighting] No active Scene view was found to prepare.");
                return;
            }

            sceneView.sceneLighting = true;
            sceneView.drawGizmos = false;
            sceneView.Repaint();

            Debug.Log(
                "[BakeOfficeLighting] Scene lighting preview enabled and gizmos hidden.");
        }

        [MenuItem("Tools/Office/Bake Lighting For Active Scene")]
        private static void BakeLightingForActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                throw new InvalidOperationException(
                    "Open a saved scene before baking lighting.");
            }

            if (Lightmapping.isRunning)
            {
                throw new InvalidOperationException(
                    "Lightmapping is already running.");
            }

            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.Clear();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Failed to save scene before baking: {scene.path}");
            }

            DateTime startedAt = DateTime.Now;
            bool baked = Lightmapping.Bake();
            if (!baked)
            {
                throw new InvalidOperationException(
                    $"Lighting bake failed for scene: {scene.path}");
            }

            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Failed to save scene after baking: {scene.path}");
            }

            Debug.Log(
                $"[BakeOfficeLighting] Lighting bake completed for '{scene.path}' " +
                $"in {(DateTime.Now - startedAt).TotalSeconds:F1}s.");
        }

        private static void RunRequestedBake()
        {
            try
            {
                if (File.Exists(RequestFilePath))
                {
                    File.Delete(RequestFilePath);
                }

                PrepareLightingPreview();
                BakeLightingForActiveScene();
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
