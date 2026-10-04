using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// (사용 안 함) 예전에는 탐색 단계 시작 시 대기실의 가드를 출발 지점으로 옮겼다.
    /// Phase 7부터 가드는 처음부터 맵 안에 스폰되므로 아무 일도 하지 않는다.
    /// 기존 맵 씬의 참조가 깨지지 않도록 "Phase 7 > 게임플레이 개편 적용" 메뉴가 이 컴포넌트를 지울 때까지만 남겨 둔다.
    /// </summary>
    [System.Obsolete("Phase 7: 가드 대기실 제거. 맵 씬에서 지운 뒤 파일도 삭제한다.")]
    public class GuardReleaseTeleporter : MonoBehaviour
    {
    }
}
