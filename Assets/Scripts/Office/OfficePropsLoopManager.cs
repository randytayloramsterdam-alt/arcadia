using System.Collections;
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

    [Tooltip("玩家当前所在模块前方，始终至少提前生成多少个模块。例如玩家在 moduleIndex=3，preSpawnAheadCount=4，则至少生成到 moduleIndex=7")]
    public int preSpawnAheadCount = 4;

    public float spawnCheckInterval = 0.5f;

    private int highestModuleIndex;
    private System.Collections.Generic.Dictionary<int, GameObject> spawnedModules = new System.Collections.Generic.Dictionary<int, GameObject>();

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
        }
    }

    void CheckAndSpawn()
    {
        if (player == null || modulePrefabs == null || modulePrefabs.Length == 0)
            return;

        float playerX = player.position.x;
        int playerModuleIndex = GetModuleIndex(playerX);

        // 玩家前方需要存在的最大 moduleIndex
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
            module.Initialize(moduleIndex);

        Debug.Log($"[OfficePropsLoopManager] Spawn: moduleIndex={moduleIndex}, prefab={prefab.name}, finalPosition={position}");
        spawnedModules.Add(moduleIndex, moduleObj);
    }
}