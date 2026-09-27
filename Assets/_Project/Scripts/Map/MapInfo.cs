using GuardVsMannequin.Core;
using GuardVsMannequin.Player;
using GuardVsMannequin.Props;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 맵 단위 정보.
    /// - 천장 높이가 가드 히트스캔 사거리보다 낮은지 검증한다. (천장에 붙은 마네킹도 맞출 수 있어야 하기 때문)
    /// - 맵별 캐릭터 외형: 비워 두면 프리팹의 기본 외형을 쓴다. 마네킹 외형을 바꾸면 가짜 마네킹도 같이 바뀐다.
    /// - 맵별 변신 사물 목록: 비워 두면 마네킹 프리팹의 기본 목록을 쓴다.
    /// 씬에 하나만 둔다.
    /// </summary>
    public class MapInfo : MonoBehaviour
    {
        [SerializeField] private string mapName = "Test";

        [Tooltip("바닥에서 천장까지의 높이. 천장이 개방된 맵은 투명 천장 높이.")]
        [SerializeField] private float ceilingHeight = 4f;

        [Tooltip("이 맵에서 마네킹(실제·가짜)이 쓸 외형. 비우면 프리팹의 기본 외형.")]
        [SerializeField] private CharacterSkin mannequinSkin;

        [Tooltip("이 맵에서 가드가 쓸 외형. 비우면 프리팹의 기본 외형.")]
        [SerializeField] private CharacterSkin guardSkin;

        [Tooltip("이 맵의 변신 가능 사물 목록. 비우면 마네킹 프리팹의 기본 목록.")]
        [SerializeField] private PropDatabase propDatabase;

        /// <summary>현재 씬의 맵 정보. 없으면 null.</summary>
        public static MapInfo Current { get; private set; }

        public string MapName => mapName;
        public float CeilingHeight => ceilingHeight;
        public CharacterSkin MannequinSkin => mannequinSkin;
        public CharacterSkin GuardSkin => guardSkin;
        public PropDatabase PropDatabase => propDatabase;

        /// <summary>
        /// 현재 맵으로 등록하고 천장 높이를 검증한다.
        /// (아바타보다 먼저 로드되는 씬 오브젝트이므로, 아바타가 생성될 때는 이미 등록되어 있다)
        /// </summary>
        private void Awake()
        {
            Current = this;
            Validate();
        }

        /// <summary>
        /// 씬이 내려가면 등록을 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
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
