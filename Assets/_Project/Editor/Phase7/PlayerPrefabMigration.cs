#pragma warning disable CS0618 // 지울 대상인 탐지기 컴포넌트를 찾기 위해 [Obsolete] 형식을 쓴다.
using FishNet.Managing.Object;
using GuardVsMannequin.Audio;
using GuardVsMannequin.Player;
using GuardVsMannequin.Player.Guard;
using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 기존 가드·마네킹 프리팹을 Phase 7-A4(수류탄)에 맞게 고친다. 다시 만들지 않으므로 직접 고친 내용(외형, 값)은 유지된다.
    /// - 가드: 탐지기(DetectorTool, DetectorPulseView와 그 오브젝트) 삭제 → 수류탄(GrenadeTool, GrenadeView) 추가
    /// - 마네킹: 수류탄 효과(StatusEffects, RevealOutlineView) 추가
    /// - 소음 목록: 수류탄 폭발음 항목 추가, 탐지기 항목 삭제
    /// - FishNet 프리팹 해시 갱신 (네트워크 컴포넌트 구성이 바뀌었으므로)
    /// </summary>
    public static class PlayerPrefabMigration
    {
        private const string DetectorPulseObjectName = "DetectorPulse";

        /// <summary>
        /// 프리팹을 고친다.
        /// </summary>
        /// <returns>바꾼 프리팹 수. 프리팹이 없으면 -1.</returns>
        public static int Run()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TestEnvironmentSetupTool.GuardPrefabPath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(TestEnvironmentSetupTool.MannequinPrefabPath) == null)
                return -1;

            int changed = 0;
            if (EditPrefab(TestEnvironmentSetupTool.GuardPrefabPath, MigrateGuard))
                changed++;
            if (EditPrefab(TestEnvironmentSetupTool.MannequinPrefabPath, MigrateMannequin))
                changed++;

            UpdateNoiseLibrary();

            var collection = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(TestEnvironmentSetupTool.DefaultPrefabObjectsPath);
            if (changed > 0 && collection != null)
                TestEnvironmentSetupTool.RefreshAssetPathHashes(collection);
            return changed;
        }

        /// <summary>
        /// 가드: 탐지기를 지우고 수류탄을 붙인다. 이미 수류탄이 있으면 바꾸지 않는다.
        /// </summary>
        private static bool MigrateGuard(GameObject root)
        {
            bool hadDetector = false;
            foreach (DetectorTool detector in root.GetComponentsInChildren<DetectorTool>(true))
            {
                Object.DestroyImmediate(detector, true);
                hadDetector = true;
            }
            foreach (DetectorPulseView pulse in root.GetComponentsInChildren<DetectorPulseView>(true))
                Object.DestroyImmediate(pulse.gameObject, true);
            Transform leftover = root.transform.Find(DetectorPulseObjectName);
            if (leftover != null)
                Object.DestroyImmediate(leftover.gameObject, true);

            bool hasGrenade = root.GetComponent<GrenadeTool>() != null;
            if (!hasGrenade)
                TestEnvironmentSetupTool.AddGrenade(root);
            return hadDetector || !hasGrenade;
        }

        /// <summary>
        /// 마네킹: 수류탄 효과 컴포넌트를 붙인다.
        /// </summary>
        private static bool MigrateMannequin(GameObject root)
        {
            bool missing = root.GetComponent<StatusEffects>() == null || root.GetComponent<RevealOutlineView>() == null;
            if (missing)
                TestEnvironmentSetupTool.AddGrenadeTargetComponents(root);
            return missing;
        }

        /// <summary>
        /// 소음 목록에 수류탄 폭발음을 넣고 탐지기 항목을 지운다. (지정해 둔 다른 소리는 유지)
        /// </summary>
        private static void UpdateNoiseLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<NoiseLibrary>(TestEnvironmentSetupTool.NoiseLibraryPath);
            if (library != null && library.AddMissingDefaults())
                EditorUtility.SetDirty(library);
        }

        /// <summary>
        /// 프리팹을 열어 고치고, 바뀌었으면 저장한다.
        /// </summary>
        private static bool EditPrefab(string path, System.Func<GameObject, bool> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = edit(root);
                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
