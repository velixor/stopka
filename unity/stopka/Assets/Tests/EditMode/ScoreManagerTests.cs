using NUnit.Framework;
using Stopka;

namespace Stopka.Tests
{
    public class ScoreManagerTests
    {
        private ScoreManager score;

        [SetUp]
        public void SetUp()
        {
            score = new ScoreManager();
        }

        [Test]
        public void InitialState_ZeroScoreAndCombo()
        {
            Assert.AreEqual(0, score.Score);
            Assert.AreEqual(0, score.ComboCount);
        }

        [Test]
        public void NormalPlacement_ScoreIncrements()
        {
            score.AddPlacement(isPerfect: false);
            Assert.AreEqual(1, score.Score);
        }

        [Test]
        public void NormalPlacement_ComboResets()
        {
            score.AddPlacement(isPerfect: true);
            score.AddPlacement(isPerfect: false);
            Assert.AreEqual(0, score.ComboCount);
        }

        [Test]
        public void PerfectPlacement_ComboBuildUp()
        {
            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(1, score.ComboCount);

            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(2, score.ComboCount);

            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(3, score.ComboCount);
        }

        [Test]
        public void PerfectPlacement_BonusPoints()
        {
            // First perfect: 1 (base) + 1 (combo bonus) = 2
            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(2, score.Score);

            // Second perfect: 2 + 1 (base) + 2 (combo bonus) = 5
            score.AddPlacement(isPerfect: true);
            Assert.AreEqual(5, score.Score);
        }

        [Test]
        public void Reset_ClearsScoreAndCombo()
        {
            score.AddPlacement(isPerfect: true);
            score.AddPlacement(isPerfect: true);
            score.Reset();
            Assert.AreEqual(0, score.Score);
            Assert.AreEqual(0, score.ComboCount);
        }

        [Test]
        public void HighScore_UpdatesWhenBeaten()
        {
            score.HighScore = 5;
            score.AddPlacement(isPerfect: false); // score = 1
            Assert.IsFalse(score.TryUpdateHighScore());

            // Get score above 5
            for (int i = 0; i < 5; i++)
                score.AddPlacement(isPerfect: false); // score = 6
            Assert.IsTrue(score.TryUpdateHighScore());
            Assert.AreEqual(6, score.HighScore);
        }

        [Test]
        public void ComboRecovery_WidensBlock()
        {
            float currentSize = 2f;
            float maxSize = 3f;
            float recoveryRate = 0.05f;

            // After combo 3, recovery = 3 * 0.05 = 0.15
            float recovered = ScoreManager.CalculateRecoveredSize(
                currentSize, maxSize, comboCount: 3, recoveryRate);

            Assert.AreEqual(2.15f, recovered, 0.001f);
        }

        [Test]
        public void ComboRecovery_CapsAtMaxSize()
        {
            float currentSize = 2.95f;
            float maxSize = 3f;
            float recoveryRate = 0.05f;

            float recovered = ScoreManager.CalculateRecoveredSize(
                currentSize, maxSize, comboCount: 5, recoveryRate);

            Assert.AreEqual(3f, recovered, 0.001f);
        }
    }
}
