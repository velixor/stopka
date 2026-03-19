using UnityEngine;

namespace Stopka
{
    public class SkyboxController : MonoBehaviour
    {
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private GameConfig config;

        private Material runtimeMaterial;

        private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
        private static readonly int MiddleColorId = Shader.PropertyToID("_MiddleColor");
        private static readonly int BottomColorId = Shader.PropertyToID("_BottomColor");

        // 4 palettes: dawn, day, sunset, night
        // Each palette has 3 colors: bottom, middle, top
        private static readonly Color[][] Palettes =
        {
            // Dawn — warm oranges and purples
            new[]
            {
                HexColor("1a0a2e"), // bottom: deep purple
                HexColor("3d1f5c"), // middle: purple
                HexColor("ff6b4a"), // top: warm orange
            },
            // Day — bright blues
            new[]
            {
                HexColor("1a1a3e"), // bottom: dark blue
                HexColor("2d4a7a"), // middle: medium blue
                HexColor("4a90d9"), // top: sky blue
            },
            // Sunset — reds and oranges
            new[]
            {
                HexColor("1a0a1e"), // bottom: very dark purple
                HexColor("8b2252"), // middle: deep rose
                HexColor("ff4500"), // top: orange-red
            },
            // Night/Space — deep blues and blacks
            new[]
            {
                HexColor("0a0a14"), // bottom: near black
                HexColor("0d1b2a"), // middle: very dark blue
                HexColor("1b2838"), // top: dark steel blue
            },
        };

        public void UpdateForHeight(float height)
        {
            if (skyboxMaterial == null) return;

            // Create runtime instance to avoid modifying the asset in editor
            if (runtimeMaterial == null)
            {
                runtimeMaterial = new Material(skyboxMaterial);
                RenderSettings.skybox = runtimeMaterial;
            }

            float progress = Mathf.Clamp01(height / config.skyboxMaxHeight);

            // Map progress to palette index (cycle through 4 palettes)
            float scaledProgress = progress * (Palettes.Length - 1);
            int idx = Mathf.FloorToInt(scaledProgress);
            float t = scaledProgress - idx;

            // Clamp to valid range
            int nextIdx = Mathf.Min(idx + 1, Palettes.Length - 1);
            idx = Mathf.Min(idx, Palettes.Length - 1);

            Color bottom = Color.Lerp(Palettes[idx][0], Palettes[nextIdx][0], t);
            Color middle = Color.Lerp(Palettes[idx][1], Palettes[nextIdx][1], t);
            Color top = Color.Lerp(Palettes[idx][2], Palettes[nextIdx][2], t);

            runtimeMaterial.SetColor(BottomColorId, bottom);
            runtimeMaterial.SetColor(MiddleColorId, middle);
            runtimeMaterial.SetColor(TopColorId, top);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
