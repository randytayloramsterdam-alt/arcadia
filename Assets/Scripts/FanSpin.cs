using UnityEngine;

public class FanSpin : MonoBehaviour
{
    [Tooltip("Degrees per second.")]
    public float speed = 720f;

    [Tooltip("Local axis to spin around.")]
    public Vector3 axis = Vector3.forward;

    [Tooltip("Use Space.Self (local) or Space.World.")]
    public Space space = Space.Self;

    void Update()
    {
        transform.Rotate(axis, speed * Time.deltaTime, space);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw the rotation axis so you can see what you're rotating around
        Vector3 origin = transform.position;
        Vector3 dir = space == Space.Self
            ? transform.TransformDirection(axis.normalized)
            : axis.normalized;

        float len = 0.8f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin - dir * len, origin + dir * len);
        Gizmos.DrawSphere(origin + dir * len, 0.05f);

        // Also draw local axes for reference
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, transform.right * 0.3f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, transform.up * 0.3f);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(origin, transform.forward * 0.3f);
    }
#endif
}
