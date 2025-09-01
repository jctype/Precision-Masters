using UnityEngine;
// test change
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WindSystem
{
    [DisallowMultipleComponent]
    public class WindManager : MonoBehaviour
    {
        [Header("Config (assign a WindConfig asset)")]
        public WindConfig config;

        [Header("Simulation Time")]
        [Tooltip("Wind sim advances with this fixed step (seconds per tick). 0.1 = 10 Hz.")]
        [Min(0.001f)] public float simTimeStep = 0.1f;

        // Internal simulation time (advanced by fixed step)
        private float simTime;

        // Prevent double drawing if multiple WindManagers are present
        private static WindManager s_primaryDrawer;

        void OnEnable()
        {
            // Choose a primary drawer so we don't render arrows twice if there are multiple managers
            if (s_primaryDrawer == null) s_primaryDrawer = this;
        }

        void OnDisable()
        {
            if (s_primaryDrawer == this) s_primaryDrawer = null;
        }

        void Update()
        {
            // Advance wind time at a fixed rate (frame-rate independent)
            simTime += simTimeStep;
        }

        /// <summary>
        /// Sample wind at a world position using current sim time.
        /// </summary>
        public Vector3 SampleWind(Vector3 worldPos) => SampleWind(worldPos, simTime);

        /// <summary>
        /// Sample wind at a world position for a supplied time value (useful for ballistic integrators).
        /// </summary>
        public Vector3 SampleWind(Vector3 worldPos, float timeSeconds)
        {
            if (config == null)
                return Vector3.zero;

            // Base wind direction (from degrees)
            Vector3 baseDir = DirFromDegrees(config.windDirectionDegrees);
            float baseSpeed = Mathf.Max(0f, config.windSpeed);

            // Start with the steady component
            Vector3 wind = baseDir * baseSpeed;

            // -------- Noise field (flowing variation) --------
            // Convert world position to a normalized noise domain and add drifting offset over time.
            float invScale = 1f / Mathf.Max(0.01f, config.noiseScale);
            float tOff = timeSeconds * config.noiseDriftSpeed;

            // Two perpendicular Perlin samplings to create a 2D vector field
            float nX = Mathf.PerlinNoise(worldPos.x * invScale + tOff, worldPos.z * invScale);
            float nZ = Mathf.PerlinNoise(worldPos.x * invScale, worldPos.z * invScale + tOff);

            // Map [0,1] -> [-1,1]
            nX = (nX * 2f) - 1f;
            nZ = (nZ * 2f) - 1f;

            // Scale noise by base speed and strength
            Vector3 noiseVec = new Vector3(nX, 0f, nZ) * baseSpeed * config.noiseStrength;

            // -------- Gusts (temporal magnitude modulation) --------
            float gust = Mathf.PerlinNoise(timeSeconds * config.gustFrequency, 0.123f); // [0..1]
            float gustCentered = (gust * 2f) - 1f; // [-1..1]
            float gustFactor = Mathf.Max(0f, 1f + gustCentered * config.gustStrength);

            // Combine
            wind = (wind + noiseVec) * gustFactor;

            return wind;
        }

        // Utility: convert compass degrees to a unit vector in XZ plane
        private static Vector3 DirFromDegrees(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            // 0° = +X (East), 90° = +Z (North)
            return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
        }

        // ---------------- Gizmo Debug Drawing ----------------

        void OnDrawGizmos()
        {
            if (config == null) return;

            // Allow ONLY one manager to draw arrows to avoid the "two lines per point" issue
            if (s_primaryDrawer != null && s_primaryDrawer != this) return;

            bool draw = (Application.isPlaying && config.drawInPlayMode) ||
                        (!Application.isPlaying && config.drawInEditMode);
            if (!draw) return;

            float spacing = Mathf.Max(1f, config.debugGridSpacing);
            float halfSize = Mathf.Max(10f, config.debugRegionHalfSize);

            // Use our internal sim time in Play Mode; Editor time for Edit Mode preview
            float t = Application.isPlaying
                      ? simTime
#if UNITY_EDITOR
                      : (float)EditorApplication.timeSinceStartup;
#else
                      : 0f;
#endif

            // Center around Scene camera in the Editor; fallback to manager position
            Vector3 center = transform.position;
#if UNITY_EDITOR
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
                center = SceneView.lastActiveSceneView.camera.transform.position;
#endif

            // Grab all terrains so we can test bounds (supports your 4 × 1000 tiles)
            Terrain[] terrains = Terrain.activeTerrains;

            // Draw grid
            for (float x = center.x - halfSize; x <= center.x + halfSize; x += spacing)
            {
                for (float z = center.z - halfSize; z <= center.z + halfSize; z += spacing)
                {
                    // Check if (x,z) lies over any terrain tile; if yes, sample its height there
                    bool overTerrain = false;
                    float height = 0f;

                    for (int i = 0; i < terrains.Length; i++)
                    {
                        var tr = terrains[i];
                        if (tr == null || tr.terrainData == null) continue;

                        Vector3 tPos = tr.transform.position;
                        Vector3 tSize = tr.terrainData.size;

                        if (x >= tPos.x && x <= tPos.x + tSize.x &&
                            z >= tPos.z && z <= tPos.z + tSize.z)
                        {
                            height = tr.SampleHeight(new Vector3(x, 0f, z));
                            overTerrain = true;
                            break;
                        }
                    }

                    if (!overTerrain) continue;

                    // Position just above ground
                    Vector3 p = new Vector3(x, height + 1f, z);

                    // Sample wind
                    Vector3 w = SampleWind(p, t);

                    // Color by magnitude (blue = calm, red = stronger)
                    float strength01 = Mathf.InverseLerp(0f, 15f, w.magnitude);
                    Gizmos.color = Color.Lerp(Color.blue, Color.red, strength01);

                    // Arrow length scaled for readability (increased max length for better visibility)
                    float arrowLen = Mathf.Min(32f, w.magnitude * config.debugArrowLengthScale);

                    // Draw arrow with shaft and head
                    if (w.sqrMagnitude > 0.0001f)
                    {
                        Vector3 q = p + w.normalized * arrowLen;
                        Gizmos.DrawLine(p, q);

                        // Draw arrowhead
                        float headSize = Mathf.Min(4f, arrowLen * 0.25f);
                        Vector3 perpendicular = Vector3.Cross(w.normalized, Vector3.up).normalized * headSize;
                        Gizmos.DrawLine(q, q - w.normalized * headSize + perpendicular);
                        Gizmos.DrawLine(q, q - w.normalized * headSize - perpendicular);
                    }
                }
            }
        }
    }
}
