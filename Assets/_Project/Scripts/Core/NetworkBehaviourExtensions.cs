using FishNet.Object;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// NetworkBehaviour를 네트워크 초기화 전에도 안전하게 조회하기 위한 확장 메서드.
    /// FishNet은 초기화 전에 내부 NetworkObject 캐시가 null이라
    /// IsServerInitialized/IsClientInitialized 등을 읽으면 NullReferenceException이 발생한다.
    /// </summary>
    public static class NetworkBehaviourExtensions
    {
        /// <summary>
        /// 네트워크 초기화가 끝나 FishNet 상태 프로퍼티를 읽을 수 있는지 확인한다.
        /// </summary>
        public static bool IsNetworkReady(this NetworkBehaviour behaviour)
        {
            return behaviour != null && behaviour.NetworkObject != null;
        }

        /// <summary>
        /// 클라이언트에서 초기화되었는지 안전하게 확인한다.
        /// </summary>
        public static bool IsClientReady(this NetworkBehaviour behaviour)
        {
            return behaviour.IsNetworkReady() && behaviour.IsClientInitialized;
        }

        /// <summary>
        /// 서버에서 초기화되었는지 안전하게 확인한다.
        /// </summary>
        public static bool IsServerReady(this NetworkBehaviour behaviour)
        {
            return behaviour.IsNetworkReady() && behaviour.IsServerInitialized;
        }
    }
}
