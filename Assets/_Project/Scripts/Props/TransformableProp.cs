using UnityEngine;

namespace GuardVsMannequin.Props
{
    /// <summary>
    /// 마네킹이 변신 대상으로 지정할 수 있는 사물 표시.
    /// 천장, 바닥, 조명 기구, 전원 장치와 그 주변 구조물에는 붙이지 않는다.
    /// </summary>
    public class TransformableProp : MonoBehaviour
    {
        [Tooltip("PropDatabase의 인덱스. 벽은 원본 크기와 관계없이 고정 크기 벽 조각 ID를 사용한다.")]
        [SerializeField] private int propId = PropDatabase.NoProp;

        /// <summary>변신 시 사용할 PropId.</summary>
        public int PropId => propId;

        /// <summary>
        /// PropId를 지정한다. (에디터 툴 전용)
        /// </summary>
        public void SetPropId(int newPropId)
        {
            propId = newPropId;
        }
    }
}
