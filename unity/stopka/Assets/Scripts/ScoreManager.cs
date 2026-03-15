using UnityEngine;

namespace Stopka
{
    public class ScoreManager
    {
        public int Score { get; private set; }
        public int ComboCount { get; private set; }
        public int HighScore { get; set; }

        public void AddPlacement(bool isPerfect)
        {
            Score++;
            if (isPerfect)
            {
                ComboCount++;
                Score += ComboCount;
            }
            else
            {
                ComboCount = 0;
            }
        }

        public bool TryUpdateHighScore()
        {
            if (Score > HighScore)
            {
                HighScore = Score;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            Score = 0;
            ComboCount = 0;
        }

        public static float CalculateRecoveredSize(
            float currentSize, float maxSize, int comboCount, float recoveryRate)
        {
            return Mathf.Min(currentSize + comboCount * recoveryRate, maxSize);
        }
    }
}
