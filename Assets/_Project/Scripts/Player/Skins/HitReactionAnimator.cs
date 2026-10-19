using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 체력이 줄면 외형 애니메이터의 Hit 트리거를 발동한다. 트리거가 없으면 아무것도 하지 않는다.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth), typeof(CharacterSkinView))]
    public class HitReactionAnimator : MonoBehaviour
    {
        private PlayerHealth health;
        private CharacterSkinView skinView;

        /// <summary>
        /// 체력 감소 이벤트를 구독한다.
        /// </summary>
        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            skinView = GetComponent<CharacterSkinView>();
            health.Damaged += OnDamaged;
        }

        /// <summary>
        /// 구독을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        /// <summary>
        /// 피격 동작을 재생한다.
        /// </summary>
        private void OnDamaged()
        {
            skinView.TryPlayTrigger(CharacterAnimatorParameters.Hit);
        }
    }
}
