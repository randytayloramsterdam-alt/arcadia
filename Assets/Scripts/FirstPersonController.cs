using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("Footsteps")]
    public AudioClip walkClip;
    [Tooltip("Meters between each footstep.")]
    public float stepDistance = 2f;
    [Tooltip("Input magnitude below this won't trigger steps. Stops tiny nudges from playing sound.")]
    [Range(0f, 0.5f)] public float minInputThreshold = 0.15f;
    [Range(0f, 1f)] public float stepVolume = 0.7f;

    [Header("Intro")]
    public float lyingDuration = 3f;
    public float standUpDuration = 7f;

    private CharacterController controller;
    private Transform cameraTransform;
    private float verticalRotation = 0f;
    private Vector3 velocity;
    private bool canControl = false;
    private bool controlEnabled = true;

    // Footstep state
    private AudioSource[] stepSources;
    private int stepSourceIndex;
    private float distanceSinceStep;

    // Camera local positions: lying on floor vs standing eye height
    private static readonly Vector3 LyingCamPos   = new Vector3(0f, -0.9f, 0f);
    private static readonly Vector3 StandingCamPos = new Vector3(0f,  0.7f, 0f);

    void Awake()
    {
        // Ensure WeaponHolder exists on camera — must be in Awake so
        // InventoryUI can find it during its own Awake.
        var mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<WeaponHolder>() == null)
            mainCam.gameObject.AddComponent<WeaponHolder>();

        // Ensure IntroNarrative exists for the opening text sequence
        if (GetComponent<IntroNarrative>() == null)
            gameObject.AddComponent<IntroNarrative>();

        // Create two alternating AudioSources so footsteps can overlap naturally
        stepSources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("StepSource_" + i);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            stepSources[i] = go.AddComponent<AudioSource>();
            stepSources[i].playOnAwake = false;
            stepSources[i].loop = false;
            stepSources[i].spatialBlend = 1f;
            stepSources[i].volume = stepVolume;
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;

        // Assign clip to both sources (may be set via Inspector after Awake)
        foreach (var src in stepSources)
        {
            src.clip = walkClip;
            src.volume = stepVolume;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initial state: lying down staring at ceiling
        cameraTransform.localPosition = LyingCamPos;
        cameraTransform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        verticalRotation = -90f;

        // If no IntroNarrative is present, start the old intro automatically.
        // Otherwise IntroNarrative will call BeginIntroSequence() when it finishes.
        if (GetComponent<IntroNarrative>() == null)
            StartCoroutine(IntroSequence());
    }

    public void BeginIntroSequence()
    {
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        // Stare at ceiling silently
        yield return new WaitForSeconds(lyingDuration);

        // Stand up animation
        float elapsed = 0f;
        while (elapsed < standUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / standUpDuration));

            cameraTransform.localPosition = Vector3.Lerp(LyingCamPos, StandingCamPos, t);

            float xAngle = Mathf.Lerp(-90f, 0f, t);
            cameraTransform.localRotation = Quaternion.Euler(xAngle, 0f, 0f);
            verticalRotation = xAngle;

            yield return null;
        }

        cameraTransform.localPosition = StandingCamPos;
        cameraTransform.localRotation = Quaternion.identity;
        verticalRotation = 0f;

        canControl = true;
    }

    public void SetControlEnabled(bool enabled)
    {
        controlEnabled = enabled;
    }

    void Update()
    {
        if (!canControl)
            return;

        if (!controlEnabled)
            return;

        // ESC toggles cursor only when player has control (no UI open)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        float inputMag = move.magnitude;
        Vector3 moveDelta = move * moveSpeed * Time.deltaTime;
        controller.Move(moveDelta);

        // Gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // ── Footsteps ──
        if (controller.isGrounded && inputMag > minInputThreshold)
        {
            distanceSinceStep += moveDelta.magnitude;
            if (distanceSinceStep >= stepDistance)
            {
                distanceSinceStep -= stepDistance;
                PlayFootstep();
            }
        }
        else
        {
            // Reset accumulator when stationary so the first step after
            // stopping doesn't fire immediately from leftover distance
            distanceSinceStep = 0f;
        }
    }

    void PlayFootstep()
    {
        if (walkClip == null) return;

        var src = stepSources[stepSourceIndex];
        stepSourceIndex = (stepSourceIndex + 1) % stepSources.Length;

        // Pick a random starting position so each step sounds slightly different
        float maxStart = Mathf.Max(0f, walkClip.length - 0.25f);
        src.time = Random.Range(0f, maxStart);
        src.volume = stepVolume;
        src.Play();
    }
}
