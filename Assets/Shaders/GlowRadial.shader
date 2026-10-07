Shader "Mech/GlowRadial"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,1)
        _CoreSize ("Core Size", Range(0,1)) = 0.3
        _Intensity ("HDR Intensity", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _CoreSize;
                float _Intensity;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float dist = distance(i.uv, float2(0.5, 0.5)) * 2.0; // 0 กลาง, 1 ขอบ
                float core = 1.0 - saturate((dist - 0) / max(_CoreSize, 0.001));   // 1 ตรงกลาง ไล่ลงถึงขอบ core
                float glow = 1.0 - saturate((dist - _CoreSize) / max(1.0 - _CoreSize, 0.001)); // ไล่จาง core -> ขอบนอก

                float3 rgb = lerp(_Color.rgb, float3(1,1,1), core) * _Intensity;
                float alpha = saturate(max(core, glow));

                return float4(rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}