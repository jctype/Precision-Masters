using UnityEngine;

public class ForceCanvasSetup : MonoBehaviour
{
    public Camera renderCamera; // Assign this in the inspector

    void Start()
    {
        Canvas canvas = GetComponent<Canvas>();

        if (canvas != null)
        {
            // Force the canvas into Screen Space - Camera mode
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            // Force the render camera to be the main camera
            canvas.worldCamera = renderCamera;
            // Set the plane distance
            canvas.planeDistance = 0.5f;

            Debug.Log("Canvas forced to Screen Space - Camera mode.");
        }
    }
}