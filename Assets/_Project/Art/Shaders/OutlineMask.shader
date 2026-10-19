// 외곽선 마스크: 대상 렌더러를 깊이 검사 없이 한 가지 색(_MaskColor)으로 그린다. (OutlineRendererFeature 전용)
// R = 조준 외곽선, G = 수류탄 표시 외곽선. 겹치면 더해진다.
Shader "Hidden/GuardVsMannequin/OutlineMask"
{
    Properties
    {
        _MaskColor ("Mask Color", Color) = (1, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Name "OutlineMask"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _MaskColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _MaskColor;
            }
            ENDHLSL
        }
    }
}
