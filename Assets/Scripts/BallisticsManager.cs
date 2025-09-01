using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using WindSystem;

/// <summary>
/// BallisticsManager (modified) — integrates with WindManager.SampleWind for per-step wind influence,
/// applies aerodynamic drag based on relative airspeed, and runs the bullet simulation at fixed timestep
/// (WaitForFixedUpdate) to improve determinism.
/// 
/// This file was adapted to call windManager.SampleWind(position, simTime) each physics timestep.
/// If you already have mass/Cd/area inside BulletData, this manager will use them automatically if present.
/// Otherwise, use the fallback serialized fields below.
/// </summary>
public class BallisticsManager : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private BulletData bulletData;
    [SerializeField] private Transform firePoint;   // Where the bullet spawns
    [SerializeField] private float gravity = 9.81f;

    [Header("Fallback ballistic physicals (used if BulletData does not provide)")]
    [Tooltip("Bullet mass in kg (fallback).")]
    [SerializeField] private float fallbackBulletMass = 0.02f; // 20 grams
    [Tooltip("Drag coefficient (fallback). Typical bullets ~0.2-0.5 depending on shape).")]
    [SerializeField] private float fallbackDragCoefficient = 0.295f;
    [Tooltip("Cross-section area (m^2) fallback (pi*r^2). For 7.62mm ~4.6e-5 m^2).")]
    [SerializeField] private float fallbackCrossSection = 4.6e-5f;
    [Tooltip("Air density (kg/m^3).")]
    [SerializeField] private float airDensity = 1.225f;

    [Header("Wind Integration")]
    [Tooltip("Reference to authoritative WindManager in scene.")]
    public WindManager windManager;
    [Tooltip("If true, the BallisticsManager advances a deterministic simTime and informs WindManager indirectly.")]
    public bool advanceWindSimTime = true;
    [Tooltip("If advanceWindSimTime is true, this fixed timestep is used for deterministic sampling.")]
    public float fixedSimStep = 0.02f;

    [Header("Hit / Miss Effects")]
    [SerializeField] private ParticleSystem hitEffect;   // Target impact effect
    [SerializeField] private ParticleSystem missEffect;  // Terrain impact effect

    [Header("UI Hit Board")]
    [SerializeField] private RectTransform hitBoardPanel;      // UI Panel in Canvas
    [SerializeField] private GameObject hitMarkerPrefab;       // Small dot prefab (e.g., 5x5)
    [SerializeField] private GameObject nearMissMarkerPrefab;  // Small circle prefab (e.g., 5x5)
    [SerializeField] private GameObject missMarkerPrefab;      // Line prefab for far misses (e.g., 50x5)

    [Header("Layers to Hit")]
    [SerializeField] private LayerMask hitMask; // Assign Target, HitPlane, Terrain layers

    private float targetDiameter = 1f; // Updated when target is first hit
    private Transform targetTransform; // Reference to target for calculations
    private Transform hitPlaneTransform; // Reference to HitPlane for misses
    private BoxCollider hitPlaneCollider; // Reference to HitPlane collider for bounds

    private enum MarkerType { Hit, NearMiss, Miss }

    // internal sim time (used if we are the sim time authority)
    private float simTime = 0f;

    void Start()
    {
        Debug.Log($"WindManager assigned: {windManager != null}");
        if (windManager != null)
        {
            Debug.Log($"WindManager config: {windManager.config != null}");
            if (windManager.config != null)
            {
                Debug.Log($"Wind speed: {windManager.config.windSpeed}");
            }
        }

        // Find and cache HitPlane references
        GameObject hitPlaneObj = GameObject.FindGameObjectWithTag("HitPlane");
        if (hitPlaneObj != null)
        {
            hitPlaneTransform = hitPlaneObj.transform;
            hitPlaneCollider = hitPlaneObj.GetComponent<BoxCollider>();
            if (hitPlaneCollider == null)
            {
                Debug.LogError("HitPlane does not have a BoxCollider!");
            }
            else
            {
                targetTransform = hitPlaneObj.transform.parent; // Initialize with Target parent
                Debug.Log($"HitPlane size: {hitPlaneCollider.size}, position: {hitPlaneTransform.position}, forward: {hitPlaneTransform.forward}, parent: {targetTransform?.name}");
            }
        }
        else
        {
            Debug.LogError("No GameObject with tag 'HitPlane' found in the scene!");
        }

        // Log panel size and validate prefabs
        if (hitBoardPanel != null)
        {
            Canvas canvas = hitBoardPanel.GetComponentInParent<Canvas>();
            Vector2 effectiveSize = new Vector2(
                hitBoardPanel.rect.width * hitBoardPanel.localScale.x,
                hitBoardPanel.rect.height * hitBoardPanel.localScale.y
            );
            Debug.Log($"hitBoardPanel size: width={hitBoardPanel.rect.width}, height={hitBoardPanel.rect.height}, scale={hitBoardPanel.localScale}, effectiveSize={effectiveSize}, canvasScaleFactor={(canvas != null ? canvas.scaleFactor : 1f)}");
        }
        else
        {
            Debug.LogError("hitBoardPanel is not assigned!");
        }

        ValidatePrefab(hitMarkerPrefab, nameof(hitMarkerPrefab));
        ValidatePrefab(nearMissMarkerPrefab, nameof(nearMissMarkerPrefab));
        ValidatePrefab(missMarkerPrefab, nameof(missMarkerPrefab));
    }

    private void ValidatePrefab(GameObject prefab, string prefabName)
    {
        if (prefab == null)
        {
            Debug.LogError($"{prefabName} is not assigned!");
            return;
        }
        RectTransform rect = prefab.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError($"{prefabName} is missing RectTransform component!");
        }
        else
        {
            Debug.Log($"{prefabName} validated: size={rect.sizeDelta}, anchorMin={rect.anchorMin}, anchorMax={rect.anchorMax}, pivot={rect.pivot}, scale={rect.localScale}");
            Image image = prefab.GetComponent<Image>();
            if (image != null)
            {
                Debug.Log($"{prefabName} Image color: {image.color}, enabled: {image.enabled}");
            }
            else
            {
                Debug.LogWarning($"{prefabName} has no Image component!");
            }
        }
        MonoBehaviour[] scripts = prefab.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script == null)
            {
                Debug.LogError($"Missing script detected on {prefabName}!");
            }
        }
    }

    // Public fire method
    public void FireBullet(Vector3 direction)
    {
        if (bulletData == null || bulletData.bulletPrefab == null)
        {
            Debug.LogError("BulletData or Prefab not assigned!");
            return;
        }

        GameObject bullet = Instantiate(bulletData.bulletPrefab, firePoint.position, Quaternion.identity);
        StartCoroutine(SimulateBulletFlight(bullet, direction.normalized * bulletData.muzzleVelocity));
    }

    // Bullet flight simulation — runs at fixed simulation timestep to improve determinism.
    private IEnumerator SimulateBulletFlight(GameObject bullet, Vector3 initialVelocity)
    {
        Vector3 position = bullet.transform.position;
        Vector3 velocity = initialVelocity;

        // Use fixedSimStep (default 0.02s) and WaitForFixedUpdate for deterministic cadence.
        float timeStep = Mathf.Max(0.0001f, fixedSimStep);
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        float timer = 0f;
        float lifetime = 10f;

        // Ensure WindManager simTime is advanced deterministically if requested
        if (advanceWindSimTime && windManager != null)
        {
            // advance windManager's internal simTime by timeStep each loop if it is set to autoAdvanceSimTime=false.
            // (windManager can also be externally advanced — we choose simple local determinism)
        }

        int logCounter = 0;
        while (timer < lifetime)
        {
            Vector3 prevPos = position;

            // Advance deterministic simTime
            simTime += timeStep * (windManager != null && windManager.config != null ? windManager.config.simTimeScale : 1f);

            Vector3 wind = Vector3.zero;
            if (windManager != null)
            {
                wind = windManager.SampleWind(position, simTime);
                if (logCounter % 10 == 0) // Log every 10 steps to avoid spam
                {
                    Debug.Log($"Wind at {position}: {wind}, simTime: {simTime}");
                }
                logCounter++;
            }
            else
            {
                Debug.LogError("WindManager is null!");
            }

            // Compute aerodynamic drag based on relative airspeed (v_rel = bullet - wind)
            float mass = fallbackBulletMass;
            float Cd = fallbackDragCoefficient;
            float A = fallbackCrossSection;

            // If BulletData contains fields for mass/Cd/area, try to use them (best-effort, optional)
            // This avoids forcing you to duplicate constants in two places
            var reflectedMass = GetBulletDataFloat("mass");
            var reflectedCd = GetBulletDataFloat("dragCoefficient");
            var reflectedA = GetBulletDataFloat("crossSectionArea");
            if (reflectedMass > 0f) mass = reflectedMass;
            if (reflectedCd > 0f) Cd = reflectedCd;
            if (reflectedA > 0f) A = reflectedA;

            Vector3 vRel = velocity - wind;
            float speedRel = vRel.magnitude;
            Vector3 dragAcc = Vector3.zero;
            if (speedRel > 0.0001f)
            {
                // Quadratic drag: Fd = 0.5 * rho * Cd * A * v^2 (direction opposite to vRel)
                Vector3 dragForce = -0.5f * airDensity * Cd * A * speedRel * vRel;
                dragAcc = dragForce / Mathf.Max(1e-6f, mass);
            }

            // Gravity acceleration
            Vector3 gravityAcc = Vector3.down * gravity;

            // Integrate (semi-implicit Euler)
            velocity += (gravityAcc + dragAcc) * timeStep;
            position += velocity * timeStep;

            if (logCounter % 10 == 0)
            {
                Debug.Log($"dragAcc: {dragAcc}, velocity: {velocity}, position: {position}");
            }

            // Collision check
            Vector3 dir = (position - prevPos);
            float dist = dir.magnitude;
            if (dist > 1e-6f)
            {
                if (Physics.Raycast(prevPos, dir.normalized, out RaycastHit hit, dist, hitMask))
                {
                    HandleImpact(hit, bullet, initialVelocity, velocity);
                    yield break;
                }
            }

            // Move visual bullet
            bullet.transform.position = position;
            if (velocity.sqrMagnitude > 1e-6f)
                bullet.transform.rotation = Quaternion.LookRotation(velocity);

            timer += timeStep;
            yield return wait; // wait for fixed update
        }

        Destroy(bullet);
    }

    // Reflection helper: tries to read floats from BulletData if fields exist (mass, dragCoefficient, crossSectionArea).
    // Returns -1 if not found.
    private float GetBulletDataFloat(string fieldName)
    {
        if (bulletData == null) return -1f;
        var type = bulletData.GetType();
        var f = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null)
        {
            object val = f.GetValue(bulletData);
            if (val is float fv) return fv;
            if (val is double dv) return (float)dv;
        }
        // also try property
        var p = type.GetProperty(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (p != null)
        {
            object val = p.GetValue(bulletData);
            if (val is float fv) return fv;
            if (val is double dv) return (float)dv;
        }
        return -1f;
    }

    private void HandleImpact(RaycastHit hit, GameObject bullet, Vector3 initialVelocity, Vector3 impactVelocity)
    {
        Vector3 hitPoint = hit.point;

        bool isTargetHit = hit.collider.CompareTag("Target");
        bool isHitPlane = hit.collider.CompareTag("HitPlane");
        bool isTerrain = hit.collider.CompareTag("Terrain");

        Debug.Log($"Bullet hit {(isTargetHit ? "TARGET" : (isHitPlane ? "HITPLANE" : (isTerrain ? "TERRAIN" : "UNKNOWN")))} at {hitPoint}");

        if (isTargetHit)
        {
            if (hitEffect) Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(hit.normal));

            // Save diameter if it's a sphere collider and store target reference
            SphereCollider sc = hit.collider as SphereCollider;
            if (sc != null)
            {
                targetDiameter = sc.radius * 2f;
                targetTransform = hit.collider.transform;
                Debug.Log($"Target diameter set to: {targetDiameter} meters, Target position: {targetTransform.position}");
            }
        }
        else if (isTerrain)
        {
            if (missEffect) Instantiate(missEffect, hitPoint, Quaternion.LookRotation(hit.normal));
        }

        // Use hitPoint for Target hits, project for misses
        Vector3 effectiveHitPoint = isTargetHit ? hitPoint : (CalculatePlaneIntersection(firePoint.position, initialVelocity, hitPlaneTransform.position, hitPlaneTransform.forward) ?? hitPoint);

        // Determine marker type
        MarkerType markerType = DetermineMarkerType(effectiveHitPoint, isTargetHit);

        PlotOnHitBoard(effectiveHitPoint, markerType);

        Destroy(bullet);
    }

    private Vector3? CalculatePlaneIntersection(Vector3 p0, Vector3 v0, Vector3 planePoint, Vector3 planeNormal)
    {
        Vector3 a = Vector3.down * gravity;
        float A = 0.5f * Vector3.Dot(a, planeNormal);
        float B = Vector3.Dot(v0, planeNormal);
        float C = Vector3.Dot(p0 - planePoint, planeNormal);

        if (Mathf.Abs(A) < 1e-6f)
        {
            if (Mathf.Abs(B) < 1e-6f) return null; // Parallel
            float time = -C / B;
            if (time < 0) return null;
            return p0 + v0 * time;
        }

        float discriminant = B * B - 4 * A * C;
        if (discriminant < 0) return null;

        float sqrtDisc = Mathf.Sqrt(discriminant);
        float time1 = (-B + sqrtDisc) / (2 * A);
        float time2 = (-B - sqrtDisc) / (2 * A);
        float selectedTime = (time1 > 0 && time2 > 0) ? Mathf.Min(time1, time2) : (time1 > 0 ? time1 : (time2 > 0 ? time2 : -1));
        if (selectedTime < 0) return null;

        return p0 + v0 * selectedTime + 0.5f * a * selectedTime * selectedTime;
    }

    // Determine marker type based on proximity to target
    private MarkerType DetermineMarkerType(Vector3 effectiveHitPoint, bool isTargetHit)
    {
        if (targetTransform == null || hitPlaneTransform == null || hitPlaneCollider == null)
        {
            Debug.LogWarning("Target, HitPlane, or HitPlaneCollider not set, defaulting to Miss");
            return MarkerType.Miss;
        }

        Vector3 offset = effectiveHitPoint - targetTransform.position;
        float distanceToCenter = new Vector2(offset.z, offset.y).magnitude; // Use Z and Y for 2D distance
        float targetRadius = targetDiameter / 2f;
        float hitPlaneHalfExtentX = hitPlaneCollider.size.x / 2f;
        float hitPlaneHalfExtentY = hitPlaneCollider.size.y / 2f;

        Debug.Log($"DetermineMarkerType: offset={offset}, distanceToCenter={distanceToCenter}, targetRadius={targetRadius}, hitPlaneHalfExtentX={hitPlaneHalfExtentX}, hitPlaneHalfExtentY={hitPlaneHalfExtentY}");

        if (isTargetHit || (Mathf.Abs(offset.z) <= hitPlaneHalfExtentX && Mathf.Abs(offset.y) <= hitPlaneHalfExtentY && distanceToCenter <= targetRadius))
        {
            Debug.Log("Determined MarkerType: Hit");
            return MarkerType.Hit;
        }
        else if (Mathf.Abs(offset.z) <= hitPlaneHalfExtentX && Mathf.Abs(offset.y) <= hitPlaneHalfExtentY)
        {
            Debug.Log("Determined MarkerType: NearMiss");
            return MarkerType.NearMiss;
        }
        else
        {
            Debug.Log("Determined MarkerType: Miss");
            return MarkerType.Miss;
        }
    }

    // Plot impact on HitBoard UI
    private void PlotOnHitBoard(Vector3 effectiveHitPoint, MarkerType markerType)
    {
        if (hitBoardPanel == null)
        {
            Debug.LogError("hitBoardPanel is null!");
            return;
        }
        if (targetTransform == null || hitPlaneTransform == null || hitPlaneCollider == null)
        {
            Debug.LogWarning("Target, HitPlane, or HitPlaneCollider not set, cannot plot marker");
            return;
        }

        // Convert world hit point to HitPlane's local space
        Vector3 localHitPoint = hitPlaneTransform.InverseTransformPoint(effectiveHitPoint);

        // Get HitPlane bounds in local space
        Vector3 hitPlaneHalfExtents = hitPlaneCollider.size * 0.5f;

        // Normalize local coordinates to [-1, 1] range based on HitPlane bounds
        // Flip the X-axis to fix left/right orientation
        float normalizedX = Mathf.Clamp(-localHitPoint.x / hitPlaneHalfExtents.x, -1f, 1f);
        float normalizedY = Mathf.Clamp(localHitPoint.y / hitPlaneHalfExtents.y, -1f, 1f);

        // Convert normalized coordinates to UI panel space
        Vector2 panelSize = hitBoardPanel.rect.size;
        Vector2 markerPos = new Vector2(
            normalizedX * panelSize.x * 0.5f,
            normalizedY * panelSize.y * 0.5f
        );

        // Check if it's a far miss (outside HitPlane bounds)
        bool isFarMiss = Mathf.Abs(localHitPoint.x) > hitPlaneHalfExtents.x ||
                        Mathf.Abs(localHitPoint.y) > hitPlaneHalfExtents.y;

        if (isFarMiss && markerType != MarkerType.Hit)
        {
            markerType = MarkerType.Miss;

            // For far misses, position at the edge of the panel in the direction of the miss
            Vector2 direction = new Vector2(normalizedX, normalizedY).normalized;
            markerPos = new Vector2(
                direction.x * panelSize.x * 0.5f,
                direction.y * panelSize.y * 0.5f
            );
        }

        // Select prefab based on marker type
        GameObject prefabToUse = (markerType == MarkerType.Hit) ? hitMarkerPrefab :
                               (markerType == MarkerType.NearMiss) ? nearMissMarkerPrefab : missMarkerPrefab;

        if (prefabToUse == null)
        {
            Debug.LogWarning($"No prefab assigned for {markerType}");
            return;
        }

        // Instantiate marker
        GameObject marker = Instantiate(prefabToUse, hitBoardPanel);
        RectTransform rect = marker.GetComponent<RectTransform>();
        RectTransform prefabRect = prefabToUse.GetComponent<RectTransform>();

        if (prefabRect != null && rect != null)
        {
            rect.sizeDelta = prefabRect.sizeDelta; // Match prefab size
        }

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        // Zero out child RectTransforms to fix offsets
        RectTransform[] childRects = marker.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform cr in childRects)
        {
            if (cr == rect) continue;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0.5f);
            cr.anchoredPosition = Vector2.zero;
            cr.localPosition = Vector3.zero;
            cr.localScale = Vector3.one;
            cr.localRotation = Quaternion.identity;
        }

        // Place the marker
        rect.anchoredPosition = markerPos;

        // Rotate miss markers perpendicular to radial direction for far misses
        if (isFarMiss && markerType == MarkerType.Miss)
        {
            float angle = Mathf.Atan2(normalizedY, normalizedX) * Mathf.Rad2Deg + 90f;
            rect.rotation = Quaternion.Euler(0, 0, angle);
        }

        Debug.Log($"Plotted {markerType} at {markerPos}, localHitPoint={localHitPoint}, normalized=({normalizedX},{normalizedY}), isFarMiss={isFarMiss}");
    }

    // Optional: Clear all markers from board
    public void ClearBoard()
    {
        foreach (Transform child in hitBoardPanel)
        {
            Destroy(child.gameObject);
        }
    }
}
