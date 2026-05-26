using System.Collections;
using System.Text;
using UnityEngine;

public class OfficePropsLoopManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform modulesRoot;

    [Header("Module Prefabs")]
    public GameObject[] modulePrefabs;

    [Header("Spawn Settings")]
    [Tooltip("第一个模块的生成位置，其他模块沿世界 +X 方向排列")]
    public Vector3 firstModulePosition = Vector3.zero;

    public float moduleLength = 20f;

    [Tooltip("游戏开始时一次性生成多少个模块")]
    public int initialModuleCount = 6;

    [Tooltip("玩家当前所在模块前方，始终至少提前生成多少个模块")]
    public int preSpawnAheadCount = 4;

    public float spawnCheckInterval = 0.5f;

    [Header("Lighting Range")]
    [Tooltip("玩家前方保持开灯的模块数量（distance >= 0 且 <= lightAheadCount 时开灯）")]
    public int lightAheadCount = 3;

    [Tooltip("玩家后方保持开灯的模块数量（distance < 0 且 >= -lightBehindCount 时开灯）")]
    public int lightBehindCount = 1;

    [Header("Visibility Range")]
    [Tooltip("玩家前方保持物体显示的模块数量（distance >= 0 且 <= visibleAheadCount 时显示）")]
    public int visibleAheadCount = 5;

    [Tooltip("玩家后方保持物体显示的模块数量（distance < 0 且 >= -visibleBehindCount 时显示）")]
    public int visibleBehindCount = 2;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    private int highestModuleIndex;
    private int lastPlayerModuleIndex = -1;
    private System.Collections.Generic.Dictionary<int, OfficePropModule> spawnedModules = new System.Collections.Generic.Dictionary<int, OfficePropModule>();

    void Start()
    {
        if (modulesRoot == null)
        {
            var rootObj = new GameObject("ModulesRoot");
            modulesRoot = rootObj.transform;
        }

        highestModuleIndex = -1;
        for (int i = 0; i < initialModuleCount; i++)
            SpawnModule(i);

        CheckAndSpawn();
        UpdateAllModuleStates();
    }

    void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnCheckInterval);
            CheckAndSpawn();
            UpdateAllModuleStates();
        }
    }

    void CheckAndSpawn()
    {
        if (player == null || modulePrefabs == null || modulePrefabs.Length == 0)
            return;

        float playerX = player.position.x;
        int playerModuleIndex = GetModuleIndex(playerX);

        int neededHighestIndex = playerModuleIndex + preSpawnAheadCount;

        while (highestModuleIndex < neededHighestIndex)
        {
            highestModuleIndex++;
            SpawnModule(highestModuleIndex);
        }
    }

    int GetModuleIndex(float worldX)
    {
        if (moduleLength <= 0f) return 0;
        int index = Mathf.FloorToInt((worldX - firstModulePosition.x) / moduleLength);
        return Mathf.Max(0, index);
    }

    void SpawnModule(int moduleIndex)
    {
        if (spawnedModules.ContainsKey(moduleIndex))
            return;

        GameObject prefab = modulePrefabs[Random.Range(0, modulePrefabs.Length)];
        Vector3 position = firstModulePosition + Vector3.right * moduleIndex * moduleLength;
        Quaternion rotation = Quaternion.identity;

        GameObject moduleObj = Instantiate(prefab, position, rotation, modulesRoot);
        var module = moduleObj.GetComponent<OfficePropModule>();
        if (module != null)
        {
            module.Initialize(moduleIndex);
            module.enableDebugLogs = enableDebugLogs;
        }

        if (enableDebugLogs)
            Debug.Log($"[OfficePropsLoopManager] Spawn: moduleIndex={moduleIndex}, prefab={prefab.name}, finalPosition={position}");

        if (module != null)
        {
            module.SetLightsActive(false);
            module.SetContentVisible(false);
        }

        spawnedModules.Add(moduleIndex, module);
    }

    void UpdateAllModuleStates()
    {
        if (player == null)
            return;

        int currentPlayerModuleIndex = GetModuleIndex(player.position.x);

        bool playerChangedModule = currentPlayerModuleIndex != lastPlayerModuleIndex;
        if (playerChangedModule)
        {
            lastPlayerModuleIndex = currentPlayerModuleIndex;
            if (enableDebugLogs)
                Debug.Log($"[OfficePropsLoopManager] Player entered moduleIndex={currentPlayerModuleIndex}");
        }

        var lightsOnBuilder = new StringBuilder();
        var visibleBuilder = new StringBuilder();

        foreach (var kvp in spawnedModules)
        {
            int moduleIndex = kvp.Key;
            OfficePropModule module = kvp.Value;

            int distance = moduleIndex - currentPlayerModuleIndex;

            // 灯光
            bool shouldLightOn = distance >= -lightBehindCount && distance <= lightAheadCount;
            module.SetLightsActive(shouldLightOn);
            if (shouldLightOn)
                lightsOnBuilder.Append($"m{moduleIndex}, ");

            // 物体显示
            bool shouldBeVisible = distance >= -visibleBehindCount && distance <= visibleAheadCount;
            module.SetContentVisible(shouldBeVisible);
            if (shouldBeVisible)
                visibleBuilder.Append($"m{moduleIndex}, ");
        }

        string lightsOnStr = lightsOnBuilder.ToString().TrimEnd(',', ' ');
        string visibleStr = visibleBuilder.ToString().TrimEnd(',', ' ');

        if (playerChangedModule)
        {
            if (enableDebugLogs)
            {
                if (lightsOnStr.Length > 0)
                    Debug.Log($"[OfficePropsLoopManager] Lights ON (m{currentPlayerModuleIndex}): {lightsOnStr}");
                else
                    Debug.Log($"[OfficePropsLoopManager] Player at m{currentPlayerModuleIndex}. All lights OFF.");

                if (visibleStr.Length > 0)
                    Debug.Log($"[OfficePropsLoopManager] Content VISIBLE (m{currentPlayerModuleIndex}): {visibleStr}");
                else
                    Debug.Log($"[OfficePropsLoopManager] Player at m{currentPlayerModuleIndex}. All content HIDDEN.");
            }
        }
    }
}