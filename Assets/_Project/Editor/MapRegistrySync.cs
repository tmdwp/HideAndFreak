using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Room;
using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 맵 목록(<see cref="MapCatalog.AllMaps"/>)에 맞춰 게임이 맵을 고르는 데 필요한 곳을 모두 갱신한다.
    /// 1. 빌드 설정: 로비 → 룸 → 존재하는 맵 씬
    /// 2. RoomState 프리팹의 맵 씬 목록 (맵 번호 = 순서)
    /// 3. strings.csv: 없는 map.&lt;번호&gt; 줄을 씬 이름으로 추가 (이미 있는 이름은 유지, 나중에 직접 번역)
    /// 4. game_settings.csv: Map 항목의 최댓값 = 맵 수 - 1
    /// </summary>
    public static class MapRegistrySync
    {
        private const string StringsPath = "Assets/_Project/Resources/Localization/strings.csv";
        private const string GameSettingsPath = "Assets/_Project/Resources/Data/game_settings.csv";

        /// <summary>
        /// 맵 목록을 게임 설정 전체에 반영한다.
        /// </summary>
        [MenuItem("GuardVsMannequin/맵 제작/맵 목록 갱신 (빌드·룸·문구)")]
        public static void Sync()
        {
            MapCatalog.Entry[] maps = MapCatalog.AllMaps;
            TestEnvironmentSetupTool.RegisterBuildScenes();
            UpdateRoomStateSceneNames(maps);
            int added = AddMissingMapNames(maps);
            UpdateMapRange(maps.Length);

            AssetDatabase.SaveAssets();
            Loc.Reload();
            GameSettingDefinitions.ClearCache();
            Debug.Log($"[MapRegistrySync] 맵 {maps.Length}개 등록 ({string.Join(", ", maps.Select((m, i) => $"{i}:{m.sceneName}"))}). " +
                      $"맵 이름 문구 {added}개 추가.");
        }

        /// <summary>
        /// 룸 상태 프리팹의 맵 씬 목록을 맵 번호 순서로 갱신한다.
        /// </summary>
        private static void UpdateRoomStateSceneNames(MapCatalog.Entry[] maps)
        {
            if (!File.Exists(TestEnvironmentSetupTool.RoomStatePrefabPath))
            {
                Debug.LogWarning("[MapRegistrySync] RoomState 프리팹이 없습니다. '테스트 환경 생성'을 먼저 실행하세요.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(TestEnvironmentSetupTool.RoomStatePrefabPath);
            RoomState roomState = contents.GetComponent<RoomState>();
            if (roomState != null)
            {
                var serialized = new SerializedObject(roomState);
                SerializedProperty names = serialized.FindProperty("gameSceneNames");
                names.arraySize = maps.Length;
                for (int i = 0; i < maps.Length; i++)
                    names.GetArrayElementAtIndex(i).stringValue = maps[i].sceneName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, TestEnvironmentSetupTool.RoomStatePrefabPath);
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        /// <summary>
        /// strings.csv에 없는 맵 이름(map.번호) 줄을 마지막 map. 줄 뒤에 추가한다. 추가한 개수를 반환한다.
        /// 새 이름은 모든 언어 칸에 씬 이름을 넣으므로, 나중에 파일에서 번역하면 된다.
        /// </summary>
        private static int AddMissingMapNames(MapCatalog.Entry[] maps)
        {
            List<string> lines = ReadLines(StringsPath, out bool hasBom, out string newLine);
            string header = lines.FirstOrDefault(line => line.StartsWith("key,")) ?? "key,ko,en";
            int languageCount = Mathf.Max(1, header.Split(',').Length - 1);

            int lastMapLine = lines.FindLastIndex(line => line.StartsWith("map."));
            int insertAt = lastMapLine >= 0 ? lastMapLine + 1 : lines.Count;
            int added = 0;
            for (int i = 0; i < maps.Length; i++)
            {
                string key = $"map.{i}";
                if (lines.Any(line => line.StartsWith(key + ",")))
                    continue;

                string name = maps[i].displayName.Replace(",", " ");
                lines.Insert(insertAt++, key + string.Concat(Enumerable.Repeat("," + name, languageCount)));
                added++;
            }

            if (added > 0)
                WriteLines(StringsPath, lines, hasBom, newLine);
            return added;
        }

        /// <summary>
        /// game_settings.csv의 Map 줄 최댓값을 맵 수에 맞춘다.
        /// </summary>
        private static void UpdateMapRange(int mapCount)
        {
            List<string> lines = ReadLines(GameSettingsPath, out bool hasBom, out string newLine);
            int index = lines.FindIndex(line => line.StartsWith("Map,"));
            string updated = $"Map,1,0,{Mathf.Max(0, mapCount - 1)},";
            if (index < 0 || lines[index] == updated)
                return;

            lines[index] = updated;
            WriteLines(GameSettingsPath, lines, hasBom, newLine);
        }

        /// <summary>
        /// 텍스트 파일을 줄 단위로 읽는다. BOM과 줄바꿈 형식을 기억해 같은 형식으로 다시 쓰기 위함이다.
        /// </summary>
        private static List<string> ReadLines(string path, out bool hasBom, out string newLine)
        {
            byte[] bytes = File.ReadAllBytes(path);
            hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            string text = new UTF8Encoding(false).GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));
            newLine = text.Contains("\r\n") ? "\r\n" : "\n";

            List<string> lines = text.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
            if (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
                lines.RemoveAt(lines.Count - 1);
            return lines;
        }

        /// <summary>
        /// 줄 목록을 원래 BOM·줄바꿈 형식으로 저장하고 다시 가져온다.
        /// </summary>
        private static void WriteLines(string path, List<string> lines, bool hasBom, string newLine)
        {
            File.WriteAllText(path, string.Join(newLine, lines) + newLine, new UTF8Encoding(hasBom));
            AssetDatabase.ImportAsset(path);
        }
    }
}
