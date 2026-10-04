using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace GuardVsMannequin.Rendering
{
    /// <summary>
    /// 화면 공간 외곽선 (URP Renderer Feature, RenderGraph).
    /// ① 마스크 패스: <see cref="IOutlineSource"/>의 렌더러를 깊이 검사 없이 마스크 텍스처에 그린다. (R = 조준, G = 수류탄 표시)
    /// ② 합성 패스: 마스크 가장자리를 넓혀 화면에 외곽선 색을 덧그린다.
    /// 메시 법선과 무관하게 같은 두께로 그려지고, 스킨 메시·오클루전 컬링으로 가려진 대상도 그릴 수 있다. (벽 너머 표시)
    /// 깊이 사전 패스(Depth Priming)와도 무관하다. 설치는 에디터 메뉴(Phase 7 적용)가 PC_Renderer에 추가한다.
    /// </summary>
    public class OutlineRendererFeature : ScriptableRendererFeature
    {
        /// <summary>인스펙터 설정.</summary>
        [Serializable]
        public class Settings
        {
            public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
            public Color aimColor = new(1f, 0.85f, 0.2f, 1f);
            public Color revealColor = new(1f, 0.2f, 0.15f, 1f);

            [Tooltip("외곽선 두께(픽셀).")]
            [Range(1f, 6f)] public float thickness = 2.5f;

            [Tooltip("Hidden/GuardVsMannequin/OutlineMask. 빌드에 포함되도록 직접 연결한다.")]
            public Shader maskShader;

            [Tooltip("Hidden/GuardVsMannequin/OutlineComposite. 빌드에 포함되도록 직접 연결한다.")]
            public Shader compositeShader;
        }

        public Settings settings = new();

        private OutlinePass pass;
        private Material aimMaskMaterial;
        private Material revealMaskMaterial;
        private Material compositeMaterial;

        /// <summary>
        /// 머티리얼과 패스를 만든다.
        /// </summary>
        public override void Create()
        {
            ReleaseMaterials();
            if (settings.maskShader == null || settings.compositeShader == null)
                return;

            aimMaskMaterial = CoreUtils.CreateEngineMaterial(settings.maskShader);
            aimMaskMaterial.SetColor(OutlinePass.MaskColorId, new Color(1f, 0f, 0f, 1f));
            revealMaskMaterial = CoreUtils.CreateEngineMaterial(settings.maskShader);
            revealMaskMaterial.SetColor(OutlinePass.MaskColorId, new Color(0f, 1f, 0f, 1f));
            compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);

            pass = new OutlinePass(OutlineRegistry.Instance, aimMaskMaterial, revealMaskMaterial, compositeMaterial)
            {
                renderPassEvent = settings.passEvent
            };
        }

        /// <summary>
        /// 게임·씬 카메라에 외곽선 패스를 넣는다. 표시할 외곽선이 없으면 넣지 않는다.
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                return;

            compositeMaterial.SetColor(OutlinePass.AimColorId, settings.aimColor);
            compositeMaterial.SetColor(OutlinePass.RevealColorId, settings.revealColor);
            compositeMaterial.SetFloat(OutlinePass.ThicknessId, settings.thickness);
            renderer.EnqueuePass(pass);
        }

        /// <summary>
        /// 머티리얼을 해제한다.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            ReleaseMaterials();
        }

        /// <summary>
        /// 만든 머티리얼을 지운다.
        /// </summary>
        private void ReleaseMaterials()
        {
            CoreUtils.Destroy(aimMaskMaterial);
            CoreUtils.Destroy(revealMaskMaterial);
            CoreUtils.Destroy(compositeMaterial);
            aimMaskMaterial = revealMaskMaterial = compositeMaterial = null;
            pass = null;
        }

        /// <summary>
        /// 마스크를 그리고 외곽선을 합성하는 패스.
        /// </summary>
        private sealed class OutlinePass : ScriptableRenderPass
        {
            public static readonly int MaskColorId = Shader.PropertyToID("_MaskColor");
            public static readonly int AimColorId = Shader.PropertyToID("_AimColor");
            public static readonly int RevealColorId = Shader.PropertyToID("_RevealColor");
            public static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
            private static readonly int MaskTexelSizeId = Shader.PropertyToID("_MaskTexelSize");

            private readonly IOutlineSource source;
            private readonly Material aimMask;
            private readonly Material revealMask;
            private readonly Material composite;

            /// <summary>마스크 패스 데이터.</summary>
            private class MaskPassData
            {
                public IOutlineSource source;
                public Material aimMask;
                public Material revealMask;
            }

            /// <summary>합성 패스 데이터.</summary>
            private class CompositePassData
            {
                public TextureHandle mask;
                public Material composite;
                public Vector4 texelSize;
            }

            public OutlinePass(IOutlineSource source, Material aimMask, Material revealMask, Material composite)
            {
                this.source = source;
                this.aimMask = aimMask;
                this.revealMask = revealMask;
                this.composite = composite;
                profilingSampler = new ProfilingSampler("Outline");
            }

            /// <summary>
            /// 마스크 텍스처를 만들고 두 패스를 기록한다.
            /// </summary>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!HasAnyEntry())
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle cameraColor = resourceData.activeColorTexture;
                if (!cameraColor.IsValid())
                    return;

                TextureDesc desc = renderGraph.GetTextureDesc(cameraColor);
                desc.name = "_OutlineMask";
                desc.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;
                desc.depthBufferBits = DepthBits.None;
                desc.msaaSamples = MSAASamples.None;
                desc.filterMode = FilterMode.Point;
                desc.clearBuffer = true;
                desc.clearColor = Color.clear;
                TextureHandle mask = renderGraph.CreateTexture(desc);

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Outline Mask", out var maskData, profilingSampler))
                {
                    maskData.source = source;
                    maskData.aimMask = aimMask;
                    maskData.revealMask = revealMask;
                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) => DrawMask(data, context.cmd));
                }

                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Outline Composite", out var compositeData, profilingSampler))
                {
                    compositeData.mask = mask;
                    compositeData.composite = composite;
                    compositeData.texelSize = new Vector4(1f / Mathf.Max(1, desc.width), 1f / Mathf.Max(1, desc.height), 0f, 0f);
                    builder.UseTexture(mask, AccessFlags.Read);
                    builder.SetRenderAttachment(cameraColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                    {
                        data.composite.SetVector(MaskTexelSizeId, data.texelSize);
                        Blitter.BlitTexture(context.cmd, data.mask, new Vector4(1f, 1f, 0f, 0f), data.composite, 0);
                    });
                }
            }

            /// <summary>
            /// 표시할 외곽선이 하나라도 있는지 확인한다. (없으면 패스를 기록하지 않아 비용이 없음)
            /// </summary>
            private bool HasAnyEntry()
            {
                foreach (OutlineEntry _ in source.Entries)
                    return true;
                return false;
            }

            /// <summary>
            /// 등록된 렌더러를 종류별 마스크 색으로 그린다. 꺼져 있거나 사라진 렌더러는 건너뛴다.
            /// </summary>
            private static void DrawMask(MaskPassData data, RasterCommandBuffer cmd)
            {
                foreach (OutlineEntry entry in data.source.Entries)
                {
                    Material material = entry.Kind == OutlineKind.Reveal ? data.revealMask : data.aimMask;
                    foreach (Renderer renderer in entry.Renderers)
                    {
                        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                            continue;

                        int subMeshCount = GetSubMeshCount(renderer);
                        for (int i = 0; i < subMeshCount; i++)
                            cmd.DrawRenderer(renderer, material, i, 0);
                    }
                }
            }

            /// <summary>
            /// 렌더러가 그리는 서브메시 수. (메시를 알 수 없으면 1)
            /// </summary>
            private static int GetSubMeshCount(Renderer renderer)
            {
                Mesh mesh = renderer switch
                {
                    SkinnedMeshRenderer skinned => skinned.sharedMesh,
                    MeshRenderer => renderer.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null,
                    _ => null
                };
                return mesh != null ? Mathf.Max(1, mesh.subMeshCount) : 1;
            }
        }
    }
}
