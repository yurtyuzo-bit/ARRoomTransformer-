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
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NoiseTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _BaseColor;
        fixed4 _StainColor;
        float4 _CarpetTiling;
        half _StainStrength;
        half _WearAmount;
        half _Metallic;
        half _Smoothness;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 tiledUV = IN.uv_MainTex * _CarpetTiling.xy;
            fixed4 c = tex2D(_MainTex, tiledUV) * _BaseColor;

            // Stain
            half stainNoise = tex2D(_NoiseTex, IN.uv_MainTex * 1.5).r;
            half stainMask = smoothstep(0.5 - _StainStrength, 0.5 + _StainStrength, stainNoise);
            c.rgb = lerp(c.rgb, _StainColor.rgb, stainMask * _StainStrength);

            // Wear
            half wearNoise = tex2D(_NoiseTex, IN.uv_MainTex * 0.7).g;
            c.rgb = lerp(c.rgb, c.rgb * 1.15, wearNoise * _WearAmount);

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
