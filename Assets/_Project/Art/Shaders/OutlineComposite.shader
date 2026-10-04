// 외곽선 합성: 마스크(_BlitTexture)의 가장자리를 _Thickness 픽셀만큼 넓혀, 대상 바깥쪽 테두리만 화면에 덧그린다. (OutlineRendererFeature 전용)
// R 채널 = 조준(_AimColor), G 채널 = 수류탄 표시(_RevealColor). 둘 다 있으면 수류탄 표시가 우선한다.
Shader "Hidden/GuardVsMannequin/OutlineComposite"
{
    Properties
    {
        _AimColor ("Aim Color", Color) = (1, 0.85, 0.2, 1)
        _RevealColor ("Reveal Color", Color) = (1, 0.2, 0.15, 1)
        _Thickness ("Thickness (px)", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "OutlineComposite"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 _AimColor;
            half4 _RevealColor;
            float _Thickness;
            float4 _MaskTexelSize;

            // 주변 12방향을 두 거리(절반, 전체)로 살펴 가장 큰 마스크 값을 찾는다.
            static const int SampleCount = 12;

            half2 SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rg;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half2 center = SampleMask(uv);

                half2 around = 0;
                float2 step = _MaskTexelSize.xy * _Thickness;
                [unroll]
                for (int i = 0; i < SampleCount; i++)
                {
                    float angle = i * (6.2831853 / SampleCount);
                    float2 direction = float2(cos(angle), sin(angle));
                    around = max(around, SampleMask(uv + direction * step));
                    around = max(around, SampleMask(uv + direction * step * 0.5));
                }

                // 대상 안쪽(center)은 지우고 바깥 테두리만 남긴다.
                half aim = saturate(around.r - center.r);
                half reveal = saturate(around.g - center.g);
                if (reveal > 0)
                    return half4(_RevealColor.rgb, _RevealColor.a * reveal);
                if (aim > 0)
                    return half4(_AimColor.rgb, _AimColor.a * aim);
                return 0;
            }
            ENDHLSL
        }
    }
}
