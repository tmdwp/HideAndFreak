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

        [Tooltip("변신 시 적용할 기울기(오일러 각). 좌우 방향(Y축 회전)은 빼고 저장한다. " +
                 "모델 파일 자체가 눕혀져 있고 배치에서 세운 사물(예: X -90°)이 변신 후에도 서 있게 한다.")]
        public Vector3 rotation = Vector3.zero;

        /// <summary>변신 외형에 적용할 회전.</summary>
        public Quaternion Rotation => Quaternion.Euler(rotation);

        /// <summary>
        /// 크기와 기울기가 적용된 메시 경계. 변신 시 배치와 충돌체 크기 계산에 사용한다.
        /// </summary>
        public Bounds GetScaledBounds()
        {
            if (mesh == null)
                return new Bounds(Vector3.up * 0.5f, Vector3.one);

            Bounds local = mesh.bounds;
            Quaternion orientation = Rotation;
            Bounds result = default;
            for (int i = 0; i < 8; i++)
            {
                // 경계 상자의 꼭짓점 8개를 크기·회전 적용 후 다시 감싼다.
                Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 point = orientation * Vector3.Scale(corner, scale);
                if (i == 0)
                    result = new Bounds(point, Vector3.zero);
                else
                    result.Encapsulate(point);
            }
            return result;
        }

        /// <summary>
        /// 월드 회전에서 좌우 방향(월드 Y축 회전)을 뺀 기울기만 구한다.
        /// rotation = yaw * tilt 로 보고, tilt = twistY(rotation⁻¹) * rotation 으로 계산한다.
        /// </summary>
        public static Quaternion ExtractTilt(Quaternion worldRotation)
        {
            Quaternion inverse = Quaternion.Inverse(worldRotation);
            Vector2 twist = new Vector2(inverse.y, inverse.w);
            // Y축 성분이 없으면(수평축 180° 뒤집힘) 좌우 방향을 정할 수 없으므로 그대로 쓴다.
            if (twist.sqrMagnitude < 1e-6f)
                return worldRotation;

            twist.Normalize();
            Quaternion yawInverse = new Quaternion(0f, twist.x, 0f, twist.y);
            return yawInverse * worldRotation;
        }
    }
}
