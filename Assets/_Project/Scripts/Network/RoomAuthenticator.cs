using System;
using System.Collections;
using System.Net;
using FishNet.Authenticating;
using FishNet.Connection;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Network
{
    /// <summary>
    /// 서버에서 새 접속을 검증한다. 통과하지 못한 연결은 FishNet이 끊는다.
    /// - 판이 진행 중이면 거부 (Steam 로비의 참가 차단을 우회한 접속 방지)
    /// - 인원이 가득 찼으면 거부
    /// - Steam 방: Steam 접속은 로비 멤버만, IP 접속은 호스트 자신(루프백)만 허용
    /// 클라이언트에게 따로 데이터를 받지 않고 서버가 아는 정보만으로 판정한다.
    /// </summary>
    public class RoomAuthenticator : Authenticator
    {
        // 참가자는 로비에 들어간 뒤 접속하지만, 호스트의 로비 멤버 목록 갱신이 늦을 수 있어 잠시 기다린다.
        private const float LobbyMemberWaitSeconds = 5f;
        private const float LobbyMemberPollInterval = 0.25f;

        private ISessionService session;
        private SteamLobbyService steamLobby;

        public override event Action<NetworkConnection, bool> OnAuthenticationResult;

        /// <summary>
        /// 판정에 필요한 서비스를 연결한다.
        /// </summary>
        public void Configure(ISessionService sessionService, SteamLobbyService lobbyService)
        {
            session = sessionService;
            steamLobby = lobbyService;
        }

        /// <summary>
        /// 접속 직후(FishNet 버전 확인 후) 호출된다. 검증은 코루틴으로 진행한다.
        /// </summary>
        public override void OnRemoteConnection(NetworkConnection connection)
        {
            StartCoroutine(Authenticate(connection));
        }

        /// <summary>
        /// 접속 경로를 검증한 뒤, 마지막으로 방 상태(진행 중/인원)를 확인해 결과를 알린다.
        /// </summary>
        private IEnumerator Authenticate(NetworkConnection connection)
        {
            string address = connection.GetAddress();

            if (session != null && session.IsSteamRoom)
            {
                if (IPAddress.TryParse(address, out IPAddress ip))
                {
                    // Steam 방의 Tugboat는 루프백에만 열리지만, 설정 실수에 대비해 한 번 더 확인한다.
                    if (!IsLoopback(ip))
                    {
                        Reject(connection, $"Steam 방에 IP로 접속 시도 ({address})");
                        yield break;
                    }
                }
                else if (ulong.TryParse(address, out ulong steamId))
                {
                    float waitEnd = Time.unscaledTime + LobbyMemberWaitSeconds;
                    while (!IsLobbyMember(steamId) && Time.unscaledTime < waitEnd)
                        yield return new WaitForSecondsRealtime(LobbyMemberPollInterval);

                    if (!IsLobbyMember(steamId))
                    {
                        Reject(connection, $"로비 멤버가 아닌 SteamID의 접속 ({steamId})");
                        yield break;
                    }
                }
                else
                {
                    Reject(connection, $"알 수 없는 주소 형식 ({address})");
                    yield break;
                }
            }

            // 기다리는 동안 연결이 끊겼으면 결과를 알리지 않는다.
            if (!connection.IsActive)
                yield break;

            if (GameServices.Room != null && GameServices.Room.IsInGame)
            {
                Reject(connection, "게임 진행 중 접속");
                yield break;
            }

            if (CountAuthenticatedClients() >= GameConstants.MaxPlayers)
            {
                Reject(connection, "방 인원 초과");
                yield break;
            }

            OnAuthenticationResult?.Invoke(connection, true);
        }

        /// <summary>
        /// Steam 로비 멤버인지 확인한다.
        /// </summary>
        private bool IsLobbyMember(ulong steamId)
        {
            return steamLobby != null && steamLobby.IsLobbyMember(steamId);
        }

        /// <summary>
        /// 루프백 주소인지 확인한다. (IPv4를 매핑한 IPv6 주소 포함)
        /// </summary>
        private static bool IsLoopback(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();
            return IPAddress.IsLoopback(ip);
        }

        /// <summary>
        /// 인증을 마친 연결 수를 센다.
        /// </summary>
        private int CountAuthenticatedClients()
        {
            int count = 0;
            foreach (NetworkConnection client in NetworkManager.ServerManager.Clients.Values)
            {
                if (client.IsAuthenticated)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 접속을 거부하고 사유를 기록한다.
        /// </summary>
        private void Reject(NetworkConnection connection, string reason)
        {
            Debug.LogWarning($"[RoomAuthenticator] 접속 거부: {reason}, 연결 {connection.ClientId}");
            if (connection.IsActive)
                OnAuthenticationResult?.Invoke(connection, false);
        }
    }
}
