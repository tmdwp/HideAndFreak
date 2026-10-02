using FishNet;
using GuardVsMannequin.Room;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 룸 화면 코드들이 함께 쓰는 로컬 플레이어 정보.
    /// </summary>
    public static class RoomClient
    {
        /// <summary>로컬 플레이어가 호스트(서버)인지 여부.</summary>
        public static bool IsHost => InstanceFinder.IsServerStarted;

        /// <summary>
        /// 로컬 플레이어의 참가자 정보를 찾는다.
        /// </summary>
        public static bool TryGetLocalMember(RoomState room, out RoomMember member)
        {
            int localId = InstanceFinder.ClientManager.Connection.ClientId;
            return room.Members.TryGetValue(localId, out member);
        }

        /// <summary>
        /// 로컬 플레이어가 준비 상태인지 여부.
        /// </summary>
        public static bool IsLocalReady(RoomState room)
        {
            return TryGetLocalMember(room, out RoomMember member) && member.isReady;
        }
    }
}
