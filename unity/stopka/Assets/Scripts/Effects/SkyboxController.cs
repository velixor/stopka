using UnityEngine;

namespace Stopka
{
    public class SkyboxController : MonoBehaviour
    {
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private float transitionSpeed = 2f;

        private Material runtimeMaterial;
        private bool initialized;

        private Color targetTop, targetMiddle, targetBottom;
        private Color currentTop, currentMiddle, currentBottom;

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

            targetTop = Color.HSVToRGB(h, s * 0.3f, 0.95f);
            targetMiddle = Color.HSVToRGB(h, s * 0.5f, 0.75f);
            targetBottom = Color.HSVToRGB(h, s * 0.6f, 0.45f);

            if (!initialized)
            {
                currentTop = targetTop;
                currentMiddle = targetMiddle;
                currentBottom = targetBottom;
                ApplyColors();
                initialized = true;
            }
        }

        private void Update()
        {
            if (runtimeMaterial == null || !initialized) return;

            currentTop = Color.Lerp(currentTop, targetTop, Time.deltaTime * transitionSpeed);
            currentMiddle = Color.Lerp(currentMiddle, targetMiddle, Time.deltaTime * transitionSpeed);
            currentBottom = Color.Lerp(currentBottom, targetBottom, Time.deltaTime * transitionSpeed);
            ApplyColors();
        }

        private void ApplyColors()
        {
            runtimeMaterial.SetColor(TopColorId, currentTop);
            runtimeMaterial.SetColor(MiddleColorId, currentMiddle);
            runtimeMaterial.SetColor(BottomColorId, currentBottom);
        }
    }
}
