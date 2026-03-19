using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stopka
{
    public class TowerWaveController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private static readonly int WaveFrontYId = Shader.PropertyToID("_WaveFrontY");
        private static readonly int WaveWidthId = Shader.PropertyToID("_WaveWidth");
        private static readonly int WaveStrengthId = Shader.PropertyToID("_WaveStrength");

        private static readonly string[] StrategyKeywords =
        {
            "_WAVE_BULGE",
            "_WAVE_SQUEEZE",
            "_WAVE_JITTER",
        };

        private Coroutine activeWave;

        public void TriggerWave(List<Block> placedBlocks, GameObject foundationBlock,
            BlockColorManager colorManager, int currentLayer)
        {
            if (placedBlocks.Count == 0) return;

            if (activeWave != null)
                StopCoroutine(activeWave);

            activeWave = StartCoroutine(WaveCoroutine(placedBlocks, foundationBlock));
        }

        private void SetStrategyKeyword(List<Block> placedBlocks, GameObject foundationBlock)
        {
            string keyword = StrategyKeywords[(int)config.waveStrategy];
            ApplyKeyword(placedBlocks, foundationBlock, keyword);
        }

        private void ApplyKeyword(List<Block> placedBlocks, GameObject foundationBlock, string keyword)
        {
            foreach (var block in placedBlocks)
            {
                if (block == null) continue;
                var r = block.GetComponent<Renderer>();
                if (r == null) continue;
                var mat = r.material;
                foreach (var kw in StrategyKeywords)
                    mat.DisableKeyword(kw);
                mat.EnableKeyword(keyword);
            }

            if (foundationBlock != null)
            {
                var r = foundationBlock.GetComponent<Renderer>();
                if (r != null)
                {
                    var mat = r.material;
                    foreach (var kw in StrategyKeywords)
                        mat.DisableKeyword(kw);
                    mat.EnableKeyword(keyword);
                }
            }
        }

        private void ClearKeywords(List<Block> placedBlocks, GameObject foundationBlock)
        {
            ApplyKeyword(placedBlocks, foundationBlock, "");
            // Disable all
            foreach (var block in placedBlocks)
            {
                if (block == null) continue;
                var r = block.GetComponent<Renderer>();
                if (r == null) continue;
                var mat = r.material;
                foreach (var kw in StrategyKeywords)
                    mat.DisableKeyword(kw);
            }
            if (foundationBlock != null)
            {
                var r = foundationBlock.GetComponent<Renderer>();
                if (r != null)
                {
                    var mat = r.material;
                    foreach (var kw in StrategyKeywords)
                        mat.DisableKeyword(kw);
                }
            }
        }

        private IEnumerator WaveCoroutine(List<Block> placedBlocks, GameObject foundationBlock)
        {
            float topY = placedBlocks[placedBlocks.Count - 1].transform.position.y;
            float bottomY = 0f;
            float totalDistance = topY - bottomY + config.waveWidth;

            SetStrategyKeyword(placedBlocks, foundationBlock);

            Shader.SetGlobalFloat(WaveWidthId, config.waveWidth);
            Shader.SetGlobalFloat(WaveStrengthId, config.waveStrength);

            float elapsed = 0f;
            float duration = totalDistance / config.waveSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentY = Mathf.Lerp(topY + config.waveWidth, bottomY - config.waveWidth, t);
                Shader.SetGlobalFloat(WaveFrontYId, currentY);
                yield return null;
            }

            Shader.SetGlobalFloat(WaveStrengthId, 0f);
            Shader.SetGlobalFloat(WaveFrontYId, -100f);
            ClearKeywords(placedBlocks, foundationBlock);

            activeWave = null;
        }
    }
}
