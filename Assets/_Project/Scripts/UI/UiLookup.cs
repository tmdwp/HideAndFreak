using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 같은 캔버스(부모 쪽)의 UI 코드를 찾는 도우미. 못 찾으면 원인을 알 수 있게 오류를 남긴다.
    /// </summary>
    public static class UiLookup
    {
        /// <summary>
        /// 자신 또는 부모에서 T(인터페이스 가능)를 찾는다. 없으면 오류 로그를 남기고 null을 반환한다.
        /// </summary>
        public static T FindInParent<T>(Component owner) where T : class
        {
            T found = owner.GetComponentInParent<T>(true);
            if (found == null)
                Debug.LogError($"[{owner.GetType().Name}] 같은 캔버스(부모 쪽)에 {typeof(T).Name}가 없습니다. 프리팹의 컴포넌트 배치를 확인하세요.", owner);
            return found;
        }
    }
}
