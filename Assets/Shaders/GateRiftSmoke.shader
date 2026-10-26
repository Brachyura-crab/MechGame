Shader "Gate/RiftSmoke"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        [HideInInspector] _SrcBlend ("Src", Float) = 5
        [HideInInspector] _DstBlend ("Dst", Float) = 10
        _T ("Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _Aspect ("Aspect (width/height)", Float) = 1
        _Rise ("Noise Rise Speed", Float) = 0.35
        _SwayAmp ("Sway Amount", Float) = 0.12
        _SwaySpeed ("Sway Speed", Float) = 0.8
        _SwayFreq ("Sway Frequency", Float) = 3
        _BaseWidth ("Base Width", Float) = 0.3
        _TipWidth ("Tip Width", Float) = 0.1
        _NoiseScale ("Noise Scale", Float) = 3
        _Ragged ("Ragged", Float) = 0.9
        _Density ("Dot Density", Float) = 1
        _DotStart ("Dots Start Height", Range(0,1)) = 0.45
        _DotScale ("Dot Cells Per Height", Float) = 14
        _DotRise ("Dot Rise Speed", Float) = 0.15
        _DotSoft ("Dot Softness", Float) = 12
        _TipAlpha ("Alpha At Tip", Range(0,1)) = 0.15
        _FadeStart ("Fade Start Height", Range(0,1)) = 0.1
        _FadeEnd ("Fade End Height", Range(0,1)) = 0.9
        _FadePower ("Fade Curve", Float) = 1
        _FadeNoise ("Fade Noise", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct A { float4 pos : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; };

            float4 _Color;
            float _T, _Seed, _Aspect, _Rise, _SwayAmp, _SwaySpeed, _SwayFreq;
            float _BaseWidth, _TipWidth, _NoiseScale, _Ragged, _Density;
            float _DotStart, _DotScale, _DotRise, _DotSoft;
            float _TipAlpha;
            float _FadeStart, _FadeEnd, _FadePower, _FadeNoise;

            V vert(A a)
            {
                V o;
                o.pos = TransformObjectToHClip(a.pos.xyz);
                o.col = a.col;
                o.uv = a.uv;
                return o;
            }

            float h21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(h21(i), h21(i + float2(1, 0)), f.x),
                            lerp(h21(i + float2(0, 1)), h21(i + float2(1, 1)), f.x), f.y);
            }

            float fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                for (int k = 0; k < 4; k++) { s += a * vnoise(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return s;
            }

            half4 frag(V i) : SV_Target
            {
                float h = i.uv.y;

                // สบัดไปมา: ปลายแกว่งมากกว่าโคน
                float sway = sin(_T * _SwaySpeed + h * _SwayFreq + _Seed * 6.28)
                           + 0.5 * sin(_T * _SwaySpeed * 1.7 + h * _SwayFreq * 1.9 + _Seed * 3.1);
                float x = (i.uv.x - 0.5) - sway * _SwayAmp * pow(h, 1.3);

                // รูปทรงเส้น: ความกว้างเปลี่ยนจากโคนไปปลาย
                float w = lerp(_BaseWidth, _TipWidth, pow(h, 0.7));
                float d = abs(x) / max(w, 1e-3);
                float shape = saturate(1.0 - d);

                // noise ไหลขึ้น ทำให้ขอบขาดวิ่น
                float2 np = float2(x * _Aspect, h - _T * _Rise) * _NoiseScale + _Seed * 10.0;
                float n = fbm(np);
                float dens = shape + (n - 0.5) * _Ragged;

                float a = smoothstep(0.3, 0.6, dens);

                // จางหายตามความสูง
                float f = saturate((h - _FadeStart) / max(_FadeEnd - _FadeStart, 1e-3));
                f = pow(f, max(_FadePower, 0.01));
                f = saturate(f + (n - 0.5) * _FadeNoise * f * (1.0 - f) * 4.0);
                a *= lerp(1.0, _TipAlpha, f);

                a *= smoothstep(0.0, 0.06, h);   // โคนโผล่ออกมานุ่มๆ
                a *= _Color.a * i.col.a;

                return half4(_Color.rgb * i.col.rgb, saturate(a));
            }
            ENDHLSL
        }
    }
}