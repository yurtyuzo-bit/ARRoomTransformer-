Shader "ARRoomTransformer/BackroomsWall"
{
    Properties
    {
        _BaseColor ("Duvar Rengi", Color) = (0.85, 0.78, 0.55, 1.0)
        _MainTex ("Duvar Kağıdı Texture", 2D) = "white" {}
        _NoiseTex ("Gürültü Texture (Kirlilik)", 2D) = "white" {}
        _NoiseStrength ("Kirlilik Yoğunluğu", Range(0, 1)) = 0.15
        _WallpaperTiling ("Duvar Kağıdı Tekrarı", Vector) = (2, 2, 0, 0)
        _DamageAmount ("Hasar/Yıpranma", Range(0, 1)) = 0.3
        _DampColor ("Nem Rengi", Color) = (0.6, 0.55, 0.35, 1.0)
        _DampAmount ("Nem Miktarı", Range(0, 1)) = 0.2
        _DampHeight ("Nem Yüksekliği", Range(0, 1)) = 0.3
        _Metallic ("Metalik", Range(0, 1)) = 0.0
        _Smoothness ("Pürüzsüzlük", Range(0, 1)) = 0.15
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
                float4 _WallpaperTiling;
                float _NoiseStrength;
                float _DamageAmount;
                float4 _DampColor;
                float _DampAmount;
                float _DampHeight;
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
                output.uv = input.uv * _WallpaperTiling.xy;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Temel duvar kağıdı rengi
                half4 wallColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;

                // Gürültü (kirlilik/yıpranma efekti)
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 3.0).r;
                wallColor.rgb = lerp(wallColor.rgb, wallColor.rgb * (1.0 - _DamageAmount), noise * _NoiseStrength);

                // Alt kısımda nem efekti
                float heightFactor = saturate(1.0 - (input.positionWS.y / _DampHeight));
                wallColor.rgb = lerp(wallColor.rgb, _DampColor.rgb, heightFactor * _DampAmount);

                // Basit lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = wallColor.rgb;
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

        // Shadow caster pass
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
