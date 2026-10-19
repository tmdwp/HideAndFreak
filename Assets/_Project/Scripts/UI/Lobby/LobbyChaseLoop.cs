using GuardVsMannequin.Player;
using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로비 배경 연출: 가드(도망)와 구울(추격)이 경로(<see cref="LobbyChasePath"/>)를 따라 계속 돈다.
    /// - 구울은 가드 뒤에서 거리를 좁혔다 벌렸다 하며, 가까워지면 할퀴기(Attack 트리거)를 한다.
    /// - 이동 애니메이션은 각 캐릭터의 <see cref="CharacterSkinView"/>가 위치 변화로 알아서 고른다.
    /// 네트워크·게임 규칙과 무관한 화면 장식이다.
    /// </summary>
    public class LobbyChaseLoop : MonoBehaviour
    {
        [Tooltip("두 캐릭터가 따라 도는 경로.")]
        [SerializeField] private LobbyChasePath path;

        [Tooltip("도망치는 쪽(가드).")]
        [SerializeField] private Transform runner;

        [Tooltip("쫓는 쪽(구울).")]
        [SerializeField] private Transform chaser;

        [Tooltip("쫓는 쪽 외형. 할퀴기 트리거와 애니메이션 속도에 쓴다.")]
        [SerializeField] private CharacterSkinView chaserSkin;

        [Tooltip("이동 속도(m/s). 가드 애니메이션은 1.6 걷기 ~ 4.5 달리기 사이에서 섞인다.")]
        [SerializeField] private float runSpeed = 3.8f;

        [Tooltip("평소 두 캐릭터 사이 거리(m).")]
        [SerializeField] private float baseGap = 3.2f;

        [Tooltip("거리가 좁혀졌다 벌어지는 폭(m). 평소 거리 ± 이 값.")]
        [SerializeField] private float gapSwing = 1.4f;

        [Tooltip("거리가 좁혀졌다 벌어지는 한 주기(초).")]
        [SerializeField] private float gapPeriod = 7f;

        [Tooltip("이 거리(m)보다 가까워지면 할퀴기를 한다.")]
        [SerializeField] private float attackDistance = 2.1f;

        [Tooltip("할퀴기 사이 최소 간격(초).")]
        [SerializeField] private float attackCooldown = 2.5f;

        [Tooltip("쫓는 쪽 애니메이션 재생 속도 배율. 발이 미끄러져 보이면 조절한다.")]
        [SerializeField] private float chaserAnimationSpeed = 1.2f;

        private float travelled;
        private float elapsed;
        private float nextAttackTime;

        /// <summary>
        /// 시작 위치에 두 캐릭터를 놓는다. (외형의 첫 프레임 속도가 튀지 않도록 Awake에서)
        /// </summary>
        private void Awake()
        {
            Place();
        }

        /// <summary>
        /// 쫓는 쪽 애니메이션 속도를 맞춘다. (외형은 각자의 Awake에서 붙는다)
        /// </summary>
        private void Start()
        {
            if (chaserSkin != null && chaserSkin.Animator != null)
                chaserSkin.Animator.speed = chaserAnimationSpeed;
        }

        /// <summary>
        /// 경로를 따라 이동하고, 가까우면 할퀴기를 시도한다.
        /// </summary>
        private void Update()
        {
            travelled += runSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            Place();
            TryAttack();
        }

        /// <summary>
        /// 지금 두 캐릭터 사이 거리. 평소 거리를 중심으로 천천히 오르내린다.
        /// </summary>
        private float CurrentGap()
        {
            float wave = gapPeriod > 0f ? Mathf.Sin(elapsed * Mathf.PI * 2f / gapPeriod) : 0f;
            return Mathf.Max(0.5f, baseGap + gapSwing * wave);
        }

        /// <summary>
        /// 가드는 이동 거리 위치에, 구울은 그보다 거리만큼 뒤에 놓는다.
        /// </summary>
        private void Place()
        {
            if (path == null)
                return;
            SetPose(runner, path.Evaluate(travelled));
            SetPose(chaser, path.Evaluate(travelled - CurrentGap()));
        }

        /// <summary>
        /// 트랜스폼이 있으면 위치·회전을 지정한다.
        /// </summary>
        private static void SetPose(Transform target, Pose pose)
        {
            if (target != null)
                target.SetPositionAndRotation(pose.position, pose.rotation);
        }

        /// <summary>
        /// 충분히 가깝고 대기 시간이 지났으면 할퀴기 트리거를 발동한다. (트리거가 없는 외형은 건너뜀)
        /// </summary>
        private void TryAttack()
        {
            if (chaserSkin == null || elapsed < nextAttackTime || CurrentGap() > attackDistance)
                return;
            if (chaserSkin.TryPlayTrigger(CharacterAnimatorParameters.Attack))
                nextAttackTime = elapsed + attackCooldown;
        }
    }
}
