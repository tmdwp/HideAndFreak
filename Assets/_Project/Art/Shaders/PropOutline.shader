// 변신 가능 사물 외곽선 (URP). 메시를 법선 방향으로 화면 기준 일정한 픽셀 두께만큼 부풀리고 뒷면만 그린다.
// 원래 사물 위에 덧그려서 가장자리에 얇은 선이 보이게 한다. (각진 메시는 모서리 선이 끊겨 보일 수 있음)
Shader "GuardVsMannequin/PropOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.9, 0.4, 1)
        _OutlineWidth ("Outline Width (pixels)", Range(0.5, 6)) = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+10" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            // 클립 공간에서 법선의 화면 방향으로 정점을 밀어 두께를 픽셀 단위로 일정하게 한다.
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                float2 direction = normalize(normalCS + 1e-5);
                positionCS.xy += direction * _OutlineWidth * 2.0 / _ScreenParams.xy * positionCS.w;
                output.positionCS = positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
