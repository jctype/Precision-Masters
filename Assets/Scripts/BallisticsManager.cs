using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WindSystem;
public class BallisticsManager : MonoBehaviour
{
    [Header("References")]
    public WindManager windManager;
    public Transform hitPlaneTransform;
    public RectTransform hitPanel;
    public GameObject hitMarkerPrefab;

    [Header("Simulation Settings")]
    public float dragCoefficient = 0.05f;
    public float simulationStep = 0.02f;
    public float maxSimulationTime = 5f;

    [Header("Marker Settings")]
    public Color nearMissColor = Color.yellow;
    public Color hitColor = Color.red;
    public Color edgeHitColor = Color.green;

    private List<GameObject> spawnedMarkers = new List<GameObject>();

    void Start()
    {
        hitPlaneTransform = GameObject.Find("HitPlane").transform;
    }

    public void ClearMarkers()
    {
        foreach (var marker in spawnedMarkers)
        {
            Destroy(marker);
        }
        spawnedMarkers.Clear();
    }

    public void FireBullet(Vector3 startPos, Vector3 startVel)
    {
        Vector3 pos = startPos;
        Vector3 velocity = startVel;
        float time = 0f;

        while (time < maxSimulationTime)
        {
            Vector3 prevPos = pos;

            // Sample wind from WindManager
            Vector3 wind = windManager != null ? windManager.SampleWind(pos, time) : Vector3.zero;

            // Relative velocity (bullet vs moving air)
            Vector3 relVel = velocity - wind;

            // Drag force (simplified quadratic model)
            Vector3 dragAccel = -dragCoefficient * relVel.magnitude * relVel;

            // Apply forces
            velocity += (Physics.gravity + dragAccel) * simulationStep;

            // Move bullet
            pos += velocity * simulationStep;

            // Raycast to detect collisions
            if (Physics.Raycast(prevPos, pos - prevPos, out RaycastHit hit, (pos - prevPos).magnitude))
            {
                Vector3 effectiveHitPoint = hit.point;

                // Transform hit into local plane space
                Vector3 localHitPoint = hitPlaneTransform.InverseTransformPoint(effectiveHitPoint);

                // Decide marker type
                string markerType = DetermineMarkerType(localHitPoint);

                // Plot marker on HitPanel
                PlotOnHitBoard(localHitPoint, markerType);

                break;
            }

            time += simulationStep;
        }
    }

    private string DetermineMarkerType(Vector3 localHitPoint)
    {
        // Use local X (horizontal) and Y (vertical) consistently
        float x = localHitPoint.x;
        float y = localHitPoint.y;

        // Example thresholds (tweak for your target size)
        float halfWidth = 0.5f;
        float halfHeight = 0.5f;

        bool nearEdge = Mathf.Abs(x) > halfWidth * 0.9f || Mathf.Abs(y) > halfHeight * 0.9f;

        if (nearEdge) return "edge";
        return "hit";
    }

    private void PlotOnHitBoard(Vector3 localHitPoint, string markerType)
    {
        // Map local hit point (x,y) into UI coordinates
        float normalizedX = (localHitPoint.x / 1.0f) + 0.5f; // assumes plane extends -0.5..0.5
        float normalizedY = (localHitPoint.y / 1.0f) + 0.5f;

        float panelX = (normalizedX - 0.5f) * hitPanel.rect.width;
        float panelY = (normalizedY - 0.5f) * hitPanel.rect.height;

        Vector3 panelPos = new Vector3(panelX, panelY, 0f);

        GameObject marker = Instantiate(hitMarkerPrefab, hitPanel);
        marker.GetComponent<RectTransform>().anchoredPosition = panelPos;

        Image markerImage = marker.GetComponent<Image>();
        switch (markerType)
        {
            case "hit":
                markerImage.color = hitColor;
                break;
            case "edge":
                markerImage.color = edgeHitColor;
                break;
            default:
                markerImage.color = nearMissColor;
                break;
        }

        spawnedMarkers.Add(marker);
    }
}
