using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 씬의 조명 계산(GI)을 끈다. 이 게임은 모든 조명이 실시간(MapLight, 사냥 시간에 꺼짐)이라 구운 조명·실시간 GI를 쓰지 않는다.
    /// 켜 두면 에디터가 열린 씬 조합(맵 + Game_Systems)마다 GI를 다시 계산하려다 큰 맵에서 메모리가 부족해
    /// "Clustering job failed ... Out of memory" 로그를 끝없이 남긴다.
    /// 모든 씬이 같은 조명 설정 에셋(Baked GI·Realtime GI 끔)을 쓰게 한다. 환경광·하늘·안개는 그대로다.
    /// </summary>
    public static class SceneLightingSetup
    {
        /// <summary>공용 조명 설정 에셋 경로.</summary>
        public const string SettingsPath = TestEnvironmentSetupTool.SoFolder + "/Lighting_RealtimeOnly.lighting";

        /// <summary>
        /// 씬에 GI를 끈 조명 설정을 지정한다. (씬 저장은 호출한 쪽이 한다)
        /// </summary>
        public static void Apply(Scene scene)
        {
            Lightmapping.SetLightingSettingsForScene(scene, GetOrCreateSettings());
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>
        /// 공용 조명 설정 에셋을 가져오거나 만든다.
        /// </summary>
        private static LightingSettings GetOrCreateSettings()
        {
            LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (settings != null)
                return settings;

            settings = new LightingSettings
            {
                name = "Lighting_RealtimeOnly",
                bakedGI = false,
                realtimeGI = false
            };
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }
    }
}
