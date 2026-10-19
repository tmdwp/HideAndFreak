using System;
using GuardVsMannequin.Core;
using GuardVsMannequin.Mods;
using UnityEngine;

namespace GuardVsMannequin.Room
{
    /// <summary>
    /// 맵별 룸 설정 규칙(map.json의 settings·인원). 서버의 설정 보정과 시작 조건 확인에 쓴다.
    /// - 맵을 고르면 그 맵이 정한 기본값을 적용한다.
    /// - 설정 값은 "게임 범위 ∩ 맵 범위"로 보정한다(<see cref="GameSettingDefinitions.GetEffective"/>).
    /// - 맵이 정한 인원 범위 밖이면 게임을 시작할 수 없다.
    /// </summary>
    public static class MapSettingRules
    {
        /// <summary>
        /// 맵이 항목의 범위를 정했는지 찾는다.
        /// </summary>
        public static bool TryGetRange(int mapId, GameSettingId id, out MapSettingRange range)
        {
            range = null;
            if (!ContentRegistry.Instance.TryGetMap(mapId, out MapEntry map) || map.Manifest.settings == null)
                return false;

            string name = id.ToString();
            foreach (MapSettingRange candidate in map.Manifest.settings)
            {
                if (candidate != null && string.Equals(candidate.id, name, StringComparison.OrdinalIgnoreCase))
                {
                    range = candidate;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 고른 맵이 정한 기본값을 적용한다. (맵을 바꿀 때, 방을 만들 때)
        /// </summary>
        public static GameSettings ApplyDefaults(GameSettings settings)
        {
            settings = ClampMapId(settings);
            if (!ContentRegistry.Instance.TryGetMap(settings.mapId, out MapEntry map) || map.Manifest.settings == null)
                return settings;

            foreach (MapSettingRange range in map.Manifest.settings)
            {
                if (range != null && Enum.TryParse(range.id, true, out GameSettingId id) && id != GameSettingId.Map)
                    settings = GameSettingDefinitions.SetClamped(settings, id, range.value);
                else
                    Debug.LogWarning($"[MapSettingRules] 맵 '{map.Id}'의 설정 항목 '{range?.id}'을 알 수 없어 무시합니다.");
            }
            return settings;
        }

        /// <summary>
        /// 맵 번호를 맵 목록 범위로 맞추고, 맵이 범위를 정한 항목을 그 범위로 보정한다.
        /// </summary>
        public static GameSettings Clamp(GameSettings settings)
        {
            settings = ClampMapId(settings);
            if (!ContentRegistry.Instance.TryGetMap(settings.mapId, out MapEntry map) || map.Manifest.settings == null)
                return settings;

            foreach (MapSettingRange range in map.Manifest.settings)
            {
                if (range != null && Enum.TryParse(range.id, true, out GameSettingId id) && id != GameSettingId.Map)
                    settings = GameSettingDefinitions.SetClamped(settings, id, GameSettingDefinitions.GetValue(settings, id));
            }
            return settings;
        }

        /// <summary>
        /// 지금 인원으로 고른 맵을 플레이할 수 있는지 확인한다. 맵이 인원을 정하지 않았으면 항상 true.
        /// </summary>
        /// <param name="minPlayers">맵의 최소 인원(정하지 않았으면 게임 최소).</param>
        /// <param name="maxPlayers">맵의 최대 인원(정하지 않았으면 게임 최대).</param>
        public static bool AllowsPlayerCount(GameSettings settings, int playerCount, out int minPlayers, out int maxPlayers)
        {
            minPlayers = GameConstants.MinPlayers;
            maxPlayers = GameConstants.MaxPlayers;
            if (ContentRegistry.Instance.TryGetMap(settings.mapId, out MapEntry map))
            {
                if (map.Manifest.minPlayers > 0)
                    minPlayers = Mathf.Max(minPlayers, map.Manifest.minPlayers);
                if (map.Manifest.maxPlayers > 0)
                    maxPlayers = Mathf.Min(maxPlayers, map.Manifest.maxPlayers);
            }
            return playerCount >= minPlayers && playerCount <= maxPlayers;
        }

        /// <summary>
        /// 맵 번호를 0 ~ (맵 수 - 1)로 맞춘다. (모드를 끈 뒤 남은 번호 방지)
        /// </summary>
        private static GameSettings ClampMapId(GameSettings settings)
        {
            settings.mapId = Mathf.Clamp(settings.mapId, 0, Mathf.Max(0, ContentRegistry.Instance.Maps.Count - 1));
            return settings;
        }
    }
}
