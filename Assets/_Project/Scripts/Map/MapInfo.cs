using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 맵 단위 정보. 천장 높이가 가드 히트스캔 사거리보다 낮은지 검증한다.
    /// (천장에 붙은 마네킹도 맞출 수 있어야 하기 때문)
    /// </summary>
    public class MapInfo : MonoBehaviour
    {
        [SerializeField] private string mapName = "Test";

        [Tooltip("바닥에서 천장까지의 높이. 천장이 개방된 맵은 투명 천장 높이.")]
        [SerializeField] private float ceilingHeight = 4f;

        public string MapName => mapName;
        public float CeilingHeight => ceilingHeight;

        /// <summary>
        /// 실행 시 천장 높이를 검증한다.
        /// </summary>
        private void Awake()
        {
            Validate();
        }

        /// <summary>
        /// 에디터에서 값이 바뀔 때 천장 높이를 검증한다.
        /// </summary>
        private void OnValidate()
        {
            Validate();
        }

        /// <summary>
        /// 천장 높이가 사거리 이상이면 에러를 남긴다.
        /// </summary>
        private void Validate()
        {
            if (ceilingHeight >= GameConstants.GunRange)
                Debug.LogError($"[MapInfo] {mapName}: 천장 높이({ceilingHeight})가 가드 사거리({GameConstants.GunRange})보다 낮아야 합니다.", this);
        }
    }
}
