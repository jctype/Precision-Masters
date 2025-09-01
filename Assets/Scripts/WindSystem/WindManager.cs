// Assets/Scripts/WindSystem/WindManager.cs
using UnityEngine;

namespace WindSystem
{
    [DisallowMultipleComponent]
    public class WindManager : MonoBehaviour
    {
        [Header("Config (assign a WindConfig asset)")]
        public WindConfig config;

        [Header("Simulation Time")]
        [Tooltip("Wind sim advances with this fixed step (seconds per tick). 0.0333 = 30 Hz.")]
        [Min(0.001f)] public float simTimeStep = 0.0333f;

        // Internal simulation time (advanced by fixed step)
        private float simTime;

        // Prevent double drawing if multiple WindManagers are present
        private static WindManager s_primaryDrawer;

        void OnEnable()
        {
            if (s_primaryDrawer == null) s_primaryDrawer = this;
        }

        void OnDisable()
        {
            if (s_primaryDrawer == this) s_primaryDrawer = null;
        }

        void Update()
        {
            // Use compatibility scale if available
            float scale = config != null ? config.simTimeScale : 1f;
            simTime += simTimeStep * scale;
        }

        /// <summary>
        /// Sample wind at world position using current sim time.
        /// </summary>
        public Vector3 SampleWind(Vector3 worldPos) => SampleWind(worldPos, simTime);

        /// <summary>
        /// Sample wind at world position for a supplied time value (useful for ballistic integrators).
        /// </summary>
        public Vector3 SampleWind(Vector3 worldPos, float timeSeconds)
        {
            if (config == null)
                return Vector3.zero;

            // Base wind: direction + speed
            Vector3 baseDir = DirFromDegrees(config.windDirectionDegrees);
            float baseSpeed = Mathf.Max(0f, config.windSpeed);
            Vector3 wind = baseDir * baseSpeed;

            // Noise (flowing variation) — gentle by default
            float invScale = 1f / Mathf.Max(0.01f, config.noiseScale);
            float tOff = timeSeconds * config.noiseDriftSpeed;

            float nX = Mathf.PerlinNoise(worldPos.x * invScale + tOff, worldPos.z * invScale);
            float nZ = Mathf.PerlinNoise(worldPos.x * invScale, worldPos.z * invScale + tOff);

            nX = (nX * 2f) - 1f;
            nZ = (nZ * 2f) - 1f;

            // Scale noise relative to baseSpeed and strength
            Vector3 noiseVec = new Vector3(nX, 0f, nZ) * baseSpeed * config.noiseStrength;

            // Gust envelope (temporal)
            float gust = Mathf.PerlinNoise(timeSeconds * config.gustFrequency, 0.123f); // [0..1]
            float gustCentered = (gust * 2f) - 1f; // [-1..1]
            float gustFactor = Mathf.Max(0f, 1f + gustCentered * config.gustStrength);

            // Combine steady wind + noise and apply gust multiplier
            wind = (wind + noiseVec) * gustFactor;

            // Note: wakes/obstacles can be added by consumers or by registering sources later.

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
            if (s_primaryDrawer != null && s_primaryDrawer != this) return;

            bool draw = (Application.isPlaying && config.drawInPlayMode) ||
                        (!Application.isPlaying && config.drawInEditMode);
            if (!draw) return;

            float spacing = Mathf.Max(1f, config.debugGridSpacing);
            float halfSize = Mathf.Max(10f, config.debugRegionHalfSize);

            float t = Application.isPlaying ? simTime
#if UNITY_EDITOR
                     : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
                     : 0f;
#endif

            Vector3 center = transform.position;
#if UNITY_EDITOR
            if (UnityEditor.SceneView.lastActiveSceneView != null && UnityEditor.SceneView.lastActiveSceneView.camera != null)
                center = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
#endif

            Terrain[] terrains = Terrain.activeTerrains;

            for (float x = center.x - halfSize; x <= center.x + halfSize; x += spacing)
            {
                for (float z = center.z - halfSize; z <= center.z + halfSize; z += spacing)
                {
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

                    Vector3 p = new Vector3(x, height + 1f, z);
                    Vector3 w = SampleWind(p, t);
                    float strength01 = Mathf.InverseLerp(0f, 15f, w.magnitude);
                    UnityEngine.Gizmos.color = Color.Lerp(Color.blue, Color.red, strength01);

                    float arrowLen = Mathf.Min(16f, w.magnitude * config.debugArrowLengthScale);
                    if (w.sqrMagnitude > 0.0001f)
                    {
                        Vector3 q = p + w.normalized * arrowLen;
                        UnityEngine.Gizmos.DrawLine(p, q);
                    }
                }
            }
        }
    }
}
