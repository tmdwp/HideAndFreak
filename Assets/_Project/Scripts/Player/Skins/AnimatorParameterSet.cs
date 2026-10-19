using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 애니메이터와 그 컨트롤러에 있는 파라미터 이름 목록. 있는 파라미터만 설정한다.
    /// (외형마다 컨트롤러가 달라 없는 파라미터를 설정하면 경고가 쌓이므로)
    /// </summary>
    public sealed class AnimatorParameterSet
    {
        private readonly HashSet<string> names = new();

        /// <summary>대상 애니메이터.</summary>
        public Animator Animator { get; }

        /// <param name="animator">파라미터를 읽어 둘 애니메이터. (컨트롤러를 지정한 뒤 만든다)</param>
        public AnimatorParameterSet(Animator animator)
        {
            Animator = animator;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                names.Add(parameter.name);
        }

        /// <summary>
        /// 해당 이름의 파라미터가 있는지 확인한다.
        /// </summary>
        public bool Has(string parameterName)
        {
            return names.Contains(parameterName);
        }

        /// <summary>
        /// 실수 파라미터가 있으면 설정한다.
        /// </summary>
        public void SetFloatIfExists(string parameterName, float value)
        {
            if (Has(parameterName))
                Animator.SetFloat(parameterName, value);
        }

        /// <summary>
        /// 트리거가 있으면 발동하고 true, 없으면 false.
        /// </summary>
        public bool TrySetTrigger(string parameterName)
        {
            if (!Has(parameterName))
                return false;
            Animator.SetTrigger(parameterName);
            return true;
        }

        /// <summary>
        /// 정수 파라미터가 있으면 설정하고 true, 없으면 false.
        /// </summary>
        public bool TrySetInteger(string parameterName, int value)
        {
            if (!Has(parameterName))
                return false;
            Animator.SetInteger(parameterName, value);
            return true;
        }

        /// <summary>
        /// 논리 파라미터가 있으면 설정하고 true, 없으면 false.
        /// </summary>
        public bool TrySetBool(string parameterName, bool value)
        {
            if (!Has(parameterName))
                return false;
            Animator.SetBool(parameterName, value);
            return true;
        }
    }
}
