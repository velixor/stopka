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

        [Header("Effects — Perfect Squash")]
        [Tooltip("Vertex displacement strength for single-block squash")]
        public float squashAmount = 0.3f;
        [Tooltip("Duration of the squash effect (seconds)")]
        public float squashDuration = 0.2f;

        [Header("Effects — Tower Wave")]
        public WaveStrategy waveStrategy = WaveStrategy.Bulge;
        [Tooltip("Speed of the wave traveling down the tower (units/sec)")]
        public float waveSpeed = 15f;
        [Tooltip("Width of the wave front (units)")]
        public float waveWidth = 1.5f;
        [Tooltip("Strength of vertex displacement")]
        public float waveStrength = 0.3f;

        [Header("Effects — Skybox")]
        [Tooltip("Tower height at which skybox completes full palette cycle")]
        public float skyboxMaxHeight = 50f;

    }
}
