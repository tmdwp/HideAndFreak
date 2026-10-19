using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 빌드 시간(셰이더 변형 수)과 실행 성능을 위해 이 게임에 필요 없는 렌더링 기능을 끈다.
    /// - PC URP 에셋: 라이트 쿠키, 라이트 레이어, LOD 크로스페이드, 반사 프로브 블렌딩·박스 투영, 부드러운 그림자 끄기, 그림자 캐스케이드 4 → 2
    ///   (사냥 시간 손전등에 필요한 추가 조명 그림자는 유지)
    /// - PC 렌더러: SSAO 끄기
    /// - Windows 그래픽 API: Direct3D11만 사용 (DX12 중복 컴파일 방지, Steam 오버레이 호환)
    /// 여러 번 실행해도 결과는 같다.
    /// </summary>
    public static class BuildOptimizationSettings
    {
        private const string PcPipelinePath = "Assets/Settings/PC_RPAsset.asset";
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";

        /// <summary>
        /// 모든 최적화 설정을 적용한다.
        /// </summary>
        [MenuItem(EditorMenus.Root + "빌드 최적화 설정 적용")]
        public static void Apply()
        {
            ApplyPipelineSettings();
            DisableSsao();
            UseDirect3D11Only();
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildOptimizationSettings] 적용 완료. 다음 빌드부터 셰이더 변형 수가 줄어듭니다. (첫 빌드는 새 변형을 다시 컴파일)");
        }

        /// <summary>
        /// PC URP 에셋에서 쓰지 않는 기능을 끈다.
        /// </summary>
        private static void ApplyPipelineSettings()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PcPipelinePath);
            if (pipeline == null)
            {
                Debug.LogError($"[BuildOptimizationSettings] URP 에셋을 찾을 수 없습니다: {PcPipelinePath}");
                return;
            }

            var serialized = new SerializedObject(pipeline);
            SetBool(serialized, "m_SupportsLightCookies", false);
            SetBool(serialized, "m_SupportsLightLayers", false);
            SetBool(serialized, "m_EnableLODCrossFade", false);
            SetBool(serialized, "m_ReflectionProbeBlending", false);
            SetBool(serialized, "m_ReflectionProbeBoxProjection", false);
            SetBool(serialized, "m_SoftShadowsSupported", false);
            SetInt(serialized, "m_ShadowCascadeCount", 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        /// <summary>
        /// PC 렌더러의 SSAO 기능을 끈다.
        /// </summary>
        private static void DisableSsao()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
            if (renderer == null)
            {
                Debug.LogWarning($"[BuildOptimizationSettings] 렌더러를 찾을 수 없습니다: {PcRendererPath}");
                return;
            }

            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures.Where(f => f != null))
            {
                if (feature.GetType().Name.Contains("AmbientOcclusion"))
                {
                    feature.SetActive(false);
                    EditorUtility.SetDirty(feature);
                }
            }
            EditorUtility.SetDirty(renderer);
        }

        /// <summary>
        /// Windows 빌드의 그래픽 API를 Direct3D11 하나로 고정한다.
        /// </summary>
        private static void UseDirect3D11Only()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows, new[] { GraphicsDeviceType.Direct3D11 });
        }

        /// <summary>
        /// 불리언 속성을 설정한다. 없으면 경고한다. (URP 버전에 따라 이름이 다를 수 있음)
        /// </summary>
        private static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
                property.boolValue = value;
            else
                Debug.LogWarning($"[BuildOptimizationSettings] URP 설정 항목을 찾을 수 없습니다: {name}");
        }

        /// <summary>
        /// 정수 속성을 설정한다. 없으면 경고한다.
        /// </summary>
        private static void SetInt(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
                property.intValue = value;
            else
                Debug.LogWarning($"[BuildOptimizationSettings] URP 설정 항목을 찾을 수 없습니다: {name}");
        }
    }
}
