using System.Collections;
using UnityEngine;
using TMPro;

namespace Stopka
{
    public class WorldScoreDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshPro scoreText;
        [SerializeField] private TextMeshPro newBestText;
        [SerializeField] private float heightOffset = 5f;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float fadeDuration = 0.3f;

        private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

        private float targetY;
        private Camera mainCamera;
        private Coroutine bounceCoroutine;
        private Coroutine glowCoroutine;
        private Coroutine fadeCoroutine;
        private bool isNewRecord;

        private void Start()
        {
            mainCamera = Camera.main;
            if (newBestText != null)
                newBestText.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            // Billboard: face the camera
            if (mainCamera != null)
                transform.forward = mainCamera.transform.forward;

            // Smoothly follow target height
            Vector3 pos = transform.position;
            float desiredY = targetY + heightOffset;
            pos.y = Mathf.Lerp(pos.y, desiredY, Time.deltaTime * followSpeed);
            transform.position = pos;
        }

        public void UpdateScore(int score)
        {
            scoreText.text = $"{score}";
            if (gameObject.activeInHierarchy)
                Bounce();
        }

        public void SetTargetHeight(float y)
        {
            targetY = y;
        }

        /// <summary>
        /// Snap position to target immediately (no lerp).
        /// </summary>
        public void SnapToTarget()
        {
            Vector3 pos = transform.position;
            pos.y = targetY + heightOffset;
            transform.position = pos;
        }

        public void ShowNewRecord()
        {
            if (isNewRecord) return;
            isNewRecord = true;

            scoreText.color = GoldColor;

            if (newBestText != null)
            {
                newBestText.gameObject.SetActive(true);
                newBestText.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.6f);
            }

            if (glowCoroutine != null)
                StopCoroutine(glowCoroutine);
            glowCoroutine = StartCoroutine(GlowPulse());
        }

        public void ResetDisplay()
        {
            isNewRecord = false;
            scoreText.text = "0";
            scoreText.color = Color.white;
            SetAlpha(0f);

            if (glowCoroutine != null)
            {
                StopCoroutine(glowCoroutine);
                glowCoroutine = null;
            }

            if (newBestText != null)
                newBestText.gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SnapToTarget();
            FadeTo(1f);
        }

        public void Hide()
        {
            if (!gameObject.activeInHierarchy)
                return;
            FadeTo(0f, () => gameObject.SetActive(false));
        }

        private void FadeTo(float targetAlpha, System.Action onComplete = null)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha, onComplete));
        }

        private IEnumerator FadeCoroutine(float targetAlpha, System.Action onComplete)
        {
            float startAlpha = scoreText.color.a;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                SetAlpha(alpha);
                yield return null;
            }

            SetAlpha(targetAlpha);
            fadeCoroutine = null;
            onComplete?.Invoke();
        }

        private void SetAlpha(float alpha)
        {
            Color c = scoreText.color;
            scoreText.color = new Color(c.r, c.g, c.b, alpha);

            if (newBestText != null && newBestText.gameObject.activeSelf)
            {
                Color nb = newBestText.color;
                newBestText.color = new Color(nb.r, nb.g, nb.b, alpha * 0.6f);
            }
        }

        private void Bounce()
        {
            if (bounceCoroutine != null)
                StopCoroutine(bounceCoroutine);
            bounceCoroutine = StartCoroutine(BounceCoroutine());
        }

        private IEnumerator BounceCoroutine()
        {
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 originalScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
                scoreText.transform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            scoreText.transform.localScale = originalScale;
            bounceCoroutine = null;
        }

        private IEnumerator GlowPulse()
        {
            while (true)
            {
                float t = Mathf.PingPong(Time.unscaledTime, 0.75f) / 0.75f;
                float alpha = Mathf.Lerp(0.8f, 1f, t);
                scoreText.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, alpha);
                yield return null;
            }
        }
    }
}
