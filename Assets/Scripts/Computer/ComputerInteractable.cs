using UnityEngine;

public class ComputerInteractable : InteractableObject
{
    [Header("Camera Focus")]
    [Tooltip("相机目标的世界位置。将空对象放置在电脑屏幕前方目标位置，拖入此字段。")]
    public Transform screenLookPoint;

    [Tooltip("相机与 screenLookPoint 的距离（米）")]
    public float cameraOffsetDistance = 0.6f;

    [Tooltip("过渡时间（秒）")]
    public float transitionDuration = 0.8f;

    [Tooltip("过渡速度")]
    public float transitionSpeed = 6f;

    [Header("UI")]
    [Tooltip("拖入 ComputerUIController 组件")]
    public ComputerUIController computerUIController;

    private Camera mainCam;
    private FirstPersonController fpsController;
    private InteractionSystem interactionSystem;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isFocused;
    private bool hasOpenedUI;

    void Reset()
    {
        showDescription = false;
    }

    void Start()
    {
        mainCam = Camera.main;
        fpsController = FindObjectOfType<FirstPersonController>();
        interactionSystem = FindObjectOfType<InteractionSystem>();
    }

    void Update()
    {
        if (isFocused && !hasOpenedUI && Input.GetKeyDown(KeyCode.Escape))
            ExitComputerFocus();
    }

    public override void OnStartInteract()
    {
        if (isFocused) return;

        Debug.Log("Computer interacted");

        originalPosition = mainCam.transform.position;
        originalRotation = mainCam.transform.rotation;

        if (fpsController != null)
            fpsController.SetControlEnabled(false);

        isFocused = true;
        hasOpenedUI = false;
        StartCoroutine(TransitionToComputer());
    }

    System.Collections.IEnumerator TransitionToComputer()
    {
        Vector3 targetWorldPos = screenLookPoint.position;
        Quaternion targetWorldRot = screenLookPoint.rotation;

        // 目标位置：从 screenLookPoint 位置向后偏移 cameraOffsetDistance
        Vector3 cameraTargetPos = targetWorldPos - screenLookPoint.forward * cameraOffsetDistance;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, transitionSpeed);

            mainCam.transform.position = Vector3.Lerp(originalPosition, cameraTargetPos, smoothT);
            mainCam.transform.rotation = Quaternion.Slerp(originalRotation, targetWorldRot, smoothT);

            yield return null;
        }

        mainCam.transform.position = cameraTargetPos;
        mainCam.transform.rotation = targetWorldRot;

        hasOpenedUI = true;
        if (computerUIController != null)
        {
            computerUIController.OnClose += ExitComputerFocus;
            computerUIController.Open();
        }
    }

    void ExitComputerFocus()
    {
        if (!isFocused) return;

        if (computerUIController != null)
            computerUIController.OnClose -= ExitComputerFocus;

        isFocused = false;
        hasOpenedUI = false;
        StopAllCoroutines();

        // 通知 InteractionSystem 电脑交互已完成，不再处于 inspect 状态
        if (interactionSystem != null)
            interactionSystem.NotifyComputerInteractDone();

        StartCoroutine(TransitionBack());
    }

    System.Collections.IEnumerator TransitionBack()
    {
        Vector3 currentPos = mainCam.transform.position;
        Quaternion currentRot = mainCam.transform.rotation;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, transitionSpeed);

            mainCam.transform.position = Vector3.Lerp(currentPos, originalPosition, smoothT);
            mainCam.transform.rotation = Quaternion.Slerp(currentRot, originalRotation, smoothT);

            yield return null;
        }

        mainCam.transform.position = originalPosition;
        mainCam.transform.rotation = originalRotation;

        if (fpsController != null)
            fpsController.SetControlEnabled(true);
    }
}