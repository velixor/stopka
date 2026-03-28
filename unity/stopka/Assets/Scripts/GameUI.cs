using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Stopka
{
    public class GameUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private UIFader startFader;
        [SerializeField] private UIFader playingFader;
        [SerializeField] private UIFader gameOverFader;
        [SerializeField] private UIFader settingsFader;
        [SerializeField] private GameObject settingsDimBackground;

        [Header("Start Screen")]
        [SerializeField] private TextMeshProUGUI highScoreStartText;
        [SerializeField] private TextMeshProUGUI tapToStartText;

        [Header("World Score")]
        [SerializeField] private WorldScoreDisplay worldScore;

        [Header("Game Over Screen")]
        [SerializeField] private TextMeshProUGUI gameOverTitleText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreEndText;
        [SerializeField] private TextMeshProUGUI restartText;

        [Header("Settings")]
        [SerializeField] private Image soundToggleTrack;
        [SerializeField] private RectTransform soundToggleKnob;
        [SerializeField] private Image vibrationToggleTrack;
        [SerializeField] private RectTransform vibrationToggleKnob;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

        private const float FadeDuration = 0.3f;
        private const float PulsePeriod = 2f;
        private const float CountUpDuration = 1f;

        private bool isNewHighScore;
        private bool isSettingsOpen;
        private bool isCountingUp;
        private bool skipCountUp;
        private int countUpTarget;
        private Coroutine tapPulseCoroutine;

        private bool soundEnabled;
        private bool vibrationEnabled;

        public bool IsSettingsOpen => isSettingsOpen;
        public bool IsCountingUp => isCountingUp;

        private void Start()
        {
            soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            vibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
            UpdateToggleVisual(soundToggleTrack, soundToggleKnob, soundEnabled);
            UpdateToggleVisual(vibrationToggleTrack, vibrationToggleKnob, vibrationEnabled);
        }

        public void SetNewHighScore(bool value)
        {
            isNewHighScore = value;
        }

        public void SetState(GameState state, ScoreManager score)
        {
            switch (state)
            {
                case GameState.Start:
                    highScoreStartText.text = $"BEST: {score.HighScore}";
                    startFader.FadeIn(FadeDuration);
                    if (playingFader != null) playingFader.HideImmediate();
                    gameOverFader.HideImmediate();
                    StartTapPulse();
                    if (worldScore != null)
                    {
                        worldScore.ResetDisplay();
                        worldScore.Hide();
                    }
                    break;

                case GameState.Playing:
                    if (worldScore != null) worldScore.Show();
                    UpdateScore(score);
                    startFader.FadeOut(FadeDuration);
                    if (playingFader != null) playingFader.FadeIn(FadeDuration);
                    gameOverFader.HideImmediate();
                    StopTapPulse();
                    break;

                case GameState.GameOver:
                    startFader.HideImmediate();
                    // playingFader stays visible during count-up, hidden after
                    ShowGameOverPanel(score);
                    break;
            }
        }

        public void UpdateScore(ScoreManager score)
        {
            if (worldScore != null)
                worldScore.UpdateScore(score.Score);
        }

        public void ShowNewRecordDuringPlay()
        {
            if (worldScore != null)
                worldScore.ShowNewRecord();
        }

        public void SetScoreTargetHeight(float y)
        {
            if (worldScore != null)
                worldScore.SetTargetHeight(y);
        }

        public void SkipCountUp()
        {
            if (isCountingUp)
                skipCountUp = true;
        }

        // --- Settings ---

        public void OpenSettings()
        {
            if (isSettingsOpen) return;
            isSettingsOpen = true;
            if (settingsDimBackground != null)
                settingsDimBackground.SetActive(true);
            settingsFader.FadeIn(FadeDuration);
        }

        public void CloseSettings()
        {
            if (!isSettingsOpen) return;
            isSettingsOpen = false;
            settingsFader.FadeOut(FadeDuration, () =>
            {
                if (settingsDimBackground != null)
                    settingsDimBackground.SetActive(false);
            });
        }

        public void ToggleSound()
        {
            soundEnabled = !soundEnabled;
            PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
            PlayerPrefs.Save();
            UpdateToggleVisual(soundToggleTrack, soundToggleKnob, soundEnabled);
            if (audioManager != null)
                audioManager.SetMuted(!soundEnabled);
        }

        public void ToggleVibration()
        {
            vibrationEnabled = !vibrationEnabled;
            PlayerPrefs.SetInt("VibrationEnabled", vibrationEnabled ? 1 : 0);
            PlayerPrefs.Save();
            UpdateToggleVisual(vibrationToggleTrack, vibrationToggleKnob, vibrationEnabled);
        }

        public bool IsVibrationEnabled => vibrationEnabled;

        // --- Private ---

        private void ShowGameOverPanel(ScoreManager score)
        {
            if (isNewHighScore)
            {
                gameOverTitleText.text = "\u2605 NEW RECORD \u2605";
                gameOverTitleText.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.7f);
                finalScoreText.color = GoldColor;
                highScoreEndText.text = $"PREVIOUS: {score.HighScore - (score.Score - score.HighScore)}";
                highScoreEndText.color = new Color(1f, 1f, 1f, 0.4f);
            }
            else
            {
                gameOverTitleText.text = "GAME OVER";
                gameOverTitleText.color = new Color(1f, 1f, 1f, 0.5f);
                finalScoreText.color = Color.white;
                highScoreEndText.text = $"BEST: {score.HighScore}";
                highScoreEndText.color = new Color(1f, 1f, 1f, 0.45f);
            }

            restartText.gameObject.SetActive(false);
            StartCoroutine(GameOverSequence(score));
        }

        private IEnumerator GameOverSequence(ScoreManager score)
        {
            // Delay before showing panel (camera pullback)
            yield return new WaitForSeconds(0.5f);

            if (playingFader != null) playingFader.FadeOut(FadeDuration);
            if (worldScore != null) worldScore.Hide();
            gameOverFader.FadeIn(FadeDuration);

            // Count up score
            isCountingUp = true;
            skipCountUp = false;
            countUpTarget = score.Score;
            float elapsed = 0f;

            while (elapsed < CountUpDuration && !skipCountUp)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / CountUpDuration);
                int displayScore = Mathf.RoundToInt(Mathf.Lerp(0, countUpTarget, t));
                finalScoreText.text = $"{displayScore}";
                yield return null;
            }

            finalScoreText.text = $"{countUpTarget}";
            isCountingUp = false;

            restartText.gameObject.SetActive(true);
        }

        private void StartTapPulse()
        {
            if (tapPulseCoroutine != null)
                StopCoroutine(tapPulseCoroutine);
            tapPulseCoroutine = StartCoroutine(TapPulseCoroutine());
        }

        private void StopTapPulse()
        {
            if (tapPulseCoroutine != null)
            {
                StopCoroutine(tapPulseCoroutine);
                tapPulseCoroutine = null;
            }
        }

        private IEnumerator TapPulseCoroutine()
        {
            while (true)
            {
                float t = Mathf.PingPong(Time.unscaledTime, PulsePeriod / 2f) / (PulsePeriod / 2f);
                float alpha = Mathf.Lerp(0.5f, 0.9f, t);
                tapToStartText.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
        }

        private void UpdateToggleVisual(Image track, RectTransform knob, bool isOn)
        {
            if (track == null || knob == null) return;

            track.color = isOn
                ? new Color(1f, 1f, 1f, 0.3f)
                : new Color(1f, 1f, 1f, 0.15f);

            // Knob position: right when on, left when off
            float trackWidth = track.rectTransform.rect.width;
            float knobSize = knob.rect.width;
            float offset = (trackWidth - knobSize) / 2f - 2f;
            knob.anchoredPosition = new Vector2(isOn ? offset : -offset, 0f);

            // Knob color
            var knobImage = knob.GetComponent<Image>();
            if (knobImage != null)
                knobImage.color = isOn ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }
    }
}
