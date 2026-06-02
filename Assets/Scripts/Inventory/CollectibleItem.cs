using UnityEngine;

public class CollectibleItem : InteractableObject
{
    public InventoryItem itemData;
    [Tooltip("Override the item name shown in the inspect panel. Uses itemData.itemName if left blank.")]
    public string itemNameOverride;
    [TextArea(3, 10)]
    [Tooltip("Override description shown in the inspect panel. Falls back to itemData.description if empty.")]
    public string itemDescription;
    [Tooltip("Per-item scale for the 3D preview model. Multiplied with the global modelScale on CollectibleInspectUI. 1 = normal size.")]
    [Range(0.1f, 5f)] public float inspectModelScale = 1f;
    [Tooltip("Per-item initial rotation (Euler angles) for the 3D preview. Added to the global initialRotation on CollectibleInspectUI.")]
    public Vector3 inspectModelRotation = Vector3.zero;
    public AudioClip collectSound;

    private bool isCollected;

    public override void OnStartInteract()
    {
        base.OnStartInteract();

        if (!isCollected && CollectibleInspectUI.Instance != null)
            CollectibleInspectUI.Instance.Show(this);
    }

    public override void OnStopInteract()
    {
        base.OnStopInteract();

        if (!isCollected)
        {
            Collect();
        }

        // Always hide the UI when interaction stops, even if collect failed
        if (CollectibleInspectUI.Instance != null)
            CollectibleInspectUI.Instance.Hide();
    }

    public void Collect()
    {
        if (isCollected) return;

        if (InventoryManager.Instance != null && itemData != null)
        {
            if (InventoryManager.Instance.AddItem(itemData))
            {
                isCollected = true;

                if (collectSound != null)
                    AudioSource.PlayClipAtPoint(collectSound, transform.position);

                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Inventory full!");
            }
        }
    }
}
