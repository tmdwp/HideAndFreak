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

        [Tooltip("애니메이션 이벤트(클립 안에서 함수를 부르는 표시)를 실행할지 여부.\n" +
                 "에셋 팩 애니메이션에는 팩 전용 스크립트를 부르는 이벤트가 들어 있는 경우가 많아, 받을 곳이 없으면 오류가 난다. " +
                 "이벤트를 받는 컴포넌트를 직접 만들었을 때만 켠다.")]
        public bool fireAnimationEvents;

        [Tooltip("총을 드는 외형(가드)의 손 위치 설정. 모델마다 손 크기·뼈 방향이 달라 외형별로 둔다.\n" +
                 "Play 중 이 에셋의 값을 바꾸면 바로 반영되고, Play를 꺼도 값이 유지된다.")]
        public WeaponGrip weaponGrip = new WeaponGrip();
    }

    /// <summary>
    /// 3인칭 총을 손에 들리는 위치와 조준 IK 손 위치. (<see cref="Guard.GuardWeaponView"/>가 매 프레임 읽는다)
    /// </summary>
    [System.Serializable]
    public class WeaponGrip
    {
        [Tooltip("켜면 손 뼈의 회전을 따르고, 끄면 조준 방향을 향한다.")]
        public bool followHandRotation;

        [Tooltip("손 위치에서의 총 위치 보정. 조준 방향(또는 손 뼈) 좌표.")]
        public Vector3 handPositionOffset = new Vector3(0f, 0.02f, 0.06f);

        [Tooltip("총 회전 보정(도).")]
        public Vector3 handRotationOffset = Vector3.zero;

        [Tooltip("3인칭 총 크기 배율.")]
        public float gunScale = 1f;

        [Tooltip("조준 IK: 카메라 피벗 기준 오른손 위치.")]
        public Vector3 ikHandPosition = new Vector3(0.18f, -0.15f, 0.45f);
    }
}
