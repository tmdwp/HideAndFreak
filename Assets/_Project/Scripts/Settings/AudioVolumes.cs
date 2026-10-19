namespace GuardVsMannequin.Settings
{
    /// <summary>
    /// 소리 종류별 음량 배율. 전체 음량은 AudioListener에 적용되므로 여기서는 종류별 배율만 제공한다.
    /// (사운드 에셋 도입 시 AudioMixer 그룹으로 교체할 수 있도록 음량 조회를 한곳에 모음)
    /// </summary>
    public static class AudioVolumes
    {
        /// <summary>효과음(소음, 총소리, 전원 차단 등) 음량 배율.</summary>
        public static float Sfx => OptionsStore.Current.sfxVolume;

        /// <summary>배경 음악 음량 배율.</summary>
        public static float Bgm => OptionsStore.Current.bgmVolume;
    }
}
