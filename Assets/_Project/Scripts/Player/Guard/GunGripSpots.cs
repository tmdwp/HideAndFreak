using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 총 프리팹 안에서 손이 잡는 지점을 표시하는 오브젝트 이름과 찾기 규칙.
    /// - Right Hand: 오른손(방아쇠 손잡이). 예전 이름 Spot도 오른손으로 본다.
    /// - Left Hand: 왼손(총열 아래 받침). 있으면 양손으로 잡는다.
    /// 이름은 공백·대소문자를 무시하고 비교하며, 총 프리팹 어느 깊이에 있어도 된다.
    /// </summary>
    public static class GunGripSpots
    {
        public const string RightHand = "Right Hand";
        public const string LeftHand = "Left Hand";
        public const string LegacyRightHand = "Spot";

        /// <summary>
        /// 총 아래에서 해당 손의 지점 오브젝트를 찾는다. 없으면 null.
        /// </summary>
        public static Transform Find(Transform gun, string spotName)
        {
            if (gun == null)
                return null;

            Transform legacy = null;
            foreach (Transform child in gun.GetComponentsInChildren<Transform>(true))
            {
                if (child == gun)
                    continue;
                if (SameName(child.name, spotName))
                    return child;
                if (spotName == RightHand && legacy == null && SameName(child.name, LegacyRightHand))
                    legacy = child;
            }
            return legacy;
        }

        /// <summary>
        /// 총 아래에서 해당 손의 지점을 찾아 총 루트 기준 좌표로 반환한다. 없으면 null.
        /// </summary>
        public static Vector3? FindLocalPoint(Transform gun, string spotName)
        {
            Transform spot = Find(gun, spotName);
            return spot != null ? gun.InverseTransformPoint(spot.position) : null;
        }

        /// <summary>
        /// 공백과 대소문자를 무시하고 이름이 같은지 비교한다. ("Left Hand" = "LeftHand" = "lefthand")
        /// </summary>
        private static bool SameName(string a, string b)
        {
            return string.Equals(a.Replace(" ", ""), b.Replace(" ", ""), System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
