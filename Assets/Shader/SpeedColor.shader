Shader "Custom/SpeedMultiColorGradientInstanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MinSpeed ("Min Speed", Float) = 0
        _MaxSpeed ("Max Speed", Float) = 10
        _ColorIntensity ("Color Intensity", Range(0, 2)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _MinSpeed;
            float _MaxSpeed;
            float _ColorIntensity;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Speed)
            UNITY_INSTANCING_BUFFER_END(Props)

            // Function to create smooth color gradient
            float3 GetSpeedColor(float t)
            {
                // Define color stops (0-1 range)
                float3 blue = float3(0.0, 0.0, 1.0);
                float3 green = float3(0.0, 1.0, 0.0);
                float3 yellow = float3(1.0, 1.0, 0.0);
                float3 red = float3(1.0, 0.0, 0.0);
                
                // Blend between colors based on speed ratio
                if (t < 0.33)
                {
                    return lerp(blue, green, t * 3.0);
                }
                else if (t < 0.66)
                {
                    return lerp(green, yellow, (t - 0.33) * 3.0);
                }
                else
                {
                    return lerp(yellow, red, (t - 0.66) * 3.0);
                }
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Get instance-specific speed
                float speed = UNITY_ACCESS_INSTANCED_PROP(Props, _Speed);
                
                // Normalize speed (0-1 range)
                float t = saturate((speed - _MinSpeed) / (_MaxSpeed - _MinSpeed));
                
                // Get color from gradient
                float3 color = GetSpeedColor(t);
                
                // Apply intensity and set alpha to 1
                o.color = float4(color * _ColorIntensity, 1.0);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                return col;
            }
            ENDCG
        }
    }
}