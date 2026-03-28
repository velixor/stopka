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

        private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);

        private float targetY;
        private Camera mainCamera;
        private Coroutine bounceCoroutine;
        private Coroutine glowCoroutine;
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
        }

        public void Hide()
        {
            gameObject.SetActive(false);
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
