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

        public bool ShouldRecover(int threshold)
        {
            return ComboCount >= threshold;
        }

        public static float CalculateRecoveredSize(
            float currentSize, float maxSize, int comboCount, int threshold, float recoveryRate)
        {
            int excess = Mathf.Max(0, comboCount - threshold);
            if (excess == 0) return currentSize;
            return Mathf.Min(currentSize + excess * recoveryRate, maxSize);
        }
    }
}
