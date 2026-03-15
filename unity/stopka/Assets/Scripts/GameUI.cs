using System.Collections;
using UnityEngine;
using TMPro;

namespace Stopka
{
    public class GameUI : MonoBehaviour
    {
        [Header("Start Screen")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private TextMeshProUGUI highScoreStartText;

        [Header("Playing HUD")]
        [SerializeField] private GameObject playingPanel;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreEndText;
        [SerializeField] private GameObject newHighScoreBadge;

        public void SetState(GameState state, ScoreManager score)
        {
            startPanel.SetActive(state == GameState.Start);
            playingPanel.SetActive(state == GameState.Playing);
            gameOverPanel.SetActive(state == GameState.GameOver);

            switch (state)
            {
                case GameState.Start:
                    highScoreStartText.text = $"Best: {score.HighScore}";
                    break;
                case GameState.Playing:
                    UpdateScore(score);
                    break;
                case GameState.GameOver:
                    finalScoreText.text = $"{score.Score}";
                    highScoreEndText.text = $"Best: {score.HighScore}";
                    newHighScoreBadge.SetActive(score.Score >= score.HighScore && score.Score > 0);
                    break;
            }
        }

        private Coroutine comboFadeCoroutine;

        public void ShowCombo(int comboCount)
        {
            if (comboFadeCoroutine != null)
                StopCoroutine(comboFadeCoroutine);

            comboText.gameObject.SetActive(true);
            comboText.text = comboCount > 1 ? $"PERFECT x{comboCount}" : "PERFECT!";
            comboFadeCoroutine = StartCoroutine(FadeComboText());
        }

        private IEnumerator FadeComboText()
        {
            Color color = comboText.color;
            color.a = 1f;
            comboText.color = color;

            yield return new WaitForSeconds(0.5f);

            float fadeDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = 1f - elapsed / fadeDuration;
                comboText.color = color;
                yield return null;
            }

            comboText.gameObject.SetActive(false);
        }

        public void UpdateScore(ScoreManager score)
        {
            scoreText.text = $"{score.Score}";
            if (score.ComboCount > 1)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = $"PERFECT x{score.ComboCount}";
            }
            else if (score.ComboCount == 1)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = "PERFECT!";
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }
}
