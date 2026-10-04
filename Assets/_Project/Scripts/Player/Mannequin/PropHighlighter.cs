using FishNet.Object;
using GuardVsMannequin.Props;
using GuardVsMannequin.Rendering;
using UnityEngine;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 마네킹 본인 화면에서, 조준 중인 변신 가능 사물에 조준 외곽선(<see cref="OutlineKind.Aim"/>)을 표시한다.
    /// 대상만 정하고, 그리기는 공용 외곽선 기능(<see cref="IOutlineService"/>)에 맡긴다.
    /// 변신 중이거나 조준한 사물이 없으면 숨긴다. 다른 플레이어에게는 보이지 않는다.
    /// </summary>
    [RequireComponent(typeof(MannequinTransformer))]
    public class PropHighlighter : NetworkBehaviour
    {
        private MannequinTransformer transformer;
        private IOutlineService outlines;
        private TransformableProp currentTarget;

        /// <summary>
        /// 변신 컴포넌트와 외곽선 기능을 캐시한다.
        /// </summary>
        private void Awake()
        {
            transformer = GetComponent<MannequinTransformer>();
            outlines = OutlineRegistry.Instance;
        }

        /// <summary>
        /// 사라질 때 외곽선을 지운다.
        /// </summary>
        private void OnDestroy()
        {
            outlines?.Hide(this);
        }

        /// <summary>
        /// 소유자 화면에서만 조준한 사물을 찾아 외곽선 대상을 바꾼다.
        /// </summary>
        private void Update()
        {
            if (!IsOwner)
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
            if (target == null)
            {
                outlines.Hide(this);
                return;
            }
            outlines.Show(this, target.GetComponentsInChildren<Renderer>(), OutlineKind.Aim);
        }
    }
}
