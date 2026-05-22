using UnityEngine;

public class OfficePropModule : MonoBehaviour
{
    public int moduleIndex { get; private set; }

    private GameObject[] childObjects;

    void Awake()
    {
        childObjects = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            childObjects[i] = transform.GetChild(i).gameObject;
    }

    public void Initialize(int index)
    {
        moduleIndex = index;
        Debug.Log($"[OfficePropModule] Initialized: moduleIndex={index}, position={transform.position}");
    }

    public void SetVisible(bool visible)
    {
        foreach (var obj in childObjects)
            obj.SetActive(visible);
    }
}