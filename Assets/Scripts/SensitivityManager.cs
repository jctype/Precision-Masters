using UnityEngine;
using StarterAssets;

public class SensitivityManager : MonoBehaviour
{
    public FirstPersonController playerController; // Reference to the FPS controller
    public float scopedSensitivity = 0.5f; // Much lower sensitivity for scoped aim
    private float defaultSensitivity; // To store the normal sensitivity

    void Start()
    {
        // Get the reference to the FirstPersonController script
        if (playerController == null)
        {
            playerController = GetComponent<FirstPersonController>();
        }
        // Store the original sensitivity to go back to later
        if (playerController != null)
        {
            defaultSensitivity = playerController.RotationSpeed;
        }
    }

    void Update()
    {
        if (playerController == null) return;

        // When right mouse button is held DOWN, reduce sensitivity
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            playerController.RotationSpeed = scopedSensitivity;
        }

        // When right mouse button is released, reset sensitivity to default
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            playerController.RotationSpeed = defaultSensitivity;
        }
    }
}