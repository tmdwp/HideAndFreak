using UnityEngine;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 에디터에서 편집 가능한 기본 게임 설정 프리셋.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettingsPreset", menuName = "GuardVsMannequin/Game Settings Preset")]
    public class GameSettingsPreset : ScriptableObject
    {
        [SerializeField] private GameSettings settings = GameSettings.CreateDefault();

        /// <summary>프리셋에 저장된 설정의 복사본.</summary>
        public GameSettings Settings => settings;

        /// <summary>
        /// 에셋 생성 시 기본값으로 초기화한다.
        /// </summary>
        private void Reset()
        {
            settings = GameSettings.CreateDefault();
        }
    }
}
