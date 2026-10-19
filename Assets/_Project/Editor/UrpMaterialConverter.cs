using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// Built-in 렌더 파이프라인용 머티리얼(Standard, Legacy, 에셋 전용 셰이더 등)을 URP Lit으로 바꾼다.
    /// URP에서 분홍색으로 보이는 외부 에셋을 맵·캐릭터 생성 도구가 자동으로 고칠 때 사용한다.
    /// 기본 텍스처·색·노멀·금속·AO·발광 값을 옮기고, 이름으로 투명/알파 컷을 추정한다.
    /// URP·셰이더 그래프·파티클·UI 셰이더는 건드리지 않는다.
    /// </summary>
    public static class UrpMaterialConverter
    {
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        // 이미 URP에서 동작하는 셰이더 이름 접두사
        private static readonly string[] CompatiblePrefixes =
        {
            "Universal Render Pipeline/", "Shader Graphs/", "Hidden/Universal", "Sprites/", "UI/", "Skybox/",
            "TextMeshPro/", "Particles/", "Legacy Shaders/Particles/", "Hidden/"
        };

        // 이름에 들어 있으면 알파 컷(나뭇잎, 철망 등)으로 보는 단어
        private static readonly string[] CutoutHints = { "cutout", "leaf", "leaves", "foliage", "grass", "fence", "chain", "net", "plant" };

        // 이름에 들어 있으면 반투명으로 보는 단어
        private static readonly string[] TransparentHints = { "transparent", "glass", "window" };

        /// <summary>
        /// 렌더러들이 쓰는 머티리얼 중 URP와 호환되지 않는 것을 모두 변환한다. 변환한 개수를 반환한다.
        /// </summary>
        public static int ConvertRenderers(IEnumerable<Renderer> renderers)
        {
            var visited = new HashSet<Material>();
            int converted = 0;
            foreach (Renderer target in renderers)
            {
                if (target == null || target is ParticleSystemRenderer)
                    continue;

                foreach (Material material in target.sharedMaterials)
                {
                    if (material != null && visited.Add(material) && ConvertIfNeeded(material))
                        converted++;
                }
            }
            return converted;
        }

        /// <summary>
        /// 머티리얼이 URP와 호환되지 않으면 URP Lit으로 바꾼다. 바꿨으면 true.
        /// </summary>
        public static bool ConvertIfNeeded(Material material)
        {
            if (material == null || IsCompatible(material.shader))
                return false;

            Shader lit = Shader.Find(LitShaderName);
            if (lit == null)
            {
                Debug.LogError("[UrpMaterialConverter] URP Lit 셰이더를 찾을 수 없습니다.");
                return false;
            }

            // 셰이더를 바꾸면 이전 속성을 읽을 수 없으므로 먼저 값을 모아 둔다.
            string oldShaderName = material.shader != null ? material.shader.name : "(없음)";
            Texture baseMap = GetTexture(material, "_MainTex", "_BaseMap", "_Albedo", "_Diffuse", "_BaseColorMap");
            Color baseColor = GetColor(material, Color.white, "_Color", "_BaseColor", "_TintColor", "_MainColor");
            Vector2 tiling = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
            Vector2 offset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            Texture normalMap = GetTexture(material, "_BumpMap", "_NormalMap", "_Normal");
            float normalScale = material.HasProperty("_BumpScale") ? material.GetFloat("_BumpScale") : 1f;
            Texture metallicMap = GetTexture(material, "_MetallicGlossMap", "_MetallicMap");
            float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness")
                : material.HasProperty("_Smoothness") ? material.GetFloat("_Smoothness") : 0.3f;
            Texture occlusionMap = GetTexture(material, "_OcclusionMap", "_AO");
            Texture emissionMap = GetTexture(material, "_EmissionMap", "_Emission");
            Color emissionColor = GetColor(material, Color.black, "_EmissionColor");
            float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;
            int standardMode = GetStandardRenderingMode(material, oldShaderName);
            bool wasCutout = standardMode == 1 || material.IsKeywordEnabled("_ALPHATEST_ON") || NameContains(material, oldShaderName, CutoutHints);
            bool wasTransparent = standardMode >= 2 || material.renderQueue >= (int)RenderQueue.Transparent || NameContains(material, oldShaderName, TransparentHints);

            Undo.RecordObject(material, "Convert to URP");
            material.shader = lit;

            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetColor("_BaseColor", baseColor);

            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_BumpScale", normalScale);
            SetKeyword(material, "_NORMALMAP", normalMap != null);

            material.SetTexture("_MetallicGlossMap", metallicMap);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            SetKeyword(material, "_METALLICSPECGLOSSMAP", metallicMap != null);

            material.SetTexture("_OcclusionMap", occlusionMap);
            SetKeyword(material, "_OCCLUSIONMAP", occlusionMap != null);

            bool hasEmission = emissionMap != null || emissionColor.maxColorComponent > 0.01f;
            material.SetTexture("_EmissionMap", emissionMap);
            material.SetColor("_EmissionColor", hasEmission && emissionMap != null && emissionColor.maxColorComponent <= 0.01f ? Color.white : emissionColor);
            SetKeyword(material, "_EMISSION", hasEmission);
            material.globalIlluminationFlags = hasEmission ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            ApplySurface(material, wasCutout, wasTransparent, cutoff);

            EditorUtility.SetDirty(material);
            Debug.Log($"[UrpMaterialConverter] {material.name}: {oldShaderName} → URP Lit", material);
            return true;
        }

        /// <summary>
        /// 이미 URP에서 동작하는 셰이더인지 확인한다. 셰이더가 없거나 오류 셰이더면 변환 대상이다.
        /// </summary>
        private static bool IsCompatible(Shader shader)
        {
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
                return false;

            foreach (string prefix in CompatiblePrefixes)
            {
                if (shader.name.StartsWith(prefix))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 불투명 / 알파 컷 / 반투명 표면 설정을 적용한다.
        /// </summary>
        private static void ApplySurface(Material material, bool cutout, bool transparent, float cutoff)
        {
            material.SetFloat("_Surface", transparent ? 1f : 0f);
            material.SetFloat("_AlphaClip", cutout ? 1f : 0f);
            material.SetFloat("_Cutoff", cutoff);
            SetKeyword(material, "_ALPHATEST_ON", cutout);
            SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", transparent);

            if (transparent)
            {
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.One);
                material.SetFloat("_DstBlend", (float)BlendMode.Zero);
                material.SetFloat("_ZWrite", 1f);
                material.renderQueue = cutout ? (int)RenderQueue.AlphaTest : -1;
                material.SetOverrideTag("RenderType", cutout ? "TransparentCutout" : "Opaque");
            }

            // 나뭇잎 등 얇은 면은 양면으로 그린다.
            material.SetFloat("_Cull", cutout ? (float)CullMode.Off : (float)CullMode.Back);
        }

        /// <summary>
        /// Built-in Standard 셰이더의 Rendering Mode(0 불투명, 1 컷아웃, 2 페이드, 3 투명)를 읽는다.
        /// Standard 계열이 아니거나 값이 없으면 -1. (렌더 큐가 기본값으로 남은 에셋도 투명을 알아보기 위해 쓴다)
        /// </summary>
        private static int GetStandardRenderingMode(Material material, string shaderName)
        {
            if (!shaderName.StartsWith("Standard") || !material.HasProperty("_Mode"))
                return -1;
            return Mathf.RoundToInt(material.GetFloat("_Mode"));
        }

        /// <summary>
        /// 여러 후보 이름 중 처음으로 값이 있는 텍스처를 반환한다.
        /// </summary>
        private static Texture GetTexture(Material material, params string[] names)
        {
            foreach (string propertyName in names)
            {
                if (material.HasProperty(propertyName) && material.GetTexture(propertyName) != null)
                    return material.GetTexture(propertyName);
            }
            return null;
        }

        /// <summary>
        /// 여러 후보 이름 중 처음으로 있는 색을 반환한다.
        /// </summary>
        private static Color GetColor(Material material, Color fallback, params string[] names)
        {
            foreach (string propertyName in names)
            {
                if (material.HasProperty(propertyName))
                    return material.GetColor(propertyName);
            }
            return fallback;
        }

        /// <summary>
        /// 머티리얼 이름이나 셰이더 이름에 단어가 들어 있는지 확인한다. (대소문자 무시)
        /// </summary>
        private static bool NameContains(Material material, string shaderName, string[] hints)
        {
            string text = (material.name + " " + shaderName).ToLowerInvariant();
            foreach (string hint in hints)
            {
                if (text.Contains(hint))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 셰이더 키워드를 켜고 끈다.
        /// </summary>
        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }
}
