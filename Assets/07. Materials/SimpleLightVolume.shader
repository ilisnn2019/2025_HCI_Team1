Shader "Custom/VolumetricLightSimple"
{
    Properties
    {
        _Color("Light Color", Color) = (0.5, 0.8, 1.0, 0.5)
        _Intensity("Intensity", Float) = 1.0
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _Speed("Flow Speed", Float) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One OneMinusSrcAlpha
        ZWrite On
        ZTest LEqual
        Cull Back

        Pass
        {
           HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _NoiseTex;
            float4 _Color;
            float _Intensity;
            float _Speed;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float noise = tex2D(_NoiseTex, i.uv + float2(0, _Time.y * _Speed)).r;
                float alpha = smoothstep(0.0, 1.0, i.uv.y) * noise;
                return _Color * _Intensity * alpha;
            }
            ENDHLSL
        }
    }
}
