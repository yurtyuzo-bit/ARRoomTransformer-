Shader "ARRoomTransformer/BackroomsFloor"
{
    Properties
    {
        _BaseColor ("Halı Rengi", Color) = (0.45, 0.40, 0.30, 1.0)
        _MainTex ("Halı Texture", 2D) = "white" {}
        _NoiseTex ("Leke Texture", 2D) = "white" {}
        _StainColor ("Leke Rengi", Color) = (0.3, 0.25, 0.15, 1.0)
        _StainStrength ("Leke Yoğunluğu", Range(0, 1)) = 0.25
        _CarpetTiling ("Halı Tekrarı", Vector) = (4, 4, 0, 0)
        _WearAmount ("Yıpranma", Range(0, 1)) = 0.4
        _Metallic ("Metalik", Range(0, 1)) = 0.0
        _Smoothness ("Pürüzsüzlük", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);   SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _StainColor;
                float _StainStrength;
                float4 _CarpetTiling;
                float _WearAmount;
                float _Metallic;
                float _Smoothness;
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv * _CarpetTiling.xy;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Halı base rengi
                half4 carpetColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;

                // Leke efekti
                half stainNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 1.5).r;
                half stainMask = smoothstep(0.5 - _StainStrength, 0.5 + _StainStrength, stainNoise);
                carpetColor.rgb = lerp(carpetColor.rgb, _StainColor.rgb, stainMask * _StainStrength);

                // Yıpranma — hafif renk solması
                half wearNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 0.7).g;
                carpetColor.rgb = lerp(carpetColor.rgb, carpetColor.rgb * 1.15, wearNoise * _WearAmount);

                // Lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = carpetColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;

                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
