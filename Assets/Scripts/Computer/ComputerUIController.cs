using UnityEngine;
using UnityEngine.UI;

public class ComputerUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("拖入你手动制作的 ComputerUI Canvas（或根节点）")]
    public GameObject computerUICanvas;

    [Tooltip("拖入 Exit 按钮")]
    public Button exitButton;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;

    private AudioSource uiAudioSource;
    private bool isOpen;

    public bool IsOpen => isOpen;

    public System.Action OnClose;

    void Awake()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;

        if (exitButton != null)
            exitButton.onClick.AddListener(Close);

        if (computerUICanvas != null)
            computerUICanvas.SetActive(false);
    }

    void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (computerUICanvas != null)
            computerUICanvas.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlaySound(openSound);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (computerUICanvas != null)
            computerUICanvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlaySound(closeSound);
        OnClose?.Invoke();
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && uiAudioSource != null)
            uiAudioSource.PlayOneShot(clip);
    }
}