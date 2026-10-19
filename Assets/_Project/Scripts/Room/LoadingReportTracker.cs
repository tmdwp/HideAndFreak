using System.Collections.Generic;

namespace GuardVsMannequin.Room
{
    /// <summary>
    /// 클라이언트별 "아직 씬을 불러오는 중" 알림 시각을 기록하고, 최근에 알림이 왔는지 판단한다. (서버 전용)
    /// 시각은 호출하는 쪽이 넘겨 주므로 Unity 시간에 의존하지 않는다.
    /// </summary>
    public sealed class LoadingReportTracker
    {
        private readonly Dictionary<int, float> lastReportTimes = new();
        private readonly float timeout;

        /// <summary>
        /// 마지막 알림 후 로딩 중으로 인정할 시간(초)으로 만든다.
        /// </summary>
        public LoadingReportTracker(float timeout)
        {
            this.timeout = timeout;
        }

        /// <summary>
        /// 클라이언트의 알림 시각을 기록한다.
        /// </summary>
        public void Record(int clientId, float now)
        {
            lastReportTimes[clientId] = now;
        }

        /// <summary>
        /// 클라이언트가 인정 시간 안에 알림을 보냈는지 확인한다.
        /// </summary>
        public bool IsLoading(int clientId, float now)
        {
            return lastReportTimes.TryGetValue(clientId, out float reportTime) && now - reportTime <= timeout;
        }

        /// <summary>
        /// 접속이 끊긴 클라이언트의 기록을 지운다.
        /// </summary>
        public void Remove(int clientId)
        {
            lastReportTimes.Remove(clientId);
        }

        /// <summary>
        /// 새 씬 전환을 시작할 때 모든 기록을 지운다.
        /// </summary>
        public void Clear()
        {
            lastReportTimes.Clear();
        }
    }
}
