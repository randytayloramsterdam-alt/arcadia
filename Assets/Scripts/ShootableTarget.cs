using UnityEngine;

public class ShootableTarget : MonoBehaviour
{
    public float health = 100f;
    public GameObject destroyEffect;

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        health -= damage;

        if (health <= 0f)
        {
            if (destroyEffect != null)
                Instantiate(destroyEffect, hitPoint, Quaternion.LookRotation(hitNormal));

            Destroy(gameObject);
        }
    }
}
