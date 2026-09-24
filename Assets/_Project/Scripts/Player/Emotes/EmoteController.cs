using FishNet.Object;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player.Emotes
{
    /// <summary>
    /// 감정 표현을 네트워크로 동기화한다.
    /// 실제 마네킹은 1~6번 키 입력으로, 가짜 마네킹은 서버 AI가 <see cref="ServerPlay"/>로 재생한다.
    /// 두 경우 모두 같은 RPC와 같은 뷰를 거치므로 겉보기로 구분할 수 없다.
    /// </summary>
    public class EmoteController : NetworkBehaviour
    {
        private IEmoteView emoteView;
        private IPlayerInput playerInput;
        private IEmoteBlocker emoteBlocker;
        private float serverEmoteEndTime;

        /// <summary>
        /// 뷰, 입력, 차단 조건 구현을 캐시한다. (가짜 마네킹은 입력과 차단 조건이 없다)
        /// </summary>
        private void Awake()
        {
            emoteView = GetComponent<IEmoteView>();
            playerInput = GetComponent<IPlayerInput>();
            emoteBlocker = GetComponent<IEmoteBlocker>();
        }

        /// <summary>
        /// 소유자만 감정 표현 입력을 구독한다.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner && playerInput != null)
                playerInput.EmotePressed += OnEmotePressed;
        }

        /// <summary>
        /// 입력 구독을 해제한다.
        /// </summary>
        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOwner && playerInput != null)
                playerInput.EmotePressed -= OnEmotePressed;
        }

        /// <summary>
        /// 차단 조건(변신 중 등)이 아니면 서버에 재생을 요청한다.
        /// </summary>
        private void OnEmotePressed(int emoteId)
        {
            if (emoteBlocker != null && emoteBlocker.IsEmoteBlocked)
                return;

            RequestEmoteServerRpc(emoteId);
        }

        /// <summary>
        /// 서버에서 요청을 검증하고 재생한다.
        /// </summary>
        [ServerRpc]
        private void RequestEmoteServerRpc(int emoteId)
        {
            if (emoteBlocker != null && emoteBlocker.IsEmoteBlocked)
                return;

            ServerPlay(emoteId);
        }

        /// <summary>
        /// 모든 클라이언트에서 감정 표현을 재생한다. 재생 중이면 무시한다. (서버 전용)
        /// </summary>
        /// <returns>재생을 시작했는지 여부.</returns>
        [Server]
        public bool ServerPlay(int emoteId)
        {
            if (emoteId < 1 || emoteId > GameConstants.EmoteCount || Time.time < serverEmoteEndTime)
                return false;

            serverEmoteEndTime = Time.time + GameConstants.EmoteDuration;
            PlayObserversRpc(emoteId);
            return true;
        }

        /// <summary>
        /// 각 클라이언트에서 뷰로 재생한다.
        /// </summary>
        [ObserversRpc]
        private void PlayObserversRpc(int emoteId)
        {
            emoteView?.Play(emoteId);
        }
    }

    /// <summary>
    /// 감정 표현을 막는 조건. (마네킹: 변신 중)
    /// </summary>
    public interface IEmoteBlocker
    {
        /// <summary>지금 감정 표현이 막혀 있는지 여부.</summary>
        bool IsEmoteBlocked { get; }
    }
}
