namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 설정 화면의 설정 묶음 하나(화면, 언어, 사운드, 조작 등).
    /// 설정 화면(<see cref="SettingsPanelView"/>)이 하위에서 자동으로 찾아 호출하므로,
    /// 새 설정을 추가할 때는 이 인터페이스를 구현한 컴포넌트를 설정 화면 안에 두기만 하면 된다.
    /// </summary>
    public interface ISettingsSection
    {
        /// <summary>
        /// 처음 열릴 때 한 번 UI 이벤트를 연결한다.
        /// </summary>
        void Initialize();

        /// <summary>
        /// 저장된 값을 화면에 표시한다. (열 때, 언어가 바뀔 때)
        /// </summary>
        void Refresh();

        /// <summary>
        /// "모두 기본값으로"를 눌렀을 때 이 묶음의 값을 기본값으로 되돌린다. (되돌리지 않는 묶음은 비워 둔다)
        /// </summary>
        void ResetToDefaults();
    }
}
