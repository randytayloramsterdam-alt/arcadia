using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EndlessOfficeSceneBuilder
{
    const string ScenePath = "Assets/Scenes/InfiniteOfficeScene.unity";

    [MenuItem("Tools/Endless Office/Create Endless Office Scene")]
    public static void CreateEndlessOfficeScene()
    {
        BuildAndSaveScene();
    }

    public static void BuildAndSaveScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "InfiniteOfficeScene";

        GameObject player = CreatePlayer();
        CreateOffice(player.transform);
        ApplyRenderSettings();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Selection.activeGameObject = player;
    }

    static GameObject CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0.9f, 1.5f);
        player.transform.rotation = Quaternion.identity;

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.32f;
        characterController.center = Vector3.zero;

        FirstPersonController firstPerson = player.AddComponent<FirstPersonController>();
        firstPerson.moveSpeed = 2.9f;
        firstPerson.mouseSensitivity = 2f;
        firstPerson.lyingDuration = 0.7f;
        firstPerson.standUpDuration = 2.4f;

        IntroNarrative intro = player.AddComponent<IntroNarrative>();
        intro.sentences = new[]
        {
            "\u6211\u597d\u50cf\u5728\u529e\u516c\u5ba4\u7684\u5730\u677f\u4e0a\u9192\u6765\u3002",
            "\u706f\u7ba1\u4e00\u76f4\u5728\u54cd\u3002",
            "\u8fd9\u91cc\u6bd4\u6211\u8bb0\u5fc6\u91cc\u8981\u957f\u5f97\u591a\u3002"
        };
        intro.revealSubtitle = "\u4e00\u6392\u6392\u529e\u516c\u684c\u6d88\u5931\u5728\u96fe\u91cc\uff0c\u50cf\u6c38\u8fdc\u4e0d\u4f1a\u5230\u5934\u3002";

        player.AddComponent<InteractionSystem>();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 72f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 360f;
        camera.backgroundColor = new Color(0.04f, 0.035f, 0.025f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        AudioListener listener = cameraObject.AddComponent<AudioListener>();
        listener.enabled = true;

        PS2Renderer ps2 = cameraObject.AddComponent<PS2Renderer>();
        ps2.renderWidth = 900;
        ps2.renderHeight = 480;
        ps2.enableSSAO = false;
        ps2.disableFog = false;
        ps2.ambientColor = new Color(0.18f, 0.16f, 0.1f);
        ps2.ambientIntensity = 0.35f;

        return player;
    }

    static void CreateOffice(Transform player)
    {
        GameObject generatorObject = new GameObject("Endless Office Generator");
        EndlessOfficeGenerator generator = generatorObject.AddComponent<EndlessOfficeGenerator>();
        generator.player = player;
        generator.chunkLength = 18f;
        generator.chunksAhead = 18;
        generator.chunksBehind = 3;
        generator.officeWidth = 34f;
        generator.ceilingHeight = 3.15f;
        generator.deskColumns = 6;
        generator.deskRowsPerChunk = 3;
        generator.fogDensity = 0.014f;
        generator.glowingScreenChance = 0.23f;
        generator.RebuildPreview();

        GameObject startMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startMarker.name = "low starting carpet";
        startMarker.transform.position = new Vector3(0f, 0.012f, 1.3f);
        startMarker.transform.localScale = new Vector3(2.4f, 0.025f, 1.6f);
        startMarker.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial("dull brown starting carpet", new Color(0.18f, 0.09f, 0.055f));
    }

    static void ApplyRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.46f, 0.42f, 0.25f, 1f);
        RenderSettings.fogDensity = 0.014f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.16f, 0.1f);
        RenderSettings.ambientIntensity = 0.35f;
        RenderSettings.reflectionIntensity = 0.18f;
    }

    static Material MakeMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        return material;
    }
}
