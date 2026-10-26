Shader "Gate/RiftShockwave"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,0.6,1,1)
        _Prog ("Progress", Range(0,1)) = 0
        _Width ("Trail Width", Range(0.01,1)) = 0.25
        _Fade ("Fade Curve", Float) = 1.5
        _Flash ("Inner Glow", Range(0,1)) = 0.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha One
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct A { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; };

            float4 _Color;
            float _Prog, _Width, _Fade, _Flash;

            V vert(A a)
            {
                V o;
                o.pos = TransformObjectToHClip(a.pos.xyz);
                o.col = a.col;
                o.uv = a.uv;
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float r = length((i.uv - 0.5) * 2.0);          // 0 กลาง, 1 ขอบรัศมีระเบิด

                float front = 1.0 - smoothstep(_Prog, _Prog + 0.02, r);       // ขอบหน้าคม
                float trail = pow(smoothstep(_Prog - _Width, _Prog, r), 2.0);  // หางไล่จางเข้าด้านใน
                float a = front * trail;

                float glow = front * (1.0 - smoothstep(0.0, max(_Prog, 0.01), r)) * _Flash;
                a = saturate(a + glow);

                a *= pow(1.0 - _Prog, _Fade);                                  // จางลงตอนขยายออก

                return half4(_Color.rgb * i.col.rgb, a * _Color.a * i.col.a);
            }
            ENDHLSL
        }
    }
}