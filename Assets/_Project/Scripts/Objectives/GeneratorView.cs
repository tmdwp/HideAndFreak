using GuardVsMannequin.Core;
using GuardVsMannequin.Player;
using UnityEngine;

namespace GuardVsMannequin.Objectives
{
    /// <summary>
    /// 전원 장치의 파괴 진행도 바(마네킹에게만 표시)와 파괴 상태 외형을 표시한다.
    /// </summary>
    [RequireComponent(typeof(Generator))]
    public class GeneratorView : MonoBehaviour
    {
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Material destroyedMaterial;

        [Tooltip("진행도 바 전체(배경 포함). 로컬 플레이어를 향해 회전한다.")]
        [SerializeField] private Transform barRoot;

        [Tooltip("진행도에 따라 X 크기가 늘어나는 채움 막대. 피벗은 왼쪽 끝.")]
        [SerializeField] private Transform barFill;

        private Generator generator;
        private bool destroyedApplied;

        /// <summary>
        /// 전원 장치를 캐시하고 진행도 바를 숨긴다.
        /// </summary>
        private void Awake()
        {
            generator = GetComponent<Generator>();
            barRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// 매 프레임 파괴 외형과 진행도 바를 갱신한다.
        /// </summary>
        private void Update()
        {
            if (generator.IsDestroyed && !destroyedApplied)
            {
                bodyRenderer.sharedMaterial = destroyedMaterial;
                destroyedApplied = true;
            }

            UpdateProgressBar();
        }

        /// <summary>
        /// 로컬 플레이어가 마네킹이고 진행 중일 때만 바를 보여준다.
        /// </summary>
        private void UpdateProgressBar()
        {
            PlayerIdentity localPlayer = LocalPlayerEvents.Current;
            bool visible = localPlayer != null && localPlayer.Role == PlayerRole.Mannequin
                           && !generator.IsDestroyed && generator.Progress > 0f;

            if (barRoot.gameObject.activeSelf != visible)
                barRoot.gameObject.SetActive(visible);
            if (!visible)
                return;

            Vector3 fillScale = barFill.localScale;
            fillScale.x = Mathf.Clamp01(generator.Progress);
            barFill.localScale = fillScale;

            // 바가 로컬 플레이어 쪽을 향하도록 수평 회전
            Vector3 toPlayer = localPlayer.transform.position - barRoot.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
                barRoot.rotation = Quaternion.LookRotation(-toPlayer);
        }
    }
}
