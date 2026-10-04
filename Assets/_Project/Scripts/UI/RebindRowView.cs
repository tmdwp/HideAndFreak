using System;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 키 설정 한 줄(이름, 현재 키, 변경, 초기화).
    /// 변경을 누르면 다음에 누른 키로 바꾼다. ESC로 취소한다.
    /// </summary>
    public class RebindRowView : MonoBehaviour
    {
        /// <summary>키 입력을 기다리는 중인지 여부. (그동안 ESC 메뉴가 열리지 않도록)</summary>
        public static bool IsAnyRebinding { get; private set; }

        [SerializeField] private string actionName;

        [Tooltip("복합 입력(WASD, Q/E)의 부분 이름. 단일 바인딩이면 비워 둔다.")]
        [SerializeField] private string partName;

        [SerializeField] private Text labelText;
        [SerializeField] private Text keyText;
        [SerializeField] private Button rebindButton;
        [SerializeField] private Button resetButton;

        /// <summary>이 줄이 바꾸는 액션 이름.</summary>
        public string ActionName => actionName;

        /// <summary>복합 입력의 부분 이름. 단일 바인딩이면 빈 문자열.</summary>
        public string PartName => partName;

        private InputAction action;
        private string labelKey;
        private int bindingIndex = -1;
        private Action onChanged;
        private InputActionRebindingExtensions.RebindingOperation operation;

        /// <summary>
        /// 편집용 입력 에셋에서 대상 바인딩을 찾고 버튼을 연결한다.
        /// </summary>
        /// <param name="editingActions">설정 화면이 편집하는 입력 에셋 복제본.</param>
        /// <param name="labelKey">표시 이름 문구의 key.</param>
        /// <param name="changed">바인딩이 바뀌었을 때 호출할 함수. (저장)</param>
        public void Bind(InputActionAsset editingActions, string labelKey, Action changed)
        {
            this.labelKey = labelKey;
            onChanged = changed;
            action = editingActions.FindAction(actionName, true);
            bindingIndex = KeyBindingCatalog.FindBindingIndex(action, string.IsNullOrEmpty(partName) ? null : partName);

            rebindButton.onClick.AddListener(StartRebind);
            resetButton.onClick.AddListener(ResetBinding);
            Refresh();
        }

        /// <summary>
        /// 화면이 닫히면 진행 중인 키 입력 대기를 취소한다.
        /// </summary>
        private void OnDisable()
        {
            operation?.Cancel();
        }

        /// <summary>
        /// 항목 이름(현재 언어)과 현재 키 이름을 표시한다.
        /// </summary>
        public void Refresh()
        {
            labelText.text = Loc.Get(labelKey);
            keyText.text = bindingIndex >= 0 ? action.GetBindingDisplayString(bindingIndex) : "-";
        }

        /// <summary>
        /// 다음에 누르는 키(마우스 버튼 포함)로 바인딩을 바꾸기 시작한다.
        /// </summary>
        private void StartRebind()
        {
            if (bindingIndex < 0 || IsAnyRebinding)
                return;

            IsAnyRebinding = true;
            keyText.text = Loc.Get("options.pressKey");
            action.Disable();

            operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(_ => FinishRebind(changed: true))
                .OnCancel(_ => FinishRebind(changed: false))
                .Start();
        }

        /// <summary>
        /// 키 입력 대기를 끝내고, 바뀌었으면 저장한다.
        /// </summary>
        private void FinishRebind(bool changed)
        {
            operation?.Dispose();
            operation = null;
            IsAnyRebinding = false;

            Refresh();
            if (changed)
                onChanged?.Invoke();
        }

        /// <summary>
        /// 이 키를 기본값으로 되돌린다.
        /// </summary>
        private void ResetBinding()
        {
            if (bindingIndex < 0 || IsAnyRebinding)
                return;

            action.RemoveBindingOverride(bindingIndex);
            Refresh();
            onChanged?.Invoke();
        }
    }
}
