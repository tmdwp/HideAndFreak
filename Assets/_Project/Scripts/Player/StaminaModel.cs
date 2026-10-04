using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 스태미나 계산만 맡는 순수 로직. (MonoBehaviour·입력에 의존하지 않음)
    /// 달리는 동안 줄고, 멈추면 잠시 뒤 회복된다. 바닥나면 <see cref="SprintRules.RestartRatio"/>까지 회복해야 다시 달릴 수 있다.
    /// </summary>
    public sealed class StaminaModel
    {
        private float recoverDelayRemaining;

        /// <summary>남은 스태미나(0~1).</summary>
        public float Current { get; private set; } = 1f;

        /// <summary>스태미나를 다 써서 다시 달릴 수 없는 상태인지 여부.</summary>
        public bool IsExhausted { get; private set; }

        /// <summary>
        /// 한 프레임 진행한다.
        /// </summary>
        /// <param name="wantsSprint">달리기 키를 누른 채 움직이고 있는지 여부.</param>
        /// <param name="deltaTime">프레임 시간(초).</param>
        /// <returns>이번 프레임에 실제로 달리는지 여부.</returns>
        public bool Tick(bool wantsSprint, float deltaTime)
        {
            if (IsExhausted && Current >= SprintRules.RestartRatio)
                IsExhausted = false;

            bool sprinting = wantsSprint && !IsExhausted && Current > 0f;
            if (sprinting)
            {
                Current = Mathf.Max(0f, Current - SprintRules.DrainPerSecond * deltaTime);
                recoverDelayRemaining = SprintRules.RecoverDelay;
                if (Current <= 0f)
                    IsExhausted = true;
            }
            else if (recoverDelayRemaining > 0f)
            {
                recoverDelayRemaining -= deltaTime;
            }
            else
            {
                Current = Mathf.Min(1f, Current + SprintRules.RecoverPerSecond * deltaTime);
            }

            return sprinting;
        }
    }
}
