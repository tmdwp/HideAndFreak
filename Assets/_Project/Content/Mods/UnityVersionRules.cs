using System;
using System.Text.RegularExpressions;

namespace GuardVsMannequin.Mods
{
    /// <summary>
    /// 모드 번들을 만든 Unity 버전을 게임에서 읽을 수 있는지 판정한다. (게임의 모드 검사와 SDK의 모드 내보내기가 같은 규칙을 쓴다)
    /// - 같은 메이저·마이너 라인(예: 6000.4)이고, 패치가 게임과 같거나 더 옛 버전이면 허용한다. (예: 6000.4.0f1 ~ 게임의 6000.4.2f1)
    /// - 게임보다 새 버전은 직렬화 형식을 읽을 수 없고, 다른 라인은 번들 안 셰이더가 깨지기 쉬워 막는다.
    /// </summary>
    public static class UnityVersionRules
    {
        /// <summary>
        /// 모드를 만든 Unity 버전이 게임 Unity 버전과 호환되는지 확인한다.
        /// </summary>
        /// <param name="modUnity">모드를 만든 Unity 버전. (예: "6000.4.0f1")</param>
        /// <param name="gameUnity">게임의 Unity 버전. 읽을 수 없으면 확인하지 않는다.</param>
        /// <param name="problem">호환되지 않을 때의 이유.</param>
        public static bool IsCompatible(string modUnity, string gameUnity, out string problem)
        {
            problem = null;
            int[] mod = Parse(modUnity);
            int[] game = Parse(gameUnity);
            if (mod == null)
            {
                problem = $"모드를 만든 Unity 버전을 알 수 없습니다({modUnity}). 다시 내보내세요.";
                return false;
            }
            if (game == null)
                return true;

            string line = $"{game[0]}.{game[1]}";
            if (mod[0] != game[0] || mod[1] != game[1])
            {
                problem = $"다른 Unity 버전 라인({modUnity})으로 만든 모드입니다. 게임과 같은 {line}.x 중 {gameUnity} 이하로 다시 내보내야 합니다.";
                return false;
            }
            if (Compare(mod, game) > 0)
            {
                problem = $"게임({gameUnity})보다 새 Unity({modUnity})로 만든 모드입니다. {line}.x 중 {gameUnity} 이하로 다시 내보내야 합니다.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 허용 범위를 사람이 읽는 문구로 만든다. (예: "6000.4.0 ~ 6000.4.2f1")
        /// </summary>
        public static string DescribeRange(string gameUnity)
        {
            int[] game = Parse(gameUnity);
            return game == null ? gameUnity : $"{game[0]}.{game[1]}.0 ~ {gameUnity}";
        }

        /// <summary>
        /// "6000.4.2f1"을 [6000, 4, 2, 릴리스 종류, 릴리스 번호]로 읽는다. (a=0, b=1, f=2, p=3) 읽을 수 없으면 null.
        /// </summary>
        private static int[] Parse(string version)
        {
            Match match = Regex.Match(version ?? string.Empty, @"^(\d+)\.(\d+)\.(\d+)(?:([abfp])(\d+))?");
            if (!match.Success)
                return null;

            int stage = match.Groups[4].Success ? "abfp".IndexOf(match.Groups[4].Value[0]) : 2;
            int build = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
            return new[] { int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), stage, build };
        }

        /// <summary>
        /// 버전 숫자 배열을 앞에서부터 비교한다. a가 더 옛 버전이면 음수.
        /// </summary>
        private static int Compare(int[] a, int[] b)
        {
            for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                if (a[i] != b[i])
                    return a[i].CompareTo(b[i]);
            }
            return 0;
        }
    }
}
