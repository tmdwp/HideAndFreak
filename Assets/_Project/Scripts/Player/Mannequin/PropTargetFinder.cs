using GuardVsMannequin.Props;
using UnityEngine;

namespace GuardVsMannequin.Player.Mannequin
{
    /// <summary>
    /// 조준 광선으로 변신할 수 있는 사물을 찾는다. (변신 사거리 안, 현재 사물 목록에 있는 것만)
    /// 변신 요청(<see cref="MannequinTransformer"/>)과 외곽선 표시(<see cref="PropHighlighter"/>)가 같은 판정을 쓴다.
    /// </summary>
    public sealed class PropTargetFinder
    {
        private readonly Transform owner;
        private readonly IAimProvider aimProvider;
        private readonly IPropDatabaseSource databaseSource;
        private readonly float range;

        /// <param name="owner">아바타 트랜스폼. (자기 몸은 무시하고, 사거리는 몸에서 잰다)</param>
        /// <param name="aimProvider">조준 광선.</param>
        /// <param name="databaseSource">현재 사물 목록.</param>
        /// <param name="range">몸에서 사물까지의 최대 거리(m).</param>
        public PropTargetFinder(Transform owner, IAimProvider aimProvider, IPropDatabaseSource databaseSource, float range)
        {
            this.owner = owner;
            this.aimProvider = aimProvider;
            this.databaseSource = databaseSource;
            this.range = range;
        }

        /// <summary>
        /// 지금 조준하고 있는 변신 가능 사물을 찾는다.
        /// </summary>
        public bool TryFind(out TransformableProp prop)
        {
            prop = null;
            Ray aim = aimProvider.GetAimRay();

            // 3인칭 카메라가 몸 뒤에 있으므로 카메라-몸 거리만큼 사거리를 늘린다.
            float cameraDistance = Vector3.Distance(aim.origin, owner.position);
            if (!AimRaycaster.TryRaycast(aim, range + cameraDistance, owner, out RaycastHit hit))
                return false;
            if (Vector3.Distance(hit.point, owner.position) > range)
                return false;

            TransformableProp candidate = hit.collider.GetComponentInParent<TransformableProp>();
            PropDatabase database = databaseSource.Database;
            if (candidate == null || database == null || !database.IsValid(candidate.PropId))
                return false;

            prop = candidate;
            return true;
        }
    }
}
