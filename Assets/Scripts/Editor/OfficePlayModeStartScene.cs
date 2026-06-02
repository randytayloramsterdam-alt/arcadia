using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class OfficePlayModeStartScene
{
    private const string OfficeScenePath = "Assets/Scenes/Office_text.unity";

    static OfficePlayModeStartScene()
    {
        EditorApplication.delayCall += UseOfficeScene;
    }

    [MenuItem("Tools/Office/Use Office_text As Play Start Scene")]
    public static void UseOfficeScene()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(OfficeScenePath);
        if (sceneAsset == null)
        {
            UnityEngine.Debug.LogWarning($"Could not find scene at {OfficeScenePath}");
            return;
        }

        EditorSceneManager.playModeStartScene = sceneAsset;
    }

    [MenuItem("Tools/Office/Clear Play Start Scene")]
    public static void ClearPlayStartScene()
    {
        EditorSceneManager.playModeStartScene = null;
    }
}
