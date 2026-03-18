Shader "Stopka/GradientSkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.05, 0.05, 0.2, 1)
        _MiddleColor ("Middle Color", Color) = (0.15, 0.1, 0.3, 1)
        _BottomColor ("Bottom Color", Color) = (0.1, 0.05, 0.15, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _MiddleColor;
                half4 _BottomColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  gradientY  : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Unity renders skybox on a unit cube/sphere — use object Y as gradient
                output.gradientY = saturate(input.positionOS.y * 0.5 + 0.5);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float y = input.gradientY;
                // Two-stop gradient: bottom→middle (lower half), middle→top (upper half)
                half4 color = y < 0.5
                    ? lerp(_BottomColor, _MiddleColor, saturate(y * 2.0))
                    : lerp(_MiddleColor, _TopColor, saturate(y * 2.0 - 1.0));
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
