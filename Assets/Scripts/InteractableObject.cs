using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [TextArea]
    public string description = "A very old radio, nothing special.";

    [Tooltip("交互时是否在屏幕下方显示 description 文本")]
    public bool showDescription = true;

    public float interactDistance = 3f;

    [Tooltip("Where the white dot appears on screen. Uses transform.position if empty.")]
    public Transform interactionPoint;

    /// <summary>Returns the world-space point where the interaction dot should appear.</summary>
    public Vector3 GetInteractionPoint()
    {
        return interactionPoint != null ? interactionPoint.position : transform.position;
    }

    public virtual void OnStartInteract() { }
    public virtual void OnStopInteract() { }
}
