Shader "Stopka/BlockWave"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _WAVE_BULGE _WAVE_SQUEEZE _WAVE_JITTER
            #pragma multi_compile _ _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END

            // Global wave parameters (set from C#)
            float _WaveFrontY;
            float _WaveWidth;
            float _WaveStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float  waveAmount : TEXCOORD1;
            };

            // Hash function for jitter
            float hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);

                // How close is this vertex to the wave front (0 = far, 1 = right on it)
                float dist = abs(posWS.y - _WaveFrontY);
                float wave = saturate(1.0 - dist / max(_WaveWidth, 0.001));
                wave *= wave; // sharper falloff
                float displacement = wave * _WaveStrength;

                #if defined(_WAVE_BULGE)
                    // Push vertices outward on XZ plane
                    float3 centerWS = TransformObjectToWorld(float3(0, input.positionOS.y, 0));
                    float3 dirXZ = posWS - centerWS;
                    dirXZ.y = 0;
                    float lenXZ = length(dirXZ);
                    if (lenXZ > 0.001)
                        posWS.xz += (dirXZ.xz / lenXZ) * displacement;

                #elif defined(_WAVE_SQUEEZE)
                    // Compress Y, expand XZ
                    float3 centerWS2 = TransformObjectToWorld(float3(0, input.positionOS.y, 0));
                    float3 dirXZ2 = posWS - centerWS2;
                    dirXZ2.y = 0;
                    float lenXZ2 = length(dirXZ2);
                    if (lenXZ2 > 0.001)
                        posWS.xz += (dirXZ2.xz / lenXZ2) * displacement;
                    // Squash toward block center Y
                    float3 objCenter = TransformObjectToWorld(float3(0, 0, 0));
                    posWS.y += (objCenter.y - posWS.y) * displacement * 0.5;

                #elif defined(_WAVE_JITTER)
                    // Random offset based on position + time
                    float3 seed = posWS * 10.0 + _Time.y * 20.0;
                    float3 jitter = float3(
                        hash(seed) - 0.5,
                        hash(seed + 1.0) - 0.5,
                        hash(seed + 2.0) - 0.5
                    );
                    posWS += jitter * displacement * 0.5;
                #endif

                output.positionCS = TransformWorldToHClip(posWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.waveAmount = wave;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Simple directional lighting
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = _BaseColor.rgb * mainLight.color * (NdotL * 0.7 + 0.3);

                // Emission: combine manual emission + wave glow
                half3 emission = half3(0, 0, 0);
                #if defined(_EMISSION)
                    emission += _EmissionColor.rgb;
                #endif

                // Wave glow (additive, based on proximity to wave front)
                emission += _BaseColor.rgb * input.waveAmount * _WaveStrength * 3.0;

                return half4(diffuse + emission, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
