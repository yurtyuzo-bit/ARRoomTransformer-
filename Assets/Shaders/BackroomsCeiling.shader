Shader "ARRoomTransformer/BackroomsCeiling"
{
    Properties
    {
        _BaseColor ("Tavan Rengi", Color) = (0.92, 0.90, 0.82, 1.0)
        _MainTex ("Tavan Texture", 2D) = "white" {}
        _GridColor ("Panel Çizgi Rengi", Color) = (0.75, 0.73, 0.65, 1.0)
        _GridWidth ("Panel Çizgi Kalınlığı", Range(0.001, 0.05)) = 0.015
        _GridTiling ("Panel Tekrarı", Vector) = (3, 3, 0, 0)
        _DirtTex ("Kir Texture", 2D) = "white" {}
        _DirtAmount ("Kirlilik", Range(0, 1)) = 0.1
        _Metallic ("Metalik", Range(0, 1)) = 0.0
        _Smoothness ("Pürüzsüzlük", Range(0, 1)) = 0.25
        _EmissionColor ("Floresan Rengi", Color) = (0.95, 0.95, 0.85, 1.0)
        _EmissionStrength ("Floresan Yoğunluğu", Range(0, 2)) = 0.3
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
            TEXTURE2D(_DirtTex);    SAMPLER(sampler_DirtTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float _GridWidth;
                float4 _GridTiling;
                float _DirtAmount;
                float _Metallic;
                float _Smoothness;
                float4 _EmissionColor;
                float _EmissionStrength;
                float4 _MainTex_ST;
                float4 _DirtTex_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 gridUV = input.uv * _GridTiling.xy;

                // Tavan paneli base rengi
                half4 ceilingColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, gridUV) * _BaseColor;

                // Panel grid çizgileri
                float2 gridFrac = frac(gridUV);
                float gridLine = step(gridFrac.x, _GridWidth) + step(1.0 - _GridWidth, gridFrac.x)
                               + step(gridFrac.y, _GridWidth) + step(1.0 - _GridWidth, gridFrac.y);
                gridLine = saturate(gridLine);
                ceilingColor.rgb = lerp(ceilingColor.rgb, _GridColor.rgb, gridLine);

                // Kir efekti
                half dirt = SAMPLE_TEXTURE2D(_DirtTex, sampler_DirtTex, input.uv * 2.0).r;
                ceilingColor.rgb = lerp(ceilingColor.rgb, ceilingColor.rgb * 0.7, dirt * _DirtAmount);

                // Lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = ceilingColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.emission = _EmissionColor.rgb * _EmissionStrength * (1.0 - gridLine);
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
