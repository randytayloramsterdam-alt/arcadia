using UnityEngine;

public class CollectibleItem : InteractableObject
{
    public InventoryItem itemData;
    public AudioClip collectSound;

    public override void OnStopInteract()
    {
        base.OnStopInteract();

        if (InventoryManager.Instance != null && itemData != null)
        {
            if (InventoryManager.Instance.AddItem(itemData))
            {
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
