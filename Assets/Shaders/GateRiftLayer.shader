Shader "Gate/RiftLayer"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _SrcBlend ("Src", Float) = 5
        [HideInInspector] _DstBlend ("Dst", Float) = 10

        _T ("Time", Float) = 0
        _Aspect ("Aspect", Float) = 1.39

        _WarpStrength ("Warp Strength", Float) = 0.02
        _WarpScale ("Warp Scale", Float) = 3
        _WarpSpeed ("Warp Speed", Float) = 0.4
        _DriftSpeed ("Drift Speed", Float) = 0.15

        _ErodeMix ("Erode Mix", Range(0,1)) = 0.5
        _ErodeAmount ("Erode Amount", Range(0,1)) = 0.45
        _ErodeScale ("Erode Scale", Float) = 5
        _ErodeSpeed ("Erode Speed", Float) = 0.2
        _ErodeSoft ("Erode Soft", Range(0.01,0.5)) = 0.2
        _CoreProtect ("Core Protect", Range(0,1)) = 0.6

        _Glitch ("Glitch", Range(0,2)) = 0
        _GlitchSeed ("Glitch Seed", Float) = 0
        _GlitchBands ("Glitch Bands", Float) = 36
        _GlitchShift ("Glitch Shift", Float) = 0.06
        _GlitchSplit ("Glitch Split", Float) = 0.006
        _GlitchDensity ("Glitch Density", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "RiftLayer"

            Blend [_SrcBlend] [_DstBlend]
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float _T, _Aspect;
            float _WarpStrength, _WarpScale, _WarpSpeed, _DriftSpeed;
            float _ErodeMix, _ErodeAmount, _ErodeScale, _ErodeSpeed, _ErodeSoft, _CoreProtect;
            float _Glitch, _GlitchSeed, _GlitchBands, _GlitchShift, _GlitchSplit, _GlitchDensity;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 3; k++)
                {
                    v += a * vnoise(p);
                    p = p * 2.02 + float2(17.1, 9.2);
                    a *= 0.5;
                }
                return v / 0.875;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float2 asp = float2(_Aspect, 1.0);

                // 1) บิด UV ให้ขอบพลิ้ว + ลายเลื่อนขึ้น
                float2 np = uv * asp * _WarpScale + float2(0.0, -_T * _DriftSpeed);
                float2 w = float2(fbm(np + _T * _WarpSpeed * 0.35),
                                  fbm(np + float2(37.2, 11.7) - _T * _WarpSpeed * 0.3)) - 0.5;
                uv += w * _WarpStrength * 2.0;

                // 2) glitch: แถบแนวนอนเลื่อนสลับ + แยกสี
                float g = _Glitch;
                float band = floor(uv.y * _GlitchBands);
                float h = hash21(float2(band, _GlitchSeed));
                float act = step(1.0 - _GlitchDensity * g, h);
                float shift = (hash21(float2(band + 7.0, _GlitchSeed + 3.0)) - 0.5) * 2.0 * _GlitchShift * g * act;
                uv.x += shift;
                float split = _GlitchSplit * g * (0.5 + act);

                float4 sc = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float4 sr = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(split, 0.0));
                float4 sb = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(split, 0.0));

                float3 premul = float3(sr.r * sr.a, sc.g * sc.a, sb.b * sb.a);
                float a = max(sc.a, max(sr.a, sb.a));
                float3 rgb = a > 0.0001 ? premul / a : sc.rgb;

                // 3) กัดขอบเป็นปื้นโปร่งแสง (ป้องกันส่วนทึบตรงกลาง)
                float n = fbm(uv * asp * _ErodeScale + float2(0.0, -_T * _ErodeSpeed) + 5.3);
                float m = smoothstep(_ErodeAmount - _ErodeSoft, _ErodeAmount + _ErodeSoft, n);
                float core = saturate((a - 0.6) / 0.4) * _CoreProtect;
                a *= lerp(1.0, m, _ErodeMix * (1.0 - core));

                return half4(rgb * _Color.rgb * i.color.rgb, a * _Color.a * i.color.a);
            }
            ENDHLSL
        }
    }
}