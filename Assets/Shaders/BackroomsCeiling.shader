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
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _DirtTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _BaseColor;
        fixed4 _GridColor;
        float4 _GridTiling;
        half _GridWidth;
        half _DirtAmount;
        half _Metallic;
        half _Smoothness;
        fixed4 _EmissionColor;
        half _EmissionStrength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 gridUV = IN.uv_MainTex * _GridTiling.xy;
            fixed4 c = tex2D(_MainTex, gridUV) * _BaseColor;

            // Grid
            float2 gridFrac = frac(gridUV);
            float gridLine = step(gridFrac.x, _GridWidth) + step(1.0 - _GridWidth, gridFrac.x)
                           + step(gridFrac.y, _GridWidth) + step(1.0 - _GridWidth, gridFrac.y);
            gridLine = saturate(gridLine);
            c.rgb = lerp(c.rgb, _GridColor.rgb, gridLine);

            // Dirt
            half dirt = tex2D(_DirtTex, IN.uv_MainTex * 2.0).r;
            c.rgb = lerp(c.rgb, c.rgb * 0.7, dirt * _DirtAmount);

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Emission = _EmissionColor.rgb * _EmissionStrength * (1.0 - gridLine);
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
