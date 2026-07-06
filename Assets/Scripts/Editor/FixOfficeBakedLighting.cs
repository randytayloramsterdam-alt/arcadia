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
    internal static class FixOfficeBakedLighting
    {
        private const string OfficeRootName = "office 1";
        private const string ComputerRootName = "Computer_And_UI_Replacement";
        private const string RequestFilePath =
            "Temp/CodexOfficeLightingFix.request";
        private const string ResultFilePath =
            "Temp/CodexOfficeLightingFix.result.txt";

        private static bool requestQueued;

        [InitializeOnLoadMethod]
        private static void QueueRequestedFix()
        {
            if (requestQueued || !File.Exists(RequestFilePath))
            {
                return;
            }

            requestQueued = true;
            EditorApplication.delayCall += RunRequestedFix;
        }

        [MenuItem("Tools/Office/Fix Office Baked Lighting")]
        private static void FixOfficeBakedLightingForScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                throw new InvalidOperationException(
                    "Open a saved scene before fixing baked lighting.");
            }

            GameObject officeRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == OfficeRootName);
            if (officeRoot == null)
            {
                throw new InvalidOperationException(
                    $"Scene root '{OfficeRootName}' was not found.");
            }

            GameObject computerRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == ComputerRootName);

            var rendererObjects = new HashSet<GameObject>();
            var renderers = new List<Renderer>();

            CollectRenderers(officeRoot, renderers, rendererObjects);
            if (computerRoot != null)
            {
                CollectRenderers(computerRoot, renderers, rendererObjects);
            }

            int updatedObjects = 0;
            int updatedRenderers = 0;
            foreach (GameObject gameObject in rendererObjects)
            {
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(gameObject);
                StaticEditorFlags requiredFlags =
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic;
                if ((flags & requiredFlags) != requiredFlags)
                {
                    GameObjectUtility.SetStaticEditorFlags(
                        gameObject, flags | requiredFlags);
                    updatedObjects++;
                }
            }

            foreach (Renderer renderer in renderers)
            {
                EditorUtility.SetDirty(renderer);
                updatedRenderers++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Failed to save scene: {scene.path}");
            }

            File.WriteAllText(
                ResultFilePath,
                $"UPDATED objects={updatedObjects} renderers={updatedRenderers} totalRenderers={renderers.Count}{Environment.NewLine}");

            Debug.Log(
                $"[FixOfficeBakedLighting] Updated {updatedObjects} objects and " +
                $"{updatedRenderers} renderers for baked GI. Total renderers: {renderers.Count}.");
        }

        private static void RunRequestedFix()
        {
            try
            {
                if (File.Exists(RequestFilePath))
                {
                    File.Delete(RequestFilePath);
                }

                FixOfficeBakedLightingForScene();
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

        private static void CollectRenderers(
            GameObject root,
            ICollection<Renderer> renderers,
            ISet<GameObject> rendererObjects)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                {
                    continue;
                }

                renderers.Add(renderer);
                rendererObjects.Add(renderer.gameObject);
            }
        }
    }
}
