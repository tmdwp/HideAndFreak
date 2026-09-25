using System.Collections.Generic;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Room
{
    /// <summary>룸에서 조정할 수 있는 게임 설정 항목.</summary>
    public enum GameSettingId : byte
    {
        Map,
        PreparationTime,
        PlayTime,
        HuntTime,
        GuardCount,
        ObjectiveCount,
        GeneratorCount,
        GeneratorDestroyTime,
        GunCooldown,
        DetectorCooldown,
        GuardMaxHealth,
        MannequinMaxHealth,
        HuntHealthMultiplier,
        MannequinAttackRange
    }

    /// <summary>
    /// 설정 항목의 표시 이름, 조정 단위, 범위와 GameSettings 읽기/쓰기를 정의한다.
    /// 룸 UI(표시)와 RoomState(서버 조정)가 같은 정의를 사용한다.
    /// 인원에 따라 달라지는 최종 범위는 <see cref="GameSettingsValidator"/>가 다시 보정한다.
    /// </summary>
    public static class GameSettingDefinitions
    {
        /// <summary>
        /// 설정 항목 하나의 정의.
        /// </summary>
        public readonly struct Definition
        {
            public readonly GameSettingId id;
            public readonly string label;
            public readonly float step;
            public readonly float min;
            public readonly float max;
            public readonly string unit;

            /// <summary>정의를 만든다.</summary>
            public Definition(GameSettingId id, string label, float step, float min, float max, string unit)
            {
                this.id = id;
                this.label = label;
                this.step = step;
                this.min = min;
                this.max = max;
                this.unit = unit;
            }
        }

        /// <summary>현재 선택할 수 있는 맵 이름. (인덱스 = mapId)</summary>
        public static readonly string[] MapNames = { "테스트 맵" };

        /// <summary>표시 순서대로 나열한 모든 설정 항목.</summary>
        public static readonly IReadOnlyList<Definition> All = new[]
        {
            new Definition(GameSettingId.Map, "맵", 1f, 0f, MapNames.Length - 1, string.Empty),
            new Definition(GameSettingId.PreparationTime, "가드 대기 시간", 5f, GameSettingsValidator.MinPreparationTime, GameSettingsValidator.MaxPreparationTime, "초"),
            new Definition(GameSettingId.PlayTime, "제한 시간", 30f, GameSettingsValidator.MinPlayTime, GameSettingsValidator.MaxPlayTime, "초"),
            new Definition(GameSettingId.HuntTime, "사냥 시간", 15f, 30f, 180f, "초"),
            new Definition(GameSettingId.GuardCount, "가드 수", 1f, 1f, GameConstants.MaxPlayers / 2, "명"),
            new Definition(GameSettingId.ObjectiveCount, "목표 수행 횟수", 1f, 0f, GameConstants.MaxPlayers + 2, "회"),
            new Definition(GameSettingId.GeneratorCount, "전원 장치 수", 1f, 1f, 12f, "개"),
            new Definition(GameSettingId.GeneratorDestroyTime, "전원 장치 파괴 시간", 1f, 3f, 30f, "초"),
            new Definition(GameSettingId.GunCooldown, "총 쿨타임", 0.5f, 0.5f, 10f, "초"),
            new Definition(GameSettingId.DetectorCooldown, "탐지기 쿨타임", 1f, 3f, 30f, "초"),
            new Definition(GameSettingId.GuardMaxHealth, "가드 체력", 1f, 1f, 10f, string.Empty),
            new Definition(GameSettingId.MannequinMaxHealth, "마네킹 체력", 1f, 1f, 5f, string.Empty),
            new Definition(GameSettingId.HuntHealthMultiplier, "사냥 시간 마네킹 체력 배율", 1f, 1f, 20f, "배"),
            new Definition(GameSettingId.MannequinAttackRange, "마네킹 공격 범위", 0.5f, 1f, 5f, "m")
        };

        /// <summary>
        /// 항목 정의를 찾는다.
        /// </summary>
        public static Definition Get(GameSettingId id)
        {
            foreach (Definition definition in All)
            {
                if (definition.id == id)
                    return definition;
            }
            return All[0];
        }

        /// <summary>
        /// 설정에서 항목 값을 읽는다.
        /// </summary>
        public static float GetValue(GameSettings settings, GameSettingId id)
        {
            return id switch
            {
                GameSettingId.Map => settings.mapId,
                GameSettingId.PreparationTime => settings.preparationTime,
                GameSettingId.PlayTime => settings.playTime,
                GameSettingId.HuntTime => settings.huntTime,
                GameSettingId.GuardCount => settings.guardCount,
                GameSettingId.ObjectiveCount => settings.objectiveCount,
                GameSettingId.GeneratorCount => settings.generatorCount,
                GameSettingId.GeneratorDestroyTime => settings.generatorDestroyTime,
                GameSettingId.GunCooldown => settings.gunCooldown,
                GameSettingId.DetectorCooldown => settings.detectorCooldown,
                GameSettingId.GuardMaxHealth => settings.guardMaxHealth,
                GameSettingId.MannequinMaxHealth => settings.mannequinMaxHealth,
                GameSettingId.HuntHealthMultiplier => settings.huntHealthMultiplier,
                GameSettingId.MannequinAttackRange => settings.mannequinAttackRange,
                _ => 0f
            };
        }

        /// <summary>
        /// 항목 값을 한 단계 올리거나 내린 설정을 반환한다. (항목 범위로 제한)
        /// </summary>
        public static GameSettings Adjust(GameSettings settings, GameSettingId id, int direction)
        {
            Definition definition = Get(id);
            float value = Mathf.Clamp(GetValue(settings, id) + definition.step * Mathf.Sign(direction), definition.min, definition.max);
            return SetValue(settings, id, value);
        }

        /// <summary>
        /// 화면에 표시할 값 문자열을 만든다.
        /// </summary>
        public static string Format(GameSettings settings, GameSettingId id)
        {
            float value = GetValue(settings, id);
            if (id == GameSettingId.Map)
            {
                int index = Mathf.Clamp(Mathf.RoundToInt(value), 0, MapNames.Length - 1);
                return MapNames[index];
            }

            Definition definition = Get(id);
            string number = Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
            return number + definition.unit;
        }

        /// <summary>
        /// 설정에 항목 값을 쓴 결과를 반환한다. (정수 항목은 반올림)
        /// </summary>
        private static GameSettings SetValue(GameSettings settings, GameSettingId id, float value)
        {
            int rounded = Mathf.RoundToInt(value);
            switch (id)
            {
                case GameSettingId.Map: settings.mapId = rounded; break;
                case GameSettingId.PreparationTime: settings.preparationTime = value; break;
                case GameSettingId.PlayTime: settings.playTime = value; break;
                case GameSettingId.HuntTime: settings.huntTime = value; break;
                case GameSettingId.GuardCount: settings.guardCount = rounded; break;
                case GameSettingId.ObjectiveCount: settings.objectiveCount = rounded; break;
                case GameSettingId.GeneratorCount: settings.generatorCount = rounded; break;
                case GameSettingId.GeneratorDestroyTime: settings.generatorDestroyTime = value; break;
                case GameSettingId.GunCooldown: settings.gunCooldown = value; break;
                case GameSettingId.DetectorCooldown: settings.detectorCooldown = value; break;
                case GameSettingId.GuardMaxHealth: settings.guardMaxHealth = rounded; break;
                case GameSettingId.MannequinMaxHealth: settings.mannequinMaxHealth = rounded; break;
                case GameSettingId.HuntHealthMultiplier: settings.huntHealthMultiplier = value; break;
                case GameSettingId.MannequinAttackRange: settings.mannequinAttackRange = value; break;
            }
            return settings;
        }
    }
}
