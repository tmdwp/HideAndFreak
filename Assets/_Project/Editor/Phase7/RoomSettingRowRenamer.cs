using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 룸 화면 프리팹(RoomCanvas)의 설정 줄 오브젝트 이름을 바뀐 설정 이름에 맞춘다. (Phase 7-A)
    /// 설정 줄은 번호(GameSettingId)로 연결되어 있어 동작에는 영향이 없고, 계층 창에서 알아보기 쉽게 하려는 정리다.
    /// </summary>
    public static class RoomSettingRowRenamer
    {
        private static readonly (string from, string to)[] Renames =
        {
            ("Row_PreparationTime", "Row_HuntDelay"),
            ("Row_DetectorCooldown", "Row_GrenadeCooldown")
        };

        /// <summary>
        /// 이름을 바꾼다.
        /// </summary>
        /// <returns>바꾼 오브젝트 수. 프리팹이 없으면 -1.</returns>
        public static int Run()
        {
            string path = UiPrefabLibrary.RoomCanvasPath;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return -1;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int renamed = 0;
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    foreach ((string from, string to) in Renames)
                    {
                        if (child.name != from)
                            continue;
                        child.name = to;
                        renamed++;
                    }
                }

                if (renamed > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                return renamed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
