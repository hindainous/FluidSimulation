Shader "Custom/ColorChangeShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0, 0, 1) // Red by default
        _ColorParameter ("Color Parameter", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _BaseColor;
            float _ColorParameter;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Interpolate between BaseColor and red based on ColorParameter
                fixed4 redColor = fixed4(1, 0, 0, 1); // Red color
                fixed4 resultColor = lerp(_BaseColor, redColor, _ColorParameter);
                return resultColor;
            }
            ENDCG
        }
    }
}