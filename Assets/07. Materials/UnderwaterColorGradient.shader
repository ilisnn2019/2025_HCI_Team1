Shader "Custom/UnderwaterRadialLight"
{
    Properties
    {
        _ShallowColor("Shallow Water Color", Color) = (0.1, 0.5, 0.8, 1)
        _DeepColor("Deep Water Color", Color) = (0.0, 0.05, 0.2, 1)
        _Center("Gradient Center (World Pos)", Vector) = (0,0,0,0)
        _Radius("Gradient Radius", Float) = 50.0
        _DepthEnd("Depth Limit Y", Float) = -30.0
        _NoiseTex("Distortion Noise", 2D) = "white" {}
        _NoiseStrength("Noise Strength", Float) = 0.05
        _FlowSpeed("Flow Speed", Float) = 0.1
        _LightColor("Light Shaft Color", Color) = (0.5, 0.8, 1.0, 1)
        _LightIntensity("Light Shaft Intensity", Float) = 1.0
        _LightFalloff("Light Shaft Falloff", Float) = 2.0
        _Transparency("Transparency", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalRenderPipeline" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _Center;
                float _Radius;
                float _DepthEnd;
                float _NoiseStrength;
                float _FlowSpeed;
                float4 _LightColor;
                float _LightIntensity;
                float _LightFalloff;
                float _Transparency;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 기본 색 (수심 + 반경 기반)
                float3 toCenter = i.worldPos - _Center.xyz;
                float radialDist = length(toCenter.xz);
                float verticalDepth = saturate((i.worldPos.y - _DepthEnd) / (0 - _DepthEnd));
                float radialT = saturate(1.0 - (radialDist / _Radius));
                float combined = saturate((verticalDepth * 0.6 + radialT * 0.4));

                float2 flowUV = i.uv + float2(0, _Time.y * _FlowSpeed);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flowUV).r;
                combined += (noise - 0.5) * _NoiseStrength;

                float3 baseColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, combined);

                // Directional Light 방향 기반 빛 기둥 효과
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float lightFactor = saturate(dot(normalize(-lightDir), float3(0, -1, 0))); // 아래로 향한 빛
                lightFactor = pow(lightFactor, _LightFalloff) * _LightIntensity;

                float3 lightColor = _LightColor.rgb * lightFactor;

                float3 finalColor = baseColor + lightColor;
                return float4(finalColor, _Transparency);
            }

            ENDHLSL
        }
    }
}
