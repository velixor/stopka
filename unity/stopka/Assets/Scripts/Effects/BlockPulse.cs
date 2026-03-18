using System.Collections;
using UnityEngine;

namespace Stopka
{
    public class BlockPulse : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public void Pulse(Color baseColor, float intensity = 2f, float duration = 0.5f)
        {
            StartCoroutine(PulseCoroutine(baseColor, intensity, duration));
        }

        private IEnumerator PulseCoroutine(Color baseColor, float intensity, float duration)
        {
            var mat = GetComponent<Renderer>().material;
            mat.EnableKeyword("_EMISSION");

            Color startEmission = baseColor * intensity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = 1f - (1f - t) * (1f - t); // ease-out
                mat.SetColor(EmissionColorId, Color.Lerp(startEmission, Color.black, t));
                yield return null;
            }

            mat.SetColor(EmissionColorId, Color.black);
            mat.DisableKeyword("_EMISSION");
            Destroy(this);
        }
    }
}
