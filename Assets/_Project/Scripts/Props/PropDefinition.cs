using System;
using UnityEngine;

namespace GuardVsMannequin.Props
{
    /// <summary>
    /// 마네킹이 변신할 수 있는 사물 한 종류의 외형 정보.
    /// </summary>
    [Serializable]
    public class PropDefinition
    {
        [Tooltip("에디터 표시용 이름.")]
        public string displayName;

        public Mesh mesh;
        public Material[] materials;

        [Tooltip("변신 시 적용할 크기. 벽 조각은 원본 크기와 관계없이 이 크기로 고정된다.")]
        public Vector3 scale = Vector3.one;

        /// <summary>
        /// 크기가 적용된 메시 경계. 변신 시 충돌체 크기 계산에 사용한다.
        /// </summary>
        public Bounds GetScaledBounds()
        {
            if (mesh == null)
                return new Bounds(Vector3.up * 0.5f, Vector3.one);

            Bounds bounds = mesh.bounds;
            return new Bounds(Vector3.Scale(bounds.center, scale), Vector3.Scale(bounds.size, scale));
        }
    }
}
