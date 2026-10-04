using GuardVsMannequin.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 프로젝트의 모든 URP Universal Renderer(PC_Renderer 등)에 <see cref="OutlineRendererFeature"/>를 추가한다.
    /// URP 에디터가 기능을 추가하는 방식(하위 에셋 + m_RendererFeatures/m_RendererFeatureMap)을 그대로 따른다.
    /// 이미 있으면 셰이더 연결만 확인한다. 여러 번 실행해도 된다.
    /// </summary>
    public static class OutlineFeatureInstaller
    {
        private const string MaskShaderPath = "Assets/_Project/Art/Shaders/OutlineMask.shader";
        private const string CompositeShaderPath = "Assets/_Project/Art/Shaders/OutlineComposite.shader";

        /// <summary>
        /// 외곽선 기능을 설치한다.
        /// </summary>
        /// <returns>새로 추가한 렌더러 수. 셰이더를 찾지 못하면 -1.</returns>
        public static int Install()
        {
            Shader mask = AssetDatabase.LoadAssetAtPath<Shader>(MaskShaderPath);
            Shader composite = AssetDatabase.LoadAssetAtPath<Shader>(CompositeShaderPath);
            if (mask == null || composite == null)
            {
                Debug.LogError($"[OutlineFeatureInstaller] 외곽선 셰이더를 찾지 못했습니다: {MaskShaderPath}, {CompositeShaderPath}");
                return -1;
            }

            int added = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null)
                    continue;

                OutlineRendererFeature feature = FindFeature(data);
                if (feature == null)
                {
                    feature = AddFeature(data);
                    added++;
                }

                feature.settings.maskShader = mask;
                feature.settings.compositeShader = composite;
                EditorUtility.SetDirty(feature);
                data.SetDirty();
                EditorUtility.SetDirty(data);
            }

            AssetDatabase.SaveAssets();
            return added;
        }

        /// <summary>
        /// 렌더러에 이미 있는 외곽선 기능을 찾는다.
        /// </summary>
        private static OutlineRendererFeature FindFeature(ScriptableRendererData data)
        {
            foreach (ScriptableRendererFeature feature in data.rendererFeatures)
            {
                if (feature is OutlineRendererFeature outline)
                    return outline;
            }
            return null;
        }

        /// <summary>
        /// 기능을 하위 에셋으로 만들어 렌더러 목록과 ID 맵에 넣는다.
        /// </summary>
        private static OutlineRendererFeature AddFeature(ScriptableRendererData data)
        {
            var feature = ScriptableObject.CreateInstance<OutlineRendererFeature>();
            feature.name = "Outline";
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var serialized = new SerializedObject(data);
            SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
            SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return feature;
        }
    }
}
