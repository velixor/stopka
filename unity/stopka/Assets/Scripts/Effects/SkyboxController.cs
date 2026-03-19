using UnityEngine;

namespace Stopka
{
    public class SkyboxController : MonoBehaviour
    {
        [SerializeField] private Material skyboxMaterial;

        private Material runtimeMaterial;

        private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
        private static readonly int MiddleColorId = Shader.PropertyToID("_MiddleColor");
        private static readonly int BottomColorId = Shader.PropertyToID("_BottomColor");

        public void UpdateFromBlockColor(Color blockColor)
        {
            if (skyboxMaterial == null) return;

            if (runtimeMaterial == null)
            {
                runtimeMaterial = new Material(skyboxMaterial);
                RenderSettings.skybox = runtimeMaterial;
            }

            Color.RGBToHSV(blockColor, out float h, out float s, out float v);

            Color top = Color.HSVToRGB(h, s * 0.3f, 0.95f);
            Color middle = Color.HSVToRGB(h, s * 0.5f, 0.75f);
            Color bottom = Color.HSVToRGB(h, s * 0.6f, 0.45f);

            runtimeMaterial.SetColor(TopColorId, top);
            runtimeMaterial.SetColor(MiddleColorId, middle);
            runtimeMaterial.SetColor(BottomColorId, bottom);
        }
    }
}
