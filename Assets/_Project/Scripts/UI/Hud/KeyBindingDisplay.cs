using System.Text;
using GuardVsMannequin.Localization;
using UnityEngine.InputSystem;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 입력 액션에 지금 묶인 키를 화면 표시용 이름으로 바꾼다. (키 설정을 바꾸면 HUD 안내도 따라 바뀜)
    /// 마우스 버튼처럼 Input System 기본 이름이 영어 약어인 키는 strings.csv의 "input.장치.컨트롤" 문구가 있으면 그것을 쓴다.
    /// </summary>
    public static class KeyBindingDisplay
    {
        // 복합 입력 부분의 표시 순서. (WASD처럼 보이도록 위-왼쪽-아래-오른쪽)
        private static readonly string[] PartOrder = { "up", "left", "down", "right", "negative", "positive" };

        /// <summary>
        /// 액션의 키 이름. 복합 입력(WASD 등)은 부분 키를 이어 붙인다. 액션이 없으면 "-".
        /// </summary>
        public static string Get(InputActionAsset actions, string actionName)
        {
            InputAction action = actions != null ? actions.FindAction(actionName) : null;
            if (action == null)
                return "-";

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite)
                    return GetCompositeDisplay(action, i);
                if (!binding.isPartOfComposite)
                    return GetSingleDisplay(action, i);
            }
            return "-";
        }

        /// <summary>
        /// 복합 입력의 부분 키를 정해진 순서로 이어 붙인다. 모두 한 글자면 붙여 쓰고(WASD), 아니면 "/"로 나눈다.
        /// </summary>
        private static string GetCompositeDisplay(InputAction action, int compositeIndex)
        {
            var builder = new StringBuilder();
            bool allSingleCharacter = true;
            var parts = new string[PartOrder.Length];

            for (int i = compositeIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
            {
                int order = System.Array.IndexOf(PartOrder, action.bindings[i].name);
                if (order < 0)
                    continue;
                parts[order] = GetSingleDisplay(action, i);
                allSingleCharacter &= parts[order].Length == 1;
            }

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                if (builder.Length > 0 && !allSingleCharacter)
                    builder.Append('/');
                builder.Append(part);
            }
            return builder.Length > 0 ? builder.ToString() : "-";
        }

        /// <summary>
        /// 바인딩 하나의 표시 이름. 번역 문구가 있으면 그것을 쓴다.
        /// </summary>
        private static string GetSingleDisplay(InputAction action, int bindingIndex)
        {
            string key = GetLocalizationKey(action.bindings[bindingIndex].effectivePath);
            return key != null && Loc.HasKey(key) ? Loc.Get(key) : action.GetBindingDisplayString(bindingIndex);
        }

        /// <summary>
        /// "&lt;Mouse&gt;/leftButton" → "input.mouse.leftButton". 형식이 다르면 null.
        /// </summary>
        private static string GetLocalizationKey(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '<')
                return null;

            int deviceEnd = path.IndexOf('>');
            int slash = path.IndexOf('/', deviceEnd + 1);
            if (deviceEnd < 0 || slash < 0)
                return null;

            string device = path.Substring(1, deviceEnd - 1).ToLowerInvariant();
            return $"input.{device}.{path.Substring(slash + 1)}";
        }
    }
}
