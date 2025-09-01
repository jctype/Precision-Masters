using UnityEngine;

public class ScopeZoom : MonoBehaviour
{
    public GameObject scopeOverlay; // Assign your ScopeCanvas in the Inspector

    void Start()
    {
        // CRITICAL: Ensure the scope is hidden when the game starts
        if (scopeOverlay != null)
        {
            scopeOverlay.SetActive(false);
        }
    }

    void Update()
    {
        if (scopeOverlay == null) return;

        // Activate the scope when the button is held down
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            scopeOverlay.SetActive(true);
        }

        // Deactivate the scope when the button is released
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            scopeOverlay.SetActive(false);
        }
    }
}