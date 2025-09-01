// Assets/Scripts/WindSystem/WindSource.cs
using UnityEngine;
using System.Collections.Generic;

namespace WindSystem
{
    [DisallowMultipleComponent]
    public class WindSource : MonoBehaviour
    {
        [Tooltip("Local forward direction for wake (if null, world forward is used).")]
        public Transform forwardSource;

        [Tooltip("Strength multiplier of this source.")]
        public float strength = 1.0f;

        [Tooltip("Local radius of influence (meters).")]
        public float radius = 6f;

        [Tooltip("Whether this source moves; if true, it will update internal time offset for phase.")]
        public bool isMoving = true;

        private WindManager windMgr;
        private float localPhase;

        void OnEnable()
        {
            windMgr = FindObjectOfType<WindManager>();
            if (windMgr != null) windMgr.GetType(); // ensure type referenced
            if (windMgr != null) windMgr.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
            localPhase = Random.Range(0f, 1000f);

            // Register with WindManager if possible
            if (windMgr != null)
            {
                // Attempt to register with a RegisterSource method if you had one.
                // We don't rely on that being present; instead, consumers sample SampleWind directly.
            }
        }

        void OnDisable()
        {
            // nothing special
        }

        // Evaluate additive perturbation at point `worldPos`, given current baseWind (useful for direction)
        // This returns a Vector3 perturbation you can add to the base wind.
        public Vector3 EvaluatePerturbation(Vector3 worldPos, Vector3 baseWind, float simTime)
        {
            // Find a config to read wake parameters from; prefer the WindManager's config if available
            WindConfig cfg = windMgr != null ? windMgr.config : null;
            if (cfg == null)
            {
                // fallback default values if no config exists
                cfg = ScriptableObject.CreateInstance<WindConfig>();
            }

            Vector3 dir = forwardSource != null ? forwardSource.forward : transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 rel = worldPos - transform.position;
            float down = Vector3.Dot(rel, dir); // downstream distance
            if (down <= 0f) return Vector3.zero;

            Vector3 proj = transform.position + dir * down;
            float lateral = Vector3.Distance(worldPos, proj);

            float sigma0 = cfg.wakeSigma0;
            float expand = cfg.wakeExpandRate;
            float L = cfg.wakeDecayLength;
            float vortexAmp = cfg.wakeVortexAmplitude;
            float vortexFreq = cfg.wakeVortexFrequency;

            float sigma = sigma0 + expand * down;
            float gauss = Mathf.Exp(-(lateral * lateral) / (2f * sigma * sigma));
            float decay = Mathf.Exp(-down / Mathf.Max(1e-3f, L));

            float attenuation = -0.6f * strength * gauss * decay;

            float phase = localPhase + simTime * (isMoving ? 0.2f : 0f);
            float lateralOsc = Mathf.Sin(down * vortexFreq + phase) * vortexAmp * gauss * decay * strength;

            Vector3 lateralDir = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 perturb = dir * attenuation + lateralDir * lateralOsc;
            return perturb;
        }
    }
}
