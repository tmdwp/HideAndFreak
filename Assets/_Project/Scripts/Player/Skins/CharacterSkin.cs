using UnityEngine;

namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 캐릭터 외형 하나(모델 + 애니메이터). 마네킹은 맵마다 다른 외형을 쓸 수 있다(<see cref="Map.MapInfo"/>).
    /// 모델 프리팹은 "캐릭터·무기 에셋 생성" 도구가 발 위치를 원점에 맞추고 스크립트·충돌체를 제거해 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "GuardVsMannequin/Character Skin", fileName = "Skin_")]
    public class CharacterSkin : ScriptableObject
    {
        [Tooltip("표시 이름. (인스펙터 확인용)")]
        public string displayName;

        [Tooltip("외형 모델 프리팹. 발이 원점, 정면이 +Z여야 한다.")]
        public GameObject modelPrefab;

        [Tooltip("모델에 적용할 애니메이터 컨트롤러. 비어 있으면 모델에 있던 컨트롤러를 그대로 쓴다.")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("모델의 정면이 +Z가 아닐 때 보정할 회전(도).")]
        public float yawOffset;

        [Tooltip("오른손이 조준 방향을 향하도록 IK를 쓸지 여부. (총을 든 가드)")]
        public bool useAimIK;
    }
}
