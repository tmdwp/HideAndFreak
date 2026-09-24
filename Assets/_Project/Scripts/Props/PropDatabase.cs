using System.Collections.Generic;
using UnityEngine;

namespace GuardVsMannequin.Props
{
    /// <summary>
    /// 변신 가능한 사물 목록. 목록의 인덱스가 네트워크로 동기화되는 PropId다.
    /// 맵마다 다른 데이터베이스를 쓸 수 있도록 씬과 분리된 에셋으로 관리한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PropDatabase", menuName = "GuardVsMannequin/Prop Database")]
    public class PropDatabase : ScriptableObject
    {
        /// <summary>변신하지 않은 상태를 나타내는 PropId.</summary>
        public const int NoProp = -1;

        [SerializeField] private List<PropDefinition> definitions = new();

        /// <summary>등록된 사물 수.</summary>
        public int Count => definitions.Count;

        /// <summary>
        /// PropId가 유효한 범위인지 확인한다.
        /// </summary>
        public bool IsValid(int propId)
        {
            return propId >= 0 && propId < definitions.Count && definitions[propId] != null && definitions[propId].mesh != null;
        }

        /// <summary>
        /// PropId에 해당하는 정의를 찾는다.
        /// </summary>
        public bool TryGet(int propId, out PropDefinition definition)
        {
            definition = IsValid(propId) ? definitions[propId] : null;
            return definition != null;
        }

        /// <summary>
        /// 정의를 추가하고 새 PropId를 반환한다. (에디터 툴 전용)
        /// </summary>
        public int Add(PropDefinition definition)
        {
            definitions.Add(definition);
            return definitions.Count - 1;
        }

        /// <summary>
        /// 모든 정의를 지운다. (에디터 툴 전용)
        /// </summary>
        public void Clear()
        {
            definitions.Clear();
        }
    }
}
