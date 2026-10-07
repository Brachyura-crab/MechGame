Shader "Mech/ShadowGradient"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.5
        _GradientHeight ("Gradient Height", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend DstColor Zero
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _ShadowStrength;
                float _GradientHeight;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (tex.a < 0.01) discard;   // นอกรูปทรงลำตัว ไม่วาดอะไรเลย

                // y: 0 = ล่าง, 1 = บน -> มืดสุดที่ล่าง ไล่จางขึ้นถึง _GradientHeight แล้วปกติ (ขาว = ไม่ลดความสว่าง)
                float darkness = 1.0 - saturate(i.uv.y / max(_GradientHeight, 0.001));
                float mul = 1.0 - darkness * _ShadowStrength;

                return float4(mul, mul, mul, tex.a);
            }
            ENDHLSL
        }
    }
}