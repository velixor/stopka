using UnityEngine;

namespace Stopka
{
    public enum WaveStrategy { Bulge, Squeeze, Jitter }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Stopka/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Speed")]
        public float startSpeed = 3f;
        public float speedIncrement = 0.05f;
        public float maxSpeed = 8f;
        public SpeedStrategy speedStrategy = SpeedStrategy.Accelerating;
        [Tooltip("How much speedIncrement grows per cycle (Accelerating strategy)")]
        public float speedAcceleration = 0.5f;

        [Header("Placement")]
        public float perfectTolerance = 0.1f;

        [Header("Combo")]
        public float comboRecoveryRate = 0.05f;
        [Tooltip("Number of consecutive perfects before recovery kicks in")]
        public int comboRecoveryThreshold = 3;
        [Tooltip("Recovery rate random multiplier range")]
        public float recoveryRandomMin = 0.5f;
        public float recoveryRandomMax = 1.5f;

        [Header("Block")]
        public float blockHeight = 0.5f;
        public Vector2 startBlockSize = new Vector2(3f, 3f);

        [Header("Spawning")]
        [Tooltip("How far off-screen the block starts sliding from")]
        public float slideRange = 5f;

        [Header("Effects — Block Pulse")]
        [Tooltip("Emission intensity multiplier on perfect placement")]
        public float pulseIntensity = 2f;
        [Tooltip("Duration of the pulse glow fade (seconds)")]
        public float pulseDuration = 0.5f;

        [Header("Effects — Tower Wave")]
        public WaveStrategy waveStrategy = WaveStrategy.Bulge;
        [Tooltip("Speed of the wave traveling down the tower (units/sec)")]
        public float waveSpeed = 15f;
        [Tooltip("Width of the wave front (units)")]
        public float waveWidth = 1.5f;
        [Tooltip("Strength of vertex displacement")]
        public float waveStrength = 0.3f;

        [Header("Colors")]
        [Tooltip("Number of blocks per color transition")]
        public int colorTransitionBlocks = 6;
        [Range(0.2f, 0.5f)]
        public float pastelSatMin = 0.25f;
        [Range(0.2f, 0.5f)]
        public float pastelSatMax = 0.45f;
        [Range(0.8f, 1f)]
        public float pastelValMin = 0.85f;
        [Range(0.8f, 1f)]
        public float pastelValMax = 0.95f;

        [Header("Camera")]
        public Vector3 cameraOffset = new Vector3(5f, 6f, 5f);
        [Tooltip("Vertical screen position of the active block (0 = bottom, 1 = top)")]
        [Range(0f, 1f)]
        public float blockScreenY = 0.4f;

    }
}
