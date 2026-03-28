using System.Collections;
using UnityEngine;

namespace Stopka
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIFader : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Coroutine activeCoroutine;

        private void EnsureInitialized()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
        }

        public void FadeIn(float duration = 0.3f)
        {
            gameObject.SetActive(true);
            EnsureInitialized();
            StopActive();
            activeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, 0.95f, 1f, duration));
        }

        public void FadeOut(float duration = 0.3f, System.Action onComplete = null)
        {
            EnsureInitialized();
            StopActive();
            activeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, 1f, 0.95f, duration, () =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            }));
        }

        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            EnsureInitialized();
            StopActive();
            canvasGroup.alpha = 1f;
            rectTransform.localScale = Vector3.one;
        }

        public void HideImmediate()
        {
            EnsureInitialized();
            StopActive();
            canvasGroup.alpha = 0f;
            rectTransform.localScale = Vector3.one * 0.95f;
            gameObject.SetActive(false);
        }

        private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float fromScale, float toScale, float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                float scale = Mathf.Lerp(fromScale, toScale, t);
                rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            canvasGroup.alpha = toAlpha;
            rectTransform.localScale = new Vector3(toScale, toScale, 1f);
            activeCoroutine = null;
            onComplete?.Invoke();
        }

        private void StopActive()
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }
    }
}
