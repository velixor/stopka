using System.Collections;
using UnityEngine;

namespace Stopka
{
    public class CameraShake : MonoBehaviour
    {
        public void Shake(float intensity = 0.1f, float duration = 0.15f)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            Vector3 originalLocalPos = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float strength = intensity * (1f - elapsed / duration);
                transform.localPosition = originalLocalPos + Random.insideUnitSphere * strength;
                yield return null;
            }

            transform.localPosition = originalLocalPos;
        }
    }
}
