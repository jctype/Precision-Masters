using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private ParticleSystem missEffect;

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit a target
        if (other.CompareTag("Target"))
        {
            Debug.Log("Bullet hit TARGET!");
            if (hitEffect != null)
                Instantiate(hitEffect, transform.position, Quaternion.identity);

            // Optional: make the target react
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
                rb.AddForceAtPosition(transform.forward * 50f, transform.position);

            Destroy(gameObject);
        }
        else if (other.CompareTag("Terrain"))
        {
            Debug.Log("Bullet hit TERRAIN!");
            if (missEffect != null)
                Instantiate(missEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
