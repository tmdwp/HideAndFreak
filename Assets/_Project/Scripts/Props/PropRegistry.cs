using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Props
{
    /// <summary>
    /// 지금 불러온 맵의 변신 사물을 번호로 찾는다. 변신 상태는 이 번호 하나로 동기화된다.
    /// 번호는 에디터가 씬 저장·빌드 때 매기므로(<c>PropIndexer</c>), 같은 맵 빌드끼리는 같은 번호가 같은 물체를 가리킨다.
    /// </summary>
    public static class PropRegistry
    {
        private static readonly Dictionary<int, TransformableProp> props = new();

        /// <summary>
        /// 플레이 모드에 들어갈 때 목록을 비운다. (도메인 재로드를 끈 경우 이전 씬의 항목이 남지 않도록)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            props.Clear();
        }

        /// <summary>
        /// 사물을 등록한다. 번호가 없거나 이미 다른 사물이 쓰는 번호면 경고한다.
        /// </summary>
        public static void Register(TransformableProp prop)
        {
            if (prop.Index == TransformableProp.None)
            {
                Debug.LogWarning($"[PropRegistry] '{prop.name}'에 사물 번호가 없어 변신할 수 없습니다. 맵 씬을 저장하면 번호가 매겨집니다.", prop);
                return;
            }
            if (props.TryGetValue(prop.Index, out TransformableProp existing) && existing != null && existing != prop)
                Debug.LogWarning($"[PropRegistry] 사물 번호 {prop.Index}가 '{existing.name}'과 '{prop.name}'에 중복됩니다. 맵 씬을 다시 저장하세요.", prop);

            props[prop.Index] = prop;
        }

        /// <summary>
        /// 사물 등록을 해제한다. (그 번호에 다른 사물이 등록되어 있으면 건드리지 않음)
        /// </summary>
        public static void Unregister(TransformableProp prop)
        {
            if (props.TryGetValue(prop.Index, out TransformableProp existing) && existing == prop)
                props.Remove(prop.Index);
        }

        /// <summary>
        /// 번호에 해당하는 사물을 찾는다.
        /// </summary>
        public static bool TryGet(int index, out TransformableProp prop)
        {
            return props.TryGetValue(index, out prop) && prop != null;
        }
    }
}
