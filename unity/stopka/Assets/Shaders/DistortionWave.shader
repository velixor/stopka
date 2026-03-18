Shader "Stopka/DistortionWave"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off

        Pass
        {
            Name "DistortionWavePass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float2 _WaveCenter;
            float _WaveRadius;
            float _WaveWidth;
            float _WaveStrength;

            float4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float2 dir = uv - _WaveCenter;
                float dist = length(dir);

                // Ring-shaped distortion at _WaveRadius, width controlled by _WaveWidth
                float wave = 1.0 - saturate(abs(dist - _WaveRadius) / _WaveWidth);
                wave *= wave; // sharper falloff

                // Offset UV radially
                float2 offset = normalize(dir + 0.0001) * wave * _WaveStrength;
                float2 distortedUV = uv + offset;

                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, distortedUV, _BlitMipLevel);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
