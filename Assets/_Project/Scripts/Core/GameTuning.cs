using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 밸런스 조정용 수치. 호스트가 룸에서 바꾸는 값(<see cref="GameSettings"/>)이 아닌, 개발 중에 다듬는 고정 수치를 모은다.
    /// 값은 Resources/Data/game_tuning.csv(빌드 후에는 GameData/Data/game_tuning.csv)에서 읽는다. (<see cref="DataFiles"/>)
    /// 파일에 없는 항목은 여기 적힌 코드 기본값을 쓴다.
    ///
    /// 항목 추가 방법: 아래에 속성 한 줄(이름 = csv의 id, 코드 기본값)을 더하고 game_tuning.csv에 같은 id로 한 줄을 더한다.
    /// 파일 내용이 다른 PC끼리는 판정이 어긋나므로, 파일 내용 지문(<see cref="Fingerprint"/>)을 게임 버전에 넣어 접속을 막는다.
    /// </summary>
    public static class GameTuning
    {
        private const string FilePath = "Data/game_tuning";
        private const string Extension = ".csv";

        private static Dictionary<string, float> values;
        private static string fingerprint;

        #region 이동

        /// <summary>사냥 시간 마네킹 이동 속도 배율. (호스트 설정의 마네킹 속도 × 이 값)</summary>
        public static float MannequinHuntSpeedMultiplier => Get(nameof(MannequinHuntSpeedMultiplier), 0.8f);

        /// <summary>달리기 중 이동 속도 배율.</summary>
        public static float SprintSpeedMultiplier => Get(nameof(SprintSpeedMultiplier), 1.6f);

        /// <summary>스태미나가 가득 찬 상태에서 쉬지 않고 달릴 수 있는 시간(초).</summary>
        public static float StaminaSprintSeconds => Get(nameof(StaminaSprintSeconds), 5f);

        /// <summary>스태미나가 바닥에서 가득 찰 때까지 걸리는 시간(초).</summary>
        public static float StaminaRecoverSeconds => Get(nameof(StaminaRecoverSeconds), 8f);

        /// <summary>달리기를 멈춘 뒤 스태미나 회복이 시작되기까지의 시간(초).</summary>
        public static float StaminaRecoverDelay => Get(nameof(StaminaRecoverDelay), 1f);

        /// <summary>스태미나를 다 쓴 뒤 다시 달릴 수 있게 되는 회복 비율(0~1).</summary>
        public static float StaminaRestartRatio => Get(nameof(StaminaRestartRatio), 0.25f);

        /// <summary>점프 순간 위쪽 속도(m/s). 점프 높이 ≈ 값² ÷ 19.6.</summary>
        public static float JumpImpulse => Get(nameof(JumpImpulse), 5f);

        #endregion

        #region 마네킹

        /// <summary>변신 대상을 지정할 수 있는 최대 거리(m).</summary>
        public static float TransformRange => Get(nameof(TransformRange), 8f);

        /// <summary>변신 해제 후 다시 변신할 수 있을 때까지의 시간(초).</summary>
        public static float UntransformCooldown => Get(nameof(UntransformCooldown), 5f);

        /// <summary>전원 장치와 상호작용할 수 있는 최대 거리(m).</summary>
        public static float InteractRange => Get(nameof(InteractRange), 2.2f);

        /// <summary>여러 마네킹이 함께 파괴할 때의 속도 증가 밑값. (인원이 늘수록 이 값의 거듭제곱으로 빨라짐)</summary>
        public static float CoopDestroySpeedBase => Get(nameof(CoopDestroySpeedBase), 1.4f);

        /// <summary>사냥 시간 마네킹 공격 쿨타임(초).</summary>
        public static float MannequinAttackCooldown => Get(nameof(MannequinAttackCooldown), 1f);

        /// <summary>마네킹 공격 판정 구체의 반경(m).</summary>
        public static float MannequinAttackRadius => Get(nameof(MannequinAttackRadius), 0.35f);

        /// <summary>변신 중 주기적 소음 간격의 최솟값(초).</summary>
        public static float DisguiseNoiseIntervalMin => Get(nameof(DisguiseNoiseIntervalMin), 8f);

        /// <summary>변신 중 주기적 소음 간격의 최댓값(초).</summary>
        public static float DisguiseNoiseIntervalMax => Get(nameof(DisguiseNoiseIntervalMax), 14f);

        /// <summary>감정 표현 한 번의 재생 시간(초). 실제/가짜 마네킹이 같아야 구분되지 않는다.</summary>
        public static float EmoteDuration => Get(nameof(EmoteDuration), 1.5f);

        #endregion

        #region 사냥 시간 마네킹 시야

        /// <summary>마네킹 본인 화면에만 켜지는 개인 조명의 범위(m).</summary>
        public static float HuntVisionLightRange => Get(nameof(HuntVisionLightRange), 12f);

        /// <summary>개인 조명의 밝기.</summary>
        public static float HuntVisionLightIntensity => Get(nameof(HuntVisionLightIntensity), 1.5f);

        /// <summary>검은 안개가 시작되는 거리(m).</summary>
        public static float HuntVisionFogStart => Get(nameof(HuntVisionFogStart), 3f);

        /// <summary>이 거리부터 완전히 보이지 않는다(m).</summary>
        public static float HuntVisionFogEnd => Get(nameof(HuntVisionFogEnd), 13.5f);

        #endregion

        #region 가드

        /// <summary>가드 탐지기 반경(m).</summary>
        public static float DetectorRadius => Get(nameof(DetectorRadius), 10f);

        /// <summary>손전등 범위(m).</summary>
        public static float FlashlightRange => Get(nameof(FlashlightRange), 25f);

        /// <summary>손전등 퍼짐 각도(도).</summary>
        public static float FlashlightAngle => Get(nameof(FlashlightAngle), 55f);

        #endregion

        #region 가짜 마네킹·진행

        /// <summary>게임 시작 시 생성할 가짜 마네킹 수의 최솟값.</summary>
        public static int FakeMannequinMinCount => GetInt(nameof(FakeMannequinMinCount), 6);

        /// <summary>게임 시작 시 생성할 가짜 마네킹 수의 최댓값.</summary>
        public static int FakeMannequinMaxCount => GetInt(nameof(FakeMannequinMaxCount), 12);

        /// <summary>탐색 단계 남은 시간이 이 값(초) 이하가 되면 가짜 마네킹을 정리한다.</summary>
        public static float FakeMannequinPurgeTime => Get(nameof(FakeMannequinPurgeTime), 20f);

        /// <summary>목표 0회 판에서 탐색 시작 후 헌트 타임까지의 가드 보호 시간(초).</summary>
        public static float ZeroObjectiveHuntDelay => Get(nameof(ZeroObjectiveHuntDelay), 20f);

        /// <summary>결과 화면을 보여준 뒤 룸으로 돌아가기까지의 시간(초).</summary>
        public static float ResultDisplayDuration => Get(nameof(ResultDisplayDuration), 10f);

        #endregion

        /// <summary>
        /// 파일에 적힌 값들의 지문(8자리 16진수). 게임 버전에 붙여 같은 값을 쓰는 PC끼리만 함께 플레이하게 한다.
        /// </summary>
        public static string Fingerprint
        {
            get
            {
                EnsureLoaded();
                return fingerprint;
            }
        }

        /// <summary>
        /// 플레이 모드에 들어갈 때마다 다시 읽는다. (에디터에서 도메인 재로드를 끈 경우에도 파일 수정이 반영되도록)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            values = null;
            fingerprint = null;
        }

        /// <summary>
        /// 실수 항목을 읽는다. 파일에 없으면 코드 기본값.
        /// </summary>
        private static float Get(string id, float fallback)
        {
            EnsureLoaded();
            return values.TryGetValue(id, out float value) ? value : fallback;
        }

        /// <summary>
        /// 정수 항목을 읽는다. 파일 값은 반올림한다.
        /// </summary>
        private static int GetInt(string id, int fallback)
        {
            return Mathf.RoundToInt(Get(id, fallback));
        }

        /// <summary>
        /// 처음 한 번 파일을 읽고 지문을 만든다. 파일을 쓸 수 없으면 모두 코드 기본값을 쓴다.
        /// </summary>
        private static void EnsureLoaded()
        {
            if (values != null)
                return;

            if (!DataFiles.TryLoad<Dictionary<string, float>>(FilePath, Extension, TryParse, out values))
            {
                Debug.LogWarning($"[GameTuning] {FilePath}{Extension}을 사용할 수 없어 코드 기본값을 사용합니다.");
                values = new Dictionary<string, float>();
            }
            fingerprint = ComputeFingerprint(values);
        }

        /// <summary>
        /// "id,value,설명" 형식을 읽는다. 숫자가 아닌 값·모르는 id는 경고 후 건너뛴다. (한 줄 실수로 파일 전체를 버리지 않도록)
        /// </summary>
        private static bool TryParse(string text, out Dictionary<string, float> result)
        {
            result = new Dictionary<string, float>();
            HashSet<string> knownIds = GetKnownIds();

            foreach (string[] row in CsvParser.Parse(text))
            {
                if (row.Length < 2 || row[0] == "id")
                    continue;

                string id = row[0];
                if (!knownIds.Contains(id))
                {
                    Debug.LogWarning($"[GameTuning] 모르는 항목이라 무시합니다: {id} (이름 오타 확인)");
                    continue;
                }
                if (!float.TryParse(row[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    Debug.LogWarning($"[GameTuning] {id}의 값 '{row[1]}'이 숫자가 아니라 코드 기본값을 사용합니다.");
                    continue;
                }
                result[id] = value;
            }
            return true;
        }

        /// <summary>
        /// 이 클래스에 정의된 항목 이름(공개 정적 속성) 목록. 지문 속성은 제외한다.
        /// </summary>
        private static HashSet<string> GetKnownIds()
        {
            var ids = new HashSet<string>();
            foreach (PropertyInfo property in typeof(GameTuning).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.Name != nameof(Fingerprint))
                    ids.Add(property.Name);
            }
            return ids;
        }

        /// <summary>
        /// 항목을 이름순으로 이어 붙여 FNV-1a 해시를 만든다. (설명·순서·주석이 바뀌어도 값이 같으면 지문이 같음)
        /// </summary>
        private static string ComputeFingerprint(Dictionary<string, float> fileValues)
        {
            var ids = new List<string>(fileValues.Keys);
            ids.Sort(System.StringComparer.Ordinal);

            var builder = new StringBuilder();
            foreach (string id in ids)
                builder.Append(id).Append('=').Append(fileValues[id].ToString("R", CultureInfo.InvariantCulture)).Append(';');

            uint hash = 2166136261;
            foreach (char c in builder.ToString())
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash.ToString("x8");
        }
    }
}
