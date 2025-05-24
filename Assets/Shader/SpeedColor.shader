Shader "Custom/SpeedColorStandardLike"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(Speed Colors)]
        _MinSpeed ("Min Speed", Float) = 0
        _MaxSpeed ("Max Speed", Float) = 10
        _ColorIntensity ("Color Intensity", Range(0, 2)) = 1
        
        [Header(Shading)]
        _ShadingContrast ("Shading Contrast", Range(0.5, 2)) = 1.2
        _RimPower ("Rim Power", Range(0, 5)) = 2.0
        _RimIntensity ("Rim Intensity", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma instancing_options assumeuniformscaling
        #pragma target 3.0

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        
        float _MinSpeed;
        float _MaxSpeed;
        float _ColorIntensity;
        float _ShadingContrast;
        float _RimPower;
        float _RimIntensity;

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _Speed)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        float3 GetSpeedColor(float t)
        {
            float3 blue = float3(0.0, 0.0, 1.0);
            float3 green = float3(0.0, 1.0, 0.0);
            float3 yellow = float3(1.0, 1.0, 0.0);
            float3 red = float3(1.0, 0.0, 0.0);
            
            if (t < 0.33)
                return lerp(blue, green, t * 3.0);
            else if (t < 0.66)
                return lerp(green, yellow, (t - 0.33) * 3.0);
            else
                return lerp(yellow, red, (t - 0.66) * 3.0);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base color from texture
            fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex);
            
            // Get speed-based color
            float speed = UNITY_ACCESS_INSTANCED_PROP(Props, _Speed);
            float t = saturate((speed - _MinSpeed) / (_MaxSpeed - _MinSpeed));
            float3 speedColor = GetSpeedColor(t) * _ColorIntensity;
            
            // Combine with texture
            o.Albedo = texColor.rgb * speedColor;
            
            // Standard material properties
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = texColor.a;
            
            // Enhanced shading
            float3 normal = WorldNormalVector(IN, o.Normal);
            float NdotV = saturate(dot(normal, normalize(IN.viewDir)));
            
            // Rim lighting
            float rim = pow(1.0 - NdotV, _RimPower) * _RimIntensity;
            o.Emission = o.Albedo * rim;
            
            // Adjust shading contrast
            o.Albedo = pow(o.Albedo, _ShadingContrast);
        }
        ENDCG
    }
    
    FallBack "Standard"
}