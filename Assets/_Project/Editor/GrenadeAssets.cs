using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 임시 수류탄 연출용 머티리얼(수류탄 몸체, 폭발 구)을 만들거나 불러온다. 이미 있으면 사용자가 바꾼 값을 유지한다.
    /// 에셋으로 저장해 프리팹이 참조하므로 셰이더가 빌드에 포함된다.
    /// </summary>
    public static class GrenadeAssets
    {
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        /// <summary>수류탄 몸체 머티리얼.</summary>
        public static Material GetGrenadeMaterial()
        {
            return GetOrCreate("M_Grenade", LitShaderName, new Color(0.22f, 0.27f, 0.18f));
        }

        /// <summary>폭발 구 머티리얼.</summary>
        public static Material GetBlastMaterial()
        {
            return GetOrCreate("M_GrenadeBlast", UnlitShaderName, new Color(1f, 0.55f, 0.2f));
        }

        /// <summary>
        /// 머티리얼 에셋을 불러오고, 없으면 만든다.
        /// </summary>
        private static Material GetOrCreate(string name, string shaderName, Color color)
        {
            string path = $"{TestEnvironmentSetupTool.MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[GrenadeAssets] 셰이더 {shaderName}를 찾지 못해 {name}을 만들지 않았습니다. (기본 머티리얼로 표시)");
                return null;
            }

            material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
