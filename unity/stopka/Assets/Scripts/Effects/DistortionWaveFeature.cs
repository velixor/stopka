using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Stopka
{
    public class DistortionWavePass : ScriptableRenderPass
    {
        private const string PassName = "DistortionWavePass";
        private static readonly int WaveStrengthId = Shader.PropertyToID("_WaveStrength");

        private Material m_Material;

        public void Setup(Material mat)
        {
            m_Material = mat;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Skip when effect is inactive (zero GPU cost)
            if (m_Material == null || m_Material.GetFloat(WaveStrengthId) < 0.001f)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = $"CameraColor-{PassName}";
            destinationDesc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

            RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_Material, 0);
            renderGraph.AddBlitPass(para, passName: PassName);

            resourceData.cameraColor = destination;
        }
    }

    public class DistortionWaveFeature : ScriptableRendererFeature
    {
        [Tooltip("The distortion wave material.")]
        public Material material;

        private DistortionWavePass m_Pass;

        public override void Create()
        {
            m_Pass = new DistortionWavePass();
            m_Pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null)
                return;

            m_Pass.Setup(material);
            renderer.EnqueuePass(m_Pass);
        }
    }
}
