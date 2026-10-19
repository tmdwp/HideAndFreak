using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 동기화된 상호작용 상태(<see cref="IInteractionState"/>)를 외형 애니메이터의 Interact(Bool)로 옮긴다.
    /// 모든 클라이언트에서 실행되어 다른 플레이어에게도 상호작용 동작이 보인다. 컨트롤러에 Interact가 없으면 아무것도 하지 않는다.
    /// </summary>
    [RequireComponent(typeof(CharacterSkinView))]
    public class InteractAnimationView : MonoBehaviour
    {
        private CharacterSkinView skinView;
        private IInteractionState interactionState;
        private bool lastInteracting;

        /// <summary>
        /// 외형과 상호작용 상태를 캐시한다.
        /// </summary>
        private void Awake()
        {
            skinView = GetComponent<CharacterSkinView>();
            interactionState = GetComponent<IInteractionState>();
        }

        /// <summary>
        /// 상태가 바뀐 프레임에만 애니메이터 값을 바꾼다.
        /// </summary>
        private void Update()
        {
            if (interactionState == null)
                return;

            bool interacting = interactionState.IsInteracting;
            if (interacting == lastInteracting)
                return;

            lastInteracting = interacting;
            skinView.TrySetBool(CharacterAnimatorParameters.Interact, interacting);
        }
    }
}
