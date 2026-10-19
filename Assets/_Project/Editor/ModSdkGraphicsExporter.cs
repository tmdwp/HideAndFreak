using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 게임의 URP 설정(PC_RPAsset·PC_Renderer·기본 볼륨 프로필)을 모드 SDK용으로 복사한다.
    /// 모드 번들에 들어가는 셰이더는 모드 제작 프로젝트의 URP 설정으로 만들어지므로, 게임과 설정이 다르면
    /// 그림자·추가 조명 등의 셰이더 변형이 빠져 분홍색이나 잘못된 빛으로 보일 수 있다.
    /// 게임 코드에 의존하는 렌더러 기능(외곽선 등)은 SDK에서 쓸 수 없으므로 복사본에서 뺀다. (URP 기본 기능은 유지)
    /// SDK에서는 "Hide&amp;Freak > 게임 그래픽 설정 적용"(<see cref="ModSdkSettings"/>)으로 이 설정을 쓴다.
    /// </summary>
    public static class ModSdkGraphicsExporter
    {
        private const string SourceAsset = "Assets/Settings/PC_RPAsset.asset";
        private const string SourceRenderer = "Assets/Settings/PC_Renderer.asset";

        /// <summary>
        /// SDK 폴더에 URP 설정 복사본을 새로 만든다. 성공하면 true.
        /// </summary>
        public static bool Export()
        {
            var sourceAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SourceAsset);
            if (sourceAsset == null || AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SourceRenderer) == null)
            {
                Debug.LogError($"[ModSdkGraphicsExporter] 게임 URP 설정을 찾을 수 없습니다: {SourceAsset}, {SourceRenderer}");
                return false;
            }

            EnsureFolder(ModSdkSettings.SettingsFolder);
            CopyOver(SourceAsset, ModSdkSettings.PipelineAssetPath);
            CopyOver(SourceRenderer, ModSdkSettings.RendererPath);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ModSdkSettings.RendererPath);
            RemoveGameRendererFeatures(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ModSdkSettings.PipelineAssetPath);
            var serializedPipeline = new SerializedObject(pipeline);
            SerializedProperty renderers = serializedPipeline.FindProperty("m_RendererDataList");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serializedPipeline.FindProperty("m_DefaultRendererIndex").intValue = 0;

            // 기본 볼륨 프로필도 SDK 폴더로 복사해 가리킨다. (게임 프로젝트의 다른 폴더에 의존하지 않도록)
            SerializedProperty volumeProfile = serializedPipeline.FindProperty("m_VolumeProfile");
            if (volumeProfile != null && volumeProfile.objectReferenceValue != null)
            {
                CopyOver(AssetDatabase.GetAssetPath(volumeProfile.objectReferenceValue), ModSdkSettings.VolumeProfilePath);
                volumeProfile.objectReferenceValue = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ModSdkSettings.VolumeProfilePath);
            }
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>
        /// 게임 코드(GuardVsMannequin 어셈블리)에 있는 렌더러 기능을 지운다. URP 기본 기능(SSAO 등)은 남긴다.
        /// </summary>
        private static void RemoveGameRendererFeatures(UniversalRendererData renderer)
        {
            var serialized = new SerializedObject(renderer);
            SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
            // 기능 목록과 같은 순서의 내부 ID 목록. 함께 지워야 URP가 "없는 기능" 경고를 내지 않는다.
            SerializedProperty featureMap = serialized.FindProperty("m_RendererFeatureMap");
            for (int i = features.arraySize - 1; i >= 0; i--)
            {
                var feature = features.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                bool fromGame = feature == null || feature.GetType().Assembly.GetName().Name.StartsWith("GuardVsMannequin");
                if (!fromGame)
                    continue;

                features.DeleteArrayElementAtIndex(i);
                if (featureMap != null && featureMap.isArray && featureMap.arraySize > i)
                    featureMap.DeleteArrayElementAtIndex(i);
                if (feature != null)
                    Object.DestroyImmediate(feature, true);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            renderer.SetDirty();
        }

        /// <summary>
        /// 에셋을 대상 경로로 복사한다. 이미 있으면 지우고 새로 복사한다.
        /// </summary>
        private static void CopyOver(string source, string destination)
        {
            AssetDatabase.DeleteAsset(destination);
            AssetDatabase.CopyAsset(source, destination);
        }

        /// <summary>
        /// "Assets/a/b/c" 형식의 폴더를 차례로 만든다.
        /// </summary>
        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            foreach (string part in parts.Skip(1))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
