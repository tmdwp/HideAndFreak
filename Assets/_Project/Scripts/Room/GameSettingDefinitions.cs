using System;
using System.Collections.Generic;
using System.Globalization;
using GuardVsMannequin.Core;
using GuardVsMannequin.Localization;
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
        MannequinAttackRange,
        GuardMoveSpeed,
        MannequinMoveSpeed
    }

    /// <summary>
    /// 설정 항목의 조정 단위, 범위, 표시 순서와 GameSettings 읽기/쓰기를 정의한다.
    /// 조정 단위·범위·순서는 Resources/Data/game_settings.csv에서, 이름·단위 문구는 strings.csv에서 읽는다.
    /// 룸 UI(표시)와 RoomState(서버 조정)가 같은 정의를 사용한다.
    /// 인원에 따라 달라지는 최종 범위는 <see cref="GameSettingsValidator"/>가 다시 보정한다.
    /// </summary>
    public static class GameSettingDefinitions
    {
        private const string TablePath = "Data/game_settings";

        /// <summary>
        /// 설정 항목 하나의 정의.
        /// </summary>
        public readonly struct Definition
        {
            public readonly GameSettingId id;
            public readonly float step;
            public readonly float min;
            public readonly float max;
            public readonly string unitKey;

            /// <summary>정의를 만든다.</summary>
            public Definition(GameSettingId id, float step, float min, float max, string unitKey)
            {
                this.id = id;
                this.step = step;
                this.min = min;
                this.max = max;
                this.unitKey = unitKey;
            }

            /// <summary>현재 언어의 항목 이름.</summary>
            public string Label => Loc.Get("setting." + id);
        }

        /// <summary>CSV를 읽지 못했거나 빠진 항목에 쓰는 기본 정의.</summary>
        private static readonly Definition[] fallbackDefinitions =
        {
            new(GameSettingId.Map, 1f, 0f, 0f, string.Empty),
            new(GameSettingId.PreparationTime, 5f, GameSettingsValidator.MinPreparationTime, GameSettingsValidator.MaxPreparationTime, "unit.seconds"),
            new(GameSettingId.PlayTime, 30f, GameSettingsValidator.MinPlayTime, GameSettingsValidator.MaxPlayTime, "unit.seconds"),
            new(GameSettingId.HuntTime, 15f, 30f, 180f, "unit.seconds"),
            new(GameSettingId.GuardCount, 1f, 1f, GameConstants.MaxPlayers / 2, "unit.people"),
            new(GameSettingId.ObjectiveCount, 1f, 0f, GameConstants.MaxPlayers + 2, "unit.times"),
            new(GameSettingId.GeneratorCount, 1f, 1f, 12f, "unit.count"),
            new(GameSettingId.GeneratorDestroyTime, 1f, 3f, 30f, "unit.seconds"),
            new(GameSettingId.GunCooldown, 0.5f, 0.5f, 10f, "unit.seconds"),
            new(GameSettingId.DetectorCooldown, 1f, 3f, 30f, "unit.seconds"),
            new(GameSettingId.GuardMaxHealth, 1f, 1f, 10f, string.Empty),
            new(GameSettingId.MannequinMaxHealth, 1f, 1f, 5f, string.Empty),
            new(GameSettingId.HuntHealthMultiplier, 1f, 1f, 20f, "unit.multiplier"),
            new(GameSettingId.MannequinAttackRange, 0.5f, 1f, 5f, "unit.meters"),
            new(GameSettingId.GuardMoveSpeed, 0.1f, GameSettingsValidator.MinMoveSpeed, GameSettingsValidator.MaxMoveSpeed, "unit.speed"),
            new(GameSettingId.MannequinMoveSpeed, 0.1f, GameSettingsValidator.MinMoveSpeed, GameSettingsValidator.MaxMoveSpeed, "unit.speed")
        };

        private static List<Definition> definitions;

        /// <summary>표시 순서대로 나열한 모든 설정 항목. (처음 사용할 때 CSV를 읽음)</summary>
        public static IReadOnlyList<Definition> All
        {
            get
            {
                definitions ??= LoadDefinitions();
                return definitions;
            }
        }

        /// <summary>
        /// 다음 사용 때 CSV를 다시 읽도록 캐시를 비운다. (에디터 도구가 CSV 수정 내용을 바로 반영할 때 사용)
        /// </summary>
        public static void ClearCache()
        {
            definitions = null;
        }

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
                GameSettingId.GuardMoveSpeed => settings.guardMoveSpeed,
                GameSettingId.MannequinMoveSpeed => settings.mannequinMoveSpeed,
                _ => 0f
            };
        }

        /// <summary>
        /// 항목 값을 한 단계 올리거나 내린 설정을 반환한다. (항목 범위로 제한)
        /// </summary>
        public static GameSettings Adjust(GameSettings settings, GameSettingId id, int direction)
        {
            Definition definition = Get(id);
            float value = GetValue(settings, id) + definition.step * Mathf.Sign(direction);
            // 0.1 단위 조정에서 생기는 부동소수 오차 제거
            value = (float)Math.Round(value, 2);
            return SetValue(settings, id, Mathf.Clamp(value, definition.min, definition.max));
        }

        /// <summary>
        /// 화면에 표시할 값 문자열을 만든다. (단위 문구는 현재 언어)
        /// </summary>
        public static string Format(GameSettings settings, GameSettingId id)
        {
            float value = GetValue(settings, id);
            if (id == GameSettingId.Map)
                return Loc.Get("map." + Mathf.RoundToInt(value));

            Definition definition = Get(id);
            string number = Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
            return string.IsNullOrEmpty(definition.unitKey) ? number : Loc.Format(definition.unitKey, number);
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
                case GameSettingId.GuardMoveSpeed: settings.guardMoveSpeed = value; break;
                case GameSettingId.MannequinMoveSpeed: settings.mannequinMoveSpeed = value; break;
            }
            return settings;
        }

        /// <summary>
        /// CSV에서 항목 정의를 읽는다. 읽을 수 없는 줄은 건너뛰고, 빠진 항목은 기본 정의를 뒤에 붙인다.
        /// </summary>
        private static List<Definition> LoadDefinitions()
        {
            var result = new List<Definition>();
            TextAsset asset = Resources.Load<TextAsset>(TablePath);
            if (asset == null)
            {
                Debug.LogWarning($"[GameSettingDefinitions] Resources/{TablePath}.csv 파일이 없어 기본 정의를 사용합니다.");
                result.AddRange(fallbackDefinitions);
                return result;
            }

            List<string[]> rows = CsvParser.Parse(asset.text);
            // 0번 행은 머리글(id,step,min,max,unit)
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                if (TryParseRow(rows[rowIndex], out Definition definition) && !Contains(result, definition.id))
                    result.Add(definition);
                else
                    Debug.LogWarning($"[GameSettingDefinitions] game_settings.csv {rowIndex + 1}번째 데이터 줄을 읽을 수 없어 건너뜁니다: {string.Join(",", rows[rowIndex])}");
            }

            foreach (Definition fallback in fallbackDefinitions)
            {
                if (!Contains(result, fallback.id))
                    result.Add(fallback);
            }
            return result;
        }

        /// <summary>
        /// CSV 한 줄을 정의로 바꾼다. (id, step, min, max, unit)
        /// </summary>
        private static bool TryParseRow(string[] row, out Definition definition)
        {
            definition = default;
            if (row.Length < 4 || !Enum.TryParse(row[0], out GameSettingId id))
                return false;
            if (!TryParseFloat(row[1], out float step) || !TryParseFloat(row[2], out float min) || !TryParseFloat(row[3], out float max))
                return false;
            if (step <= 0f || max < min)
                return false;

            string unitKey = row.Length > 4 ? row[4] : string.Empty;
            definition = new Definition(id, step, min, max, unitKey);
            return true;
        }

        /// <summary>
        /// 시스템 지역 설정과 관계없이 소수점(.)으로 숫자를 읽는다.
        /// </summary>
        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// 목록에 같은 항목이 이미 있는지 확인한다.
        /// </summary>
        private static bool Contains(List<Definition> list, GameSettingId id)
        {
            foreach (Definition item in list)
            {
                if (item.id == id)
                    return true;
            }
            return false;
        }
    }
}
