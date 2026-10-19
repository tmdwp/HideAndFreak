using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuardVsMannequin.Map
{
    /// <summary>
    /// 사냥 시간에 꺼지지 않고 깜빡이며 남을 맵 조명을 고른다. (값만 반환)
    /// 조명 위치로 정하므로 같은 맵이면 모든 PC에서 같은 조명이 골라진다. (보이는 밝은 곳이 PC마다 달라지지 않게)
    /// </summary>
    public static class HuntFlickerSelector
    {
        // 위치 반올림 단위(cm)
        private const float PositionPrecision = 100f;

        /// <summary>
        /// 조명 중 비율(0~1)만큼을 골라 반환한다. 수는 반올림한다.
        /// </summary>
        public static HashSet<MapLight> Choose(IReadOnlyList<MapLight> mapLights, float ratio)
        {
            List<MapLight> valid = mapLights.Where(mapLight => mapLight != null).ToList();
            int count = Mathf.RoundToInt(Mathf.Clamp01(ratio) * valid.Count);
            return valid
                .OrderBy(mapLight => PositionHash(mapLight.transform.position))
                .ThenBy(mapLight => mapLight.transform.position.x)
                .ThenBy(mapLight => mapLight.transform.position.z)
                .Take(count)
                .ToHashSet();
        }

        /// <summary>
        /// 위치를 cm 단위 정수로 바꿔 섞은 값. (FNV-1a, PC·실행마다 같은 값)
        /// </summary>
        private static uint PositionHash(Vector3 position)
        {
            uint hash = 2166136261u;
            foreach (float axis in new[] { position.x, position.y, position.z })
            {
                int value = Mathf.RoundToInt(axis * PositionPrecision);
                for (int shift = 0; shift < 32; shift += 8)
                {
                    hash ^= (uint)(value >> shift) & 0xFFu;
                    hash *= 16777619u;
                }
            }
            return hash;
        }
    }
}
