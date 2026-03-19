using UnityEngine;

namespace Stopka
{
    public class BlockColorManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private Color currentPastel;
        private Color nextPastel;
        private int blocksInSegment;

        public Color CurrentBlockColor { get; private set; }

        public void Initialize()
        {
            currentPastel = RandomPastel();
            nextPastel = RandomPastel();
            blocksInSegment = 0;
            CurrentBlockColor = currentPastel;
        }

        public Color GetColorForLayer(int layer)
        {
            float t = (float)blocksInSegment / config.colorTransitionBlocks;
            Color color = Color.Lerp(currentPastel, nextPastel, t);
            CurrentBlockColor = color;

            blocksInSegment++;
            if (blocksInSegment >= config.colorTransitionBlocks)
            {
                currentPastel = nextPastel;
                nextPastel = RandomPastel();
                blocksInSegment = 0;
            }

            return color;
        }

        private Color RandomPastel()
        {
            float hue = Random.Range(0f, 1f);
            float sat = Random.Range(config.pastelSatMin, config.pastelSatMax);
            float val = Random.Range(config.pastelValMin, config.pastelValMax);
            return Color.HSVToRGB(hue, sat, val);
        }
    }
}
