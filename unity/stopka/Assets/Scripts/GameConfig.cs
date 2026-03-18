using UnityEngine;

namespace Stopka
{
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

    }
}
