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
    public AudioClip[] walkClips = new AudioClip[3];
    [Tooltip("Per-clip volume, index maps to walkClips array.")]
    public float[] walkClipVolumes = new float[] { 0.7f, 0.7f, 0.7f };
    [Tooltip("Master volume multiplier applied on top of per-clip volume.")]
    [Range(0f, 1f)] public float stepVolume = 0.7f;
    [Tooltip("Meters between each footstep.")]
    public float stepDistance = 2f;
    [Tooltip("Input magnitude below this won't trigger steps. Stops tiny nudges from playing sound.")]
    [Range(0f, 0.5f)] public float minInputThreshold = 0.15f;

    [Header("Camera Shake")]
    [Tooltip("Enable a subtle camera shake on each footstep.")]
    public bool enableCameraShake = true;
    [Tooltip("Shake strength. Higher = more wobble.")]
    [Range(0f, 0.1f)] public float cameraShakeAmplitude = 0.015f;
    [Tooltip("How long the shake lasts (seconds).")]
    [Range(0f, 0.5f)] public float cameraShakeDuration = 0.15f;

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
    private bool wasStepping;

    // Camera shake state
    private Coroutine shakeCoroutine;

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

        // Set master volume on both sources (per-clip volume applied at play time)
        foreach (var src in stepSources)
        {
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

        // Movement (smooth input for responsive feel)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        Vector3 moveDelta = move * moveSpeed * Time.deltaTime;

        // Track position before move so we can measure actual displacement
        Vector3 posBefore = transform.position;
        controller.Move(moveDelta);

        // Gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Actual horizontal displacement this frame (ignores vertical / gravity)
        Vector3 disp = transform.position - posBefore;
        disp.y = 0f;
        float actualMove = disp.magnitude;

        // ── Footsteps ──
        // Use raw input for instant key-release detection (no smoothing decay)
        float rawH = Input.GetAxisRaw("Horizontal");
        float rawV = Input.GetAxisRaw("Vertical");
        bool hasMoveIntent = (rawH * rawH + rawV * rawV) > 0.01f;

        // Require both: player intends to move AND character actually moved
        bool isStepping = controller.isGrounded && hasMoveIntent && actualMove > 0.0001f;

        if (isStepping)
        {
            if (!wasStepping)
            {
                // Just started moving — prime so the first step fires quickly
                distanceSinceStep = stepDistance * 0.75f;
            }

            distanceSinceStep += actualMove;
            if (distanceSinceStep >= stepDistance)
            {
                distanceSinceStep -= stepDistance;
                PlayFootstep();
            }
        }
        else
        {
            if (wasStepping)
            {
                // Just stopped — cut off any lingering footstep audio
                StopFootsteps();
            }
            distanceSinceStep = 0f;
        }

        wasStepping = isStepping;
    }

    void PlayFootstep()
    {
        // Count valid (non-null) clips
        int validCount = 0;
        for (int i = 0; i < walkClips.Length; i++)
            if (walkClips[i] != null) validCount++;

        if (validCount == 0) return;

        // Pick a random valid clip
        int pick = Random.Range(0, validCount);
        int clipIdx = 0;
        int found = 0;
        for (int i = 0; i < walkClips.Length; i++)
        {
            if (walkClips[i] != null)
            {
                if (found == pick) { clipIdx = i; break; }
                found++;
            }
        }

        AudioClip chosen = walkClips[clipIdx];
        var src = stepSources[stepSourceIndex];
        stepSourceIndex = (stepSourceIndex + 1) % stepSources.Length;

        // Random playback start so each step sounds slightly different
        float maxStart = Mathf.Max(0f, chosen.length - 0.25f);
        src.clip = chosen;
        src.time = Random.Range(0f, maxStart);

        // Per-clip volume * master step volume
        float clipVol = (clipIdx < walkClipVolumes.Length) ? walkClipVolumes[clipIdx] : 0.7f;
        src.volume = Mathf.Clamp01(clipVol * stepVolume);
        src.Play();

        // Camera shake
        if (enableCameraShake)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(CameraShakeRoutine());
        }
    }

    void StopFootsteps()
    {
        foreach (var src in stepSources)
        {
            if (src != null && src.isPlaying)
                src.Stop();
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
            // Restore camera position in case shake left it offset
            cameraTransform.localPosition = StandingCamPos;
        }
    }

    IEnumerator CameraShakeRoutine()
    {
        Vector3 basePos = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.deltaTime;
            float damping = 1f - (elapsed / cameraShakeDuration);
            float x = Random.Range(-1f, 1f) * cameraShakeAmplitude * damping;
            float yOff = Random.Range(-1f, 1f) * cameraShakeAmplitude * 0.6f * damping;
            cameraTransform.localPosition = new Vector3(basePos.x + x, basePos.y + yOff, basePos.z);
            yield return null;
        }

        cameraTransform.localPosition = basePos;
        shakeCoroutine = null;
    }
}
