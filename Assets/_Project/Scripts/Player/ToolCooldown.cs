using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 도구 쿨타임. 소유자 화면(즉시 반응·HUD 표시)과 서버(검증)가 각자 시간을 잰다.
    /// 서버는 네트워크 지연으로 요청이 조금 일찍 도착할 수 있어 <see cref="Tolerance"/>만큼 봐준다.
    /// </summary>
    public sealed class ToolCooldown
    {
        /// <summary>서버 검증 허용 오차(초).</summary>
        public const float Tolerance = 0.15f;

        private float localEnd;
        private float serverEnd;

        /// <summary>소유자 화면의 남은 쿨타임(초).</summary>
        public float LocalRemaining => Mathf.Max(0f, localEnd - Time.time);

        /// <summary>서버에서 다시 사용할 수 있는지 여부. (허용 오차 포함)</summary>
        public bool IsServerReady => Time.time >= serverEnd - Tolerance;

        /// <summary>
        /// 소유자 화면의 쿨타임을 시작한다.
        /// </summary>
        public void StartLocal(float duration)
        {
            localEnd = Time.time + duration;
        }

        /// <summary>
        /// 서버의 쿨타임을 시작한다. (사용 가능 여부와 관계없이)
        /// </summary>
        public void StartServer(float duration)
        {
            serverEnd = Time.time + duration;
        }

        /// <summary>
        /// 서버에서 사용할 수 있으면 쿨타임을 시작하고 true, 아직 쿨타임이면 false.
        /// </summary>
        public bool TryStartServer(float duration)
        {
            if (!IsServerReady)
                return false;
            StartServer(duration);
            return true;
        }
    }
}
