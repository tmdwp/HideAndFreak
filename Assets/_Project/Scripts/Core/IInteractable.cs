namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 좌클릭을 유지해 상호작용하는 대상. (전원 장치 등)
    /// 모든 호출은 서버에서만 이루어진다.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>상호작용 가능한 최대 거리.</summary>
        float InteractRange { get; }

        /// <summary>
        /// 상호작용을 시작한다. 조건이 맞지 않으면 false를 반환한다.
        /// </summary>
        bool TryBeginInteract(IInteractor interactor);

        /// <summary>
        /// 상호작용을 끝낸다.
        /// </summary>
        void EndInteract(IInteractor interactor);

        /// <summary>
        /// 이 주체가 아직 상호작용 참여자인지 확인한다. (대상이 거리·조건 등으로 참여자를 뺄 수 있으므로)
        /// </summary>
        bool IsInteracting(IInteractor interactor);
    }

    /// <summary>
    /// 지금 상호작용 중인지 알려 주는 상태. (모든 클라이언트에 동기화된 값, 애니메이션 표시용)
    /// </summary>
    public interface IInteractionState
    {
        /// <summary>상호작용 중인지 여부.</summary>
        bool IsInteracting { get; }
    }

    /// <summary>
    /// 상호작용을 하는 주체. (마네킹)
    /// </summary>
    public interface IInteractor
    {
        /// <summary>상호작용하는 아바타의 클라이언트 ID.</summary>
        int ClientId { get; }

        /// <summary>현재 월드 위치. 거리 판정에 사용한다.</summary>
        UnityEngine.Vector3 Position { get; }

        /// <summary>지금 상호작용을 계속할 수 있는지 여부. (사망, 변신 등으로 불가능해질 수 있음)</summary>
        bool CanInteract { get; }
    }
}
