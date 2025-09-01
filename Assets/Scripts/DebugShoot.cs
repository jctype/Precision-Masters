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
            ballisticsManager.FireBullet(direction);
        }
    }
}
