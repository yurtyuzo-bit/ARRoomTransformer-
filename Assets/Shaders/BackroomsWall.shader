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
        _DampHeight ("Nem Yüksekliği", Range(0, 5)) = 0.3 // world space height
        _Metallic ("Metalik", Range(0, 1)) = 0.0
        _Smoothness ("Pürüzsüzlük", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NoiseTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        fixed4 _BaseColor;
        fixed4 _DampColor;
        float4 _WallpaperTiling;
        half _NoiseStrength;
        half _DamageAmount;
        half _DampAmount;
        half _DampHeight;
        half _Metallic;
        half _Smoothness;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Apply tiling to MainTex UV
            float2 tiledUV = IN.uv_MainTex * _WallpaperTiling.xy;

            // Base color
            fixed4 c = tex2D(_MainTex, tiledUV) * _BaseColor;

            // Noise for damage/dirt
            fixed noise = tex2D(_NoiseTex, IN.uv_MainTex * 3.0).r;
            c.rgb = lerp(c.rgb, c.rgb * (1.0 - _DamageAmount), noise * _NoiseStrength);

            // Dampness at the bottom (worldPos.y)
            // saturate restricts to 0..1
            float heightFactor = saturate(1.0 - (IN.worldPos.y / max(_DampHeight, 0.001)));
            c.rgb = lerp(c.rgb, _DampColor.rgb, heightFactor * _DampAmount);

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
