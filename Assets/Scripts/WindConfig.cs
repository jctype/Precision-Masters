// Assets/Scripts/WindSystem/WindConfig.cs
using UnityEngine;

namespace WindSystem
{
    [CreateAssetMenu(fileName = "WindConfig", menuName = "Game Systems/Wind Config", order = 0)]
    public class WindConfig : ScriptableObject
    {
        [Header("Base Breeze")]
        [Tooltip("Compass heading in degrees. 0 = +X (East), 90 = +Z (North).")]
        public float windDirectionDegrees = 90f;

        [Tooltip("Average wind speed in meters/second.")]
        [Min(0f)] public float windSpeed = 5f;

        [Header("Noise (Flowing Variation)")]
        [Tooltip("Bigger values create larger, smoother zones. Smaller values create tighter, choppier variation.")]
        [Min(0.01f)] public float noiseScale = 100f;

        [Tooltip("How strongly the noise bends/perturbs the wind (0 = none, 1 = strong).")]
        [Range(0f, 1f)] public float noiseStrength = 0.0f; // start laminar by default

        [Tooltip("How fast the noise pattern drifts across the world (Satori-like flow).")]
        [Min(0f)] public float noiseDriftSpeed = 0.0f; // start zero drift by default

        [Header("Gusts (Temporal Intensity)")]
        [Tooltip("How much gusts modulate the magnitude around the base wind (0 = none).")]
        [Range(0f, 2f)] public float gustStrength = 0f; // default off

        [Tooltip("How quickly gust intensity varies over time (higher = quicker fluctuations).")]
        [Min(0f)] public float gustFrequency = 0.3f;

        [Header("Editor Debug")]
        [Tooltip("Show wind arrows in Edit Mode (Scene view).")]
        public bool drawInEditMode = true;

        [Tooltip("Show wind arrows while the game is running (Play Mode).")]
        public bool drawInPlayMode = true;

        [Tooltip("Grid spacing between arrows (Scene units). Larger = fewer arrows.")]
        [Min(1f)] public float debugGridSpacing = 50f;

        [Tooltip("Half-size of the debug draw region around the Scene camera (Scene units).")]
        [Min(10f)] public float debugRegionHalfSize = 400f;

        [Tooltip("Scales the length of debug arrows for readability.")]
        [Min(0.1f)] public float debugArrowLengthScale = 4f;

        // --------------------------
        // Legacy /  fields
        // (kept public so older scripts referencing these don't break)
        // --------------------------

        [Header(" (legacy names)")]
        [Tooltip(": previous 'simTimeScale' used by other scripts.")]
        public float simTimeScale = 1f;

        [Tooltip(": original name for wake initial spread (meters).")]
        public float wakeSigma0 = 1.2f;

        [Tooltip(": wake expansion rate.")]
        public float wakeExpandRate = 0.2f;

        [Tooltip(": how far wakes decay (meters).")]
        public float wakeDecayLength = 40f;

        [Tooltip(": lateral vortex amplitude in wakes.")]
        public float wakeVortexAmplitude = 1.0f;

        [Tooltip(": lateral vortex spatial frequency.")]
        public float wakeVortexFrequency = 0.6f;

        [Tooltip(": cap on number of active wakes evaluated.")]
        public int maxActiveWakes = 24;

        // You can extend this  block with any other legacy fields your project expects.
    }
}
