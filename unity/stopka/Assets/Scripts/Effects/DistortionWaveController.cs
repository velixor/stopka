using System.Collections;
using UnityEngine;

namespace Stopka
{
    public class DistortionWaveController : MonoBehaviour
    {
        [SerializeField] private Material waveMaterial;

        private static readonly int WaveCenterId = Shader.PropertyToID("_WaveCenter");
        private static readonly int WaveRadiusId = Shader.PropertyToID("_WaveRadius");
        private static readonly int WaveWidthId = Shader.PropertyToID("_WaveWidth");
        private static readonly int WaveStrengthId = Shader.PropertyToID("_WaveStrength");

        private Coroutine activeWave;

        private void OnEnable()
        {
            // Ensure effect is off at start
            if (waveMaterial != null)
                waveMaterial.SetFloat(WaveStrengthId, 0f);
        }

        public void TriggerWave(Vector3 worldPosition, Camera cam, float duration = 0.6f, float strength = 0.03f)
        {
            if (waveMaterial == null || cam == null) return;

            if (activeWave != null)
                StopCoroutine(activeWave);

            activeWave = StartCoroutine(WaveCoroutine(worldPosition, cam, duration, strength));
        }

        private IEnumerator WaveCoroutine(Vector3 worldPosition, Camera cam, float duration, float strength)
        {
            Vector3 viewport = cam.WorldToViewportPoint(worldPosition);
            waveMaterial.SetVector(WaveCenterId, new Vector4(viewport.x, viewport.y, 0f, 0f));
            waveMaterial.SetFloat(WaveWidthId, 0.1f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeOut = 1f - (1f - t) * (1f - t);

                waveMaterial.SetFloat(WaveRadiusId, easeOut * 1.5f);
                waveMaterial.SetFloat(WaveStrengthId, strength * (1f - t));
                yield return null;
            }

            waveMaterial.SetFloat(WaveStrengthId, 0f);
            activeWave = null;
        }
    }
}
