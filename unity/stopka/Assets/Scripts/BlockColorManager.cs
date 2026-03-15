using UnityEngine;

namespace Stopka
{
    public class BlockColorManager : MonoBehaviour
    {
        [SerializeField] private float hueStep = 0.03f;
        [SerializeField] private float saturation = 0.6f;
        [SerializeField] private float value = 0.9f;

        private float currentHue;

        public void Initialize()
        {
            currentHue = Random.Range(0f, 1f);
        }

        public Color GetColorForLayer(int layer)
        {
            float hue = (currentHue + hueStep * layer) % 1f;
            return Color.HSVToRGB(hue, saturation, value);
        }
    }
}
