using UnityEngine;

public class ComputerMailNotification : MonoBehaviour
{
    [Header("Audio")]
    public bool enableNewMailSound = true;
    public AudioSource audioSource;
    public AudioClip newMailClip;
    [Range(0f, 1f)] public float newMailVolume = 0.8f;

    public void PlayNewMailNotification()
    {
        if (!enableNewMailSound)
            return;
        if (newMailClip == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(newMailClip, newMailVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(newMailClip, Vector3.zero, newMailVolume);
        }
    }
}