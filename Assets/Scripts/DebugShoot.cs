using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugShoot : MonoBehaviour
{
    private BallisticsManager ballisticsManager;

    void Start()
    {
        ballisticsManager = GetComponent<BallisticsManager>();
        if (ballisticsManager == null)
        {
            Debug.LogError("BallisticsManager not found on this object!");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Fire in the direction the camera is facing
            Vector3 direction = Camera.main.transform.forward;
            Vector3 startPos = transform.position; // Assuming the start position is the current position of the DebugShoot object
            Vector3 startVel = direction * 10f; // Assuming the start velocity is 10 units in the direction of the camera

            ballisticsManager.FireBullet(startPos, startVel);
        }
    }
}
