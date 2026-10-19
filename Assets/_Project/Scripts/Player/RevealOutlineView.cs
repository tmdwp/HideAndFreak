using System.Collections.Generic;
using GuardVsMannequin.Core;
using GuardVsMannequin.Rendering;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 위치가 드러난 동안(<see cref="IStatusEffects.IsRevealed"/>) 상대 진영 화면에만 이 아바타의 외곽선을 벽 너머로 표시한다.
    /// (수류탄에 맞은 마네킹 → 살아 있는 가드 화면)
    /// 변신 중이면 변신한 사물, 아니면 몸체 — 지금 켜져 있는 렌더러를 그대로 쓰므로 외형 종류와 무관하다.
    /// 누가 볼 수 있는지는 역할의 진영(<see cref="RoleDefinition.Side"/>)으로 판단한다. (역할로 분기하지 않음)
    /// </summary>
    [RequireComponent(typeof(PlayerIdentity))]
    public class RevealOutlineView : MonoBehaviour
    {
        /// <summary>렌더러 목록을 다시 모으는 간격(초). (변신·해제로 바뀌는 외형을 따라가기 위함)</summary>
        private const float RefreshInterval = 0.25f;

        private readonly List<Renderer> renderers = new();
        private IStatusEffects statusEffects;
        private PlayerIdentity identity;
        private IOutlineService outlines;
        private float nextRefreshTime;
        private bool isShowing;

        /// <summary>
        /// 상태 효과와 외곽선 기능을 캐시한다.
        /// </summary>
        private void Awake()
        {
            identity = GetComponent<PlayerIdentity>();
            statusEffects = GetComponent<IStatusEffects>();
            outlines = OutlineRegistry.Instance;
        }

        /// <summary>
        /// 꺼지거나 사라질 때 외곽선을 지운다.
        /// </summary>
        private void OnDisable()
        {
            Hide();
        }

        /// <summary>
        /// 표시 조건을 확인하고, 표시 중이면 렌더러 목록을 주기적으로 갱신한다.
        /// </summary>
        private void Update()
        {
            bool shouldShow = statusEffects != null && statusEffects.IsRevealed && IsOpponentOfLocalPlayer();
            if (!shouldShow)
            {
                Hide();
                return;
            }

            if (isShowing && Time.time < nextRefreshTime)
                return;

            nextRefreshTime = Time.time + RefreshInterval;
            CollectVisibleRenderers();
            outlines.Show(this, renderers.ToArray(), OutlineKind.Reveal);
            isShowing = true;
        }

        /// <summary>
        /// 살아 있는 로컬 플레이어가 이 아바타와 다른 진영인지 확인한다.
        /// </summary>
        private bool IsOpponentOfLocalPlayer()
        {
            PlayerIdentity viewer = LocalPlayerEvents.Current;
            if (viewer == null || viewer == identity)
                return false;

            MatchWinner viewerSide = RoleDefinitions.Get(viewer.Role).Side;
            MatchWinner targetSide = RoleDefinitions.Get(identity.Role).Side;
            return viewerSide != MatchWinner.None && targetSide != MatchWinner.None && viewerSide != targetSide;
        }

        /// <summary>
        /// 지금 화면에 그려지는 메시 렌더러만 모은다. (빛·파티클·선 렌더러 제외)
        /// </summary>
        private void CollectVisibleRenderers()
        {
            renderers.Clear();
            foreach (Renderer candidate in GetComponentsInChildren<Renderer>())
            {
                if (candidate.enabled && (candidate is MeshRenderer || candidate is SkinnedMeshRenderer))
                    renderers.Add(candidate);
            }
        }

        /// <summary>
        /// 외곽선을 지운다.
        /// </summary>
        private void Hide()
        {
            if (!isShowing)
                return;
            outlines.Hide(this);
            isShowing = false;
        }
    }
}
