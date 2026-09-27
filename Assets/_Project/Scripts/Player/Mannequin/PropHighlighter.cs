using FishNet.Object;
using GuardVsMannequin.Props;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 마네킹 본인 화면에서, 조준 중인 변신 가능 사물에 얇은 외곽선을 표시한다.
    /// 사물의 메시를 외곽선 머티리얼(GuardVsMannequin/PropOutline)로 한 번 더 그리는 방식이다.
    /// 변신 중이거나 조준한 사물이 없으면 숨긴다. 다른 플레이어에게는 보이지 않는다.
    /// </summary>
    [RequireComponent(typeof(MannequinTransformer))]
    public class PropHighlighter : NetworkBehaviour
    {
        [Tooltip("외곽선 머티리얼. 색과 두께는 머티리얼에서 조정한다.")]
        [SerializeField] private Material outlineMaterial;

        private MannequinTransformer transformer;
        private GameObject outlineObject;
        private MeshFilter outlineFilter;
        private MeshRenderer outlineRenderer;
        private TransformableProp currentTarget;

        /// <summary>
        /// 변신 컴포넌트를 캐시한다.
        /// </summary>
        private void Awake()
        {
            transformer = GetComponent<MannequinTransformer>();
        }

        /// <summary>
        /// 외곽선 오브젝트를 정리한다. (사물에 붙어 있으므로 아바타와 함께 사라지지 않는다)
        /// </summary>
        private void OnDestroy()
        {
            if (outlineObject != null)
                Destroy(outlineObject);
        }

        /// <summary>
        /// 소유자 화면에서만 조준한 사물을 찾아 외곽선을 옮긴다.
        /// </summary>
        private void Update()
        {
            if (!IsOwner || outlineMaterial == null)
                return;

            TransformableProp target = null;
            if (!transformer.IsDisguised && transformer.TryGetAimedProp(out TransformableProp aimed))
                target = aimed;

            if (target != currentTarget)
                ShowOn(target);
        }

        /// <summary>
        /// 외곽선을 대상 사물로 옮긴다. null이면 숨긴다.
        /// </summary>
        private void ShowOn(TransformableProp target)
        {
            currentTarget = target;
            MeshFilter source = target != null ? target.GetComponent<MeshFilter>() : null;
            if (source == null || source.sharedMesh == null)
            {
                if (outlineObject != null)
                    outlineObject.SetActive(false);
                return;
            }

            EnsureOutlineObject();
            outlineObject.transform.SetParent(source.transform, false);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;

            outlineFilter.sharedMesh = source.sharedMesh;
            var materials = new Material[Mathf.Max(1, source.sharedMesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = outlineMaterial;
            outlineRenderer.sharedMaterials = materials;
            outlineObject.SetActive(true);
        }

        /// <summary>
        /// 외곽선용 오브젝트를 처음 한 번만 만든다. (그림자·조명 영향 없음)
        /// </summary>
        private void EnsureOutlineObject()
        {
            if (outlineObject != null)
                return;

            outlineObject = new GameObject("PropOutline");
            outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }
}
