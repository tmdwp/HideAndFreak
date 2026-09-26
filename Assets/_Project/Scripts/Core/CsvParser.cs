using System.Collections.Generic;
using System.Text;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 사람이 엑셀이나 텍스트 편집기로 관리하는 CSV 데이터 파일을 읽는다.
    /// - 쉼표가 들어간 값은 큰따옴표로 감싼다. 값 안의 큰따옴표는 "" 로 쓴다.
    /// - 값 안의 줄바꿈은 \n 으로 쓴다. (읽을 때 실제 줄바꿈으로 바뀜)
    /// - #으로 시작하는 줄과 빈 줄은 무시한다. (메모용)
    /// </summary>
    public static class CsvParser
    {
        /// <summary>
        /// CSV 텍스트를 행 목록으로 나눈다. 첫 행(머리글)도 포함한다.
        /// </summary>
        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(text))
                return rows;

            // 엑셀이 UTF-8 CSV에 붙이는 BOM 제거
            if (text[0] == '﻿')
                text = text.Substring(1);

            foreach (string rawLine in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#"))
                    continue;
                rows.Add(ParseLine(line));
            }
            return rows;
        }

        /// <summary>
        /// 한 줄을 값 목록으로 나눈다. 큰따옴표로 감싼 값 안의 쉼표는 구분자로 보지 않는다.
        /// </summary>
        private static string[] ParseLine(string line)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    values.Add(Unescape(current.ToString()));
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(Unescape(current.ToString()));
            return values.ToArray();
        }

        /// <summary>
        /// 값 앞뒤 공백을 지우고 \n 을 실제 줄바꿈으로 바꾼다.
        /// </summary>
        private static string Unescape(string value)
        {
            return value.Trim().Replace("\\n", "\n");
        }
    }
}
