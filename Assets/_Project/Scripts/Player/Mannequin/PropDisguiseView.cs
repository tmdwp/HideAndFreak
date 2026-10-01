using ECM2;
using GuardVsMannequin.Props;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 변신 상태에 맞춰 마네킹의 외형과 충돌체 크기를 바꾼다.
    /// 모든 피어에서 실행되어, 서버의 히트스캔도 변신한 크기로 판정된다.
    /// </summary>
    [RequireComponent(typeof(MannequinTransformer))]
    public class PropDisguiseView : MonoBehaviour
    {
        private const float MinRadius = 0.15f;
        private const float MaxRadius = 1.0f;

        [SerializeField] private CharacterMovement characterMovement;

        [Tooltip("변신 중 숨길 마네킹 본체 렌더러.")]
        [SerializeField] private Renderer[] bodyRenderers;

        private MannequinTransformer transformer;
        private GameObject disguiseObject;
        private MeshFilter disguiseFilter;
        private MeshRenderer disguiseRenderer;
        private float defaultRadius;
        private float defaultHeight;
        private bool disguiseShadowsOnly;

        /// <summary>
        /// 기본 충돌체 크기를 저장하고 변신 이벤트를 구독한다.
        /// </summary>
        private void Awake()
        {
            transformer = GetComponent<MannequinTransformer>();
            defaultRadius = characterMovement.radius;
            defaultHeight = characterMovement.height;
            transformer.DisguiseChanged += ApplyDisguise;
        }

        /// <summary>
        /// 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            transformer.DisguiseChanged -= ApplyDisguise;
        }

        /// <summary>
        /// PropId에 맞게 외형과 충돌체를 적용한다. NoProp이면 원래 모습으로 되돌린다.
        /// </summary>
        private void ApplyDisguise(int propId)
        {
            if (!transformer.Database.TryGet(propId, out PropDefinition definition))
            {
                RestoreBody();
                return;
            }

            EnsureDisguiseObject();
            disguiseFilter.sharedMesh = definition.mesh;
            disguiseRenderer.sharedMaterials = definition.materials;

            // 맵에 놓인 기울기를 그대로 적용하고, 사물의 바닥이 캐릭터 발 위치(피벗)에 오도록 배치한다.
            Bounds bounds = definition.GetScaledBounds();
            disguiseObject.transform.localRotation = definition.Rotation;
            disguiseObject.transform.localScale = definition.scale;
            disguiseObject.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            disguiseObject.SetActive(true);

            SetBodyVisible(false);

            float radius = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f, MinRadius, MaxRadius);
            characterMovement.SetDimensions(radius, Mathf.Max(bounds.size.y, radius * 2f));
        }

        /// <summary>
        /// 변신 외형을 숨기고 본체와 기본 충돌체로 되돌린다.
        /// </summary>
        private void RestoreBody()
        {
            if (disguiseObject != null)
                disguiseObject.SetActive(false);

            SetBodyVisible(true);
            characterMovement.SetDimensions(defaultRadius, defaultHeight);
        }

        /// <summary>
        /// 변신 외형용 자식 오브젝트를 처음 한 번만 만든다.
        /// </summary>
        private void EnsureDisguiseObject()
        {
            if (disguiseObject != null)
                return;

            disguiseObject = new GameObject("Disguise");
            disguiseObject.transform.SetParent(transform, false);
            disguiseFilter = disguiseObject.AddComponent<MeshFilter>();
            disguiseRenderer = disguiseObject.AddComponent<MeshRenderer>();
            ApplyDisguiseShadowMode();
        }

        /// <summary>
        /// 변신 외형을 그림자만 보이게 할지 정한다. 사냥 시간 1인칭에서 본인 화면에 사물이 가리지 않게 한다.
        /// </summary>
        public void SetDisguiseShadowsOnly(bool shadowsOnly)
        {
            disguiseShadowsOnly = shadowsOnly;
            ApplyDisguiseShadowMode();
        }

        /// <summary>
        /// 저장된 그림자 모드를 변신 외형 렌더러에 적용한다.
        /// </summary>
        private void ApplyDisguiseShadowMode()
        {
            if (disguiseRenderer != null)
                disguiseRenderer.shadowCastingMode = disguiseShadowsOnly ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        }

        /// <summary>
        /// 마네킹 본체 렌더러를 켜고 끈다.
        /// </summary>
        private void SetBodyVisible(bool visible)
        {
            // 외형(CharacterSkinView)이 있으면 외형 렌더러를 숨긴다.
            foreach (Renderer bodyRenderer in CharacterSkinView.ResolveBodyRenderers(this, bodyRenderers))
                bodyRenderer.enabled = visible;
        }
    }
}
