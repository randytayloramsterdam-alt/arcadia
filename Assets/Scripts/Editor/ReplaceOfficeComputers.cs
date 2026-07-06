using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcadia.EditorTools
{
    internal static class ReplaceOfficeComputers
    {
        private const string PrefabPath = "Assets/prefeb/Computer_And_UI.prefab";
        private const string OfficeRootName = "office 1";
        private const string ReplacementRootName = "Computer_And_UI_Replacements";

        private static readonly Regex ComputerNamePattern =
            new Regex(@"^电脑(?:\.\d{3})?$", RegexOptions.Compiled);

        [MenuItem("Tools/Office/Replace All Computers With Computer_And_UI")]
        private static void ReplaceAll()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("No loaded active scene was found.");
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Prefab was not found: {PrefabPath}");
            }

            GameObject officeRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == OfficeRootName);
            if (officeRoot == null)
            {
                throw new InvalidOperationException(
                    $"Scene root '{OfficeRootName}' was not found in {scene.path}.");
            }

            if (scene.GetRootGameObjects().Any(root => root.name == ReplacementRootName))
            {
                throw new InvalidOperationException(
                    $"'{ReplacementRootName}' already exists. Replacement was not run twice.");
            }

            Transform[] targets = officeRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform =>
                    transform.gameObject.activeSelf &&
                    ComputerNamePattern.IsMatch(transform.name))
                .OrderBy(transform => transform.name, StringComparer.Ordinal)
                .ToArray();

            if (targets.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No computer objects were found below '{OfficeRootName}'.");
            }

            Undo.SetCurrentGroupName("Replace office computers");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject replacementRoot = new GameObject(ReplacementRootName);
            Undo.RegisterCreatedObjectUndo(replacementRoot, "Create computer replacement root");
            SceneManager.MoveGameObjectToScene(replacementRoot, scene);
            replacementRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            replacementRoot.transform.localScale = Vector3.one;

            int replacedCount = 0;
            foreach (Transform target in targets)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to instantiate {PrefabPath} for {target.name}.");
                }

                Undo.RegisterCreatedObjectUndo(instance, $"Replace {target.name}");
                instance.name = $"Computer_And_UI_{replacedCount + 1:000}";
                instance.transform.SetParent(replacementRoot.transform, false);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;

                Transform computerModel = instance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == "Computer");
                if (computerModel == null)
                {
                    throw new InvalidOperationException(
                        $"The prefab instance for {target.name} has no 'Computer' child.");
                }

                Quaternion modelRotationFromRoot =
                    Quaternion.Inverse(instance.transform.rotation) * computerModel.rotation;
                instance.transform.rotation =
                    target.rotation * Quaternion.Inverse(modelRotationFromRoot);
                instance.transform.position += target.position - computerModel.position;

                Undo.RecordObject(target.gameObject, $"Disable {target.name}");
                target.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target.gameObject);
                replacedCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = replacementRoot;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save scene: {scene.path}");
            }

            Debug.Log(
                $"[ReplaceOfficeComputers] Replaced {replacedCount} computers in " +
                $"'{scene.path}' with '{PrefabPath}'.");
        }

        [MenuItem("Tools/Office/Validate Computer Replacements")]
        private static void ValidateReplacements()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject officeRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == OfficeRootName);
            GameObject replacementRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == ReplacementRootName);
            if (officeRoot == null || replacementRoot == null)
            {
                throw new InvalidOperationException(
                    "The office root or replacement root is missing.");
            }

            Transform[] targets = officeRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => ComputerNamePattern.IsMatch(transform.name))
                .OrderBy(transform => transform.name, StringComparer.Ordinal)
                .ToArray();
            Transform[] instances = replacementRoot.transform.Cast<Transform>()
                .OrderBy(transform => transform.name, StringComparer.Ordinal)
                .ToArray();

            if (targets.Length != instances.Length)
            {
                throw new InvalidOperationException(
                    $"Target count {targets.Length} does not match instance count {instances.Length}.");
            }

            float maximumPositionError = 0f;
            float maximumRotationError = 0f;
            float maximumBoundsCenterError = 0f;
            float maximumBoundsBottomError = 0f;
            int activeOriginals = 0;
            for (int index = 0; index < targets.Length; index++)
            {
                Transform computerModel = instances[index].GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == "Computer");
                if (computerModel == null)
                {
                    throw new InvalidOperationException(
                        $"{instances[index].name} has no 'Computer' child.");
                }

                maximumPositionError = Mathf.Max(
                    maximumPositionError,
                    Vector3.Distance(targets[index].position, computerModel.position));
                maximumRotationError = Mathf.Max(
                    maximumRotationError,
                    Quaternion.Angle(targets[index].rotation, computerModel.rotation));
                Bounds targetBounds = GetCombinedRendererBounds(targets[index]);
                Bounds replacementBounds = GetCombinedRendererBounds(computerModel);
                maximumBoundsCenterError = Mathf.Max(
                    maximumBoundsCenterError,
                    Vector2.Distance(
                        new Vector2(targetBounds.center.x, targetBounds.center.z),
                        new Vector2(replacementBounds.center.x, replacementBounds.center.z)));
                maximumBoundsBottomError = Mathf.Max(
                    maximumBoundsBottomError,
                    Mathf.Abs(targetBounds.min.y - replacementBounds.min.y));
                if (targets[index].gameObject.activeSelf)
                {
                    activeOriginals++;
                }
            }

            Debug.Log(
                $"[ReplaceOfficeComputers] Validation: {instances.Length} prefab instances, " +
                $"{activeOriginals} active originals, maximum position error " +
                $"{maximumPositionError:F6}, maximum rotation error {maximumRotationError:F6} degrees, " +
                $"maximum horizontal bounds-center error {maximumBoundsCenterError:F6}, " +
                $"maximum bounds-bottom error {maximumBoundsBottomError:F6}.");
        }

        [MenuItem("Tools/Office/Fix Existing Computer Alignment")]
        private static void FixExistingAlignment()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject officeRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == OfficeRootName);
            GameObject replacementRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == ReplacementRootName);
            if (officeRoot == null || replacementRoot == null)
            {
                throw new InvalidOperationException(
                    "The office root or replacement root is missing.");
            }

            Transform[] targets = officeRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => ComputerNamePattern.IsMatch(transform.name))
                .OrderBy(transform => transform.name, StringComparer.Ordinal)
                .ToArray();
            Transform[] instances = replacementRoot.transform.Cast<Transform>()
                .OrderBy(transform => transform.name, StringComparer.Ordinal)
                .ToArray();
            if (targets.Length != instances.Length)
            {
                throw new InvalidOperationException(
                    $"Target count {targets.Length} does not match instance count {instances.Length}.");
            }

            Undo.SetCurrentGroupName("Fix office computer alignment");
            int undoGroup = Undo.GetCurrentGroup();
            float smallestScale = float.MaxValue;
            float largestScale = 0f;

            for (int index = 0; index < targets.Length; index++)
            {
                Transform target = targets[index];
                Transform instance = instances[index];
                Transform computerModel = instance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == "Computer");
                if (computerModel == null)
                {
                    throw new InvalidOperationException(
                        $"{instance.name} has no 'Computer' child.");
                }

                Undo.RecordObject(instance, $"Align {instance.name}");
                instance.localScale = Vector3.one;

                Quaternion modelRotationFromRoot =
                    Quaternion.Inverse(instance.rotation) * computerModel.rotation;
                instance.rotation = target.rotation * Quaternion.Inverse(modelRotationFromRoot);

                Bounds targetBounds = GetCombinedRendererBounds(target);
                Bounds replacementBounds = GetCombinedRendererBounds(computerModel);
                float scale = CalculateUniformScale(targetBounds.size, replacementBounds.size);
                instance.localScale = Vector3.one * scale;

                replacementBounds = GetCombinedRendererBounds(computerModel);
                Vector3 boundsCorrection = new Vector3(
                    targetBounds.center.x - replacementBounds.center.x,
                    targetBounds.min.y - replacementBounds.min.y,
                    targetBounds.center.z - replacementBounds.center.z);
                instance.position += boundsCorrection;

                smallestScale = Mathf.Min(smallestScale, scale);
                largestScale = Mathf.Max(largestScale, scale);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save scene: {scene.path}");
            }

            Debug.Log(
                $"[ReplaceOfficeComputers] Corrected {instances.Length} prefab instances using " +
                $"renderer bounds. Uniform scale range: {smallestScale:F6} to {largestScale:F6}.");
        }

        [MenuItem("Tools/Office/Replicate Four Correct Computers")]
        private static void ReplicateFourCorrectComputers()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject officeRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == OfficeRootName);
            GameObject replacementRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == ReplacementRootName);
            if (prefab == null || officeRoot == null || replacementRoot == null)
            {
                throw new InvalidOperationException(
                    "The prefab, office root, or replacement root is missing.");
            }

            Transform[] targets = officeRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => ComputerNamePattern.IsMatch(transform.name))
                .OrderBy(transform => transform.name, StringComparer.Ordinal)
                .ToArray();
            Transform[] templates = replacementRoot.transform.Cast<Transform>().ToArray();
            if (templates.Length != 4)
            {
                throw new InvalidOperationException(
                    $"Expected exactly four manually corrected templates, found {templates.Length}.");
            }

            Transform[] sourceTargets = new Transform[templates.Length];
            var usedTargets = new System.Collections.Generic.HashSet<Transform>();
            for (int templateIndex = 0; templateIndex < templates.Length; templateIndex++)
            {
                Transform templateModel = templates[templateIndex]
                    .GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == "Computer");
                if (templateModel == null)
                {
                    throw new InvalidOperationException(
                        $"{templates[templateIndex].name} has no 'Computer' child.");
                }

                Vector3 templateCenter = GetCombinedRendererBounds(templateModel).center;
                Transform sourceTarget = targets
                    .Where(target => !usedTargets.Contains(target))
                    .OrderBy(target =>
                        (GetCombinedRendererBounds(target).center - templateCenter).sqrMagnitude)
                    .First();
                sourceTargets[templateIndex] = sourceTarget;
                usedTargets.Add(sourceTarget);
            }

            Undo.SetCurrentGroupName("Replicate four corrected office computers");
            int undoGroup = Undo.GetCurrentGroup();
            var templateUsageCounts = new int[templates.Length];

            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                Transform target = targets[targetIndex];
                int templateIndex = Array.FindIndex(
                    sourceTargets,
                    sourceTarget => sourceTarget == target);
                Transform instance;

                if (templateIndex >= 0)
                {
                    instance = templates[templateIndex];
                    Undo.RecordObject(instance.gameObject, $"Rename {instance.name}");
                    Undo.RecordObject(instance, $"Preserve {instance.name}");
                }
                else
                {
                    templateIndex = Enumerable.Range(0, templates.Length)
                        .OrderBy(index =>
                            Quaternion.Angle(target.rotation, sourceTargets[index].rotation))
                        .First();

                    GameObject clone = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                    if (clone == null)
                    {
                        throw new InvalidOperationException(
                            $"Failed to instantiate {PrefabPath} for {target.name}.");
                    }

                    Undo.RegisterCreatedObjectUndo(clone, $"Copy computer for {target.name}");
                    instance = clone.transform;
                    instance.SetParent(replacementRoot.transform, false);

                    Transform template = templates[templateIndex];
                    Transform sourceTarget = sourceTargets[templateIndex];
                    Quaternion targetDelta =
                        target.rotation * Quaternion.Inverse(sourceTarget.rotation);
                    instance.position =
                        target.position + targetDelta * (template.position - sourceTarget.position);
                    instance.rotation = targetDelta * template.rotation;
                    instance.localScale = template.localScale;
                }

                instance.name = $"Computer_And_UI_{targetIndex + 1:000}";
                templateUsageCounts[templateIndex]++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = replacementRoot;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save scene: {scene.path}");
            }

            string[] calibrationDetails = Enumerable.Range(0, templates.Length)
                .Select(index =>
                    $"{sourceTargets[index].name}: yaw {templates[index].eulerAngles.y:F1}°, " +
                    $"{templateUsageCounts[index]} copies")
                .ToArray();
            Debug.Log(
                $"[ReplaceOfficeComputers] Replicated four manually corrected templates to " +
                $"{targets.Length} positions. {string.Join("; ", calibrationDetails)}.");
        }

        private static Bounds GetCombinedRendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !(renderer is ParticleSystemRenderer))
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"{root.name} contains no renderers.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static float CalculateUniformScale(Vector3 targetSize, Vector3 sourceSize)
        {
            float[] targetAxes = { targetSize.x, targetSize.y, targetSize.z };
            float[] sourceAxes = { sourceSize.x, sourceSize.y, sourceSize.z };
            Array.Sort(targetAxes);
            Array.Sort(sourceAxes);

            float[] ratios = new float[3];
            for (int index = 0; index < ratios.Length; index++)
            {
                if (sourceAxes[index] <= 0.000001f)
                {
                    throw new InvalidOperationException("Replacement renderer bounds have zero size.");
                }

                ratios[index] = targetAxes[index] / sourceAxes[index];
            }

            Array.Sort(ratios);
            return ratios[1];
        }
    }
}
