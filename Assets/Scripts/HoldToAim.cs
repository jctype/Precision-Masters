using UnityEngine;

public class HoldToAim : MonoBehaviour
{
    public GameObject scopeOverlay; // The ScopeCanvas
    public Camera playerCamera;     // The Main Camera
    public float zoomedFOV = 5.0f; // The zoomed FOV

    private float defaultFOV; // To store the normal FOV

    void Start()
    {
        // Ensure the scope is hidden when the game starts
        if (scopeOverlay != null)
        {
            scopeOverlay.SetActive(false);
        }

        // Get the default FOV from the camera
        if (playerCamera != null)
        {
            defaultFOV = playerCamera.fieldOfView;
        }
    }

    void Update()
    {
        if (scopeOverlay == null || playerCamera == null) return;

        if (Input.GetKey(KeyCode.Mouse1)) // Hold to aim
        {
            scopeOverlay.SetActive(true);
            playerCamera.fieldOfView = zoomedFOV; // ZOOM IN
        }
        else
        {
            scopeOverlay.SetActive(false);
            playerCamera.fieldOfView = defaultFOV; // ZOOM OUT
        }
    }
}