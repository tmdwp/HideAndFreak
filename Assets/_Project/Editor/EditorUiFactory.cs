using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 에디터에서 uGUI 요소를 코드로 만드는 도우미. 만든 UI는 씬에 저장되어 인스펙터에서 그대로 편집할 수 있다.
    /// </summary>
    public static class EditorUiFactory
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.11f, 0.92f);
        public static readonly Color ButtonColor = new Color(0.22f, 0.25f, 0.30f, 1f);
        public static readonly Color AccentColor = new Color(0.85f, 0.55f, 0.20f, 1f);
        public static readonly Color TextColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        public static readonly Color NoticeColor = new Color(1f, 0.85f, 0.4f, 1f);

        private static Font defaultFont;

        /// <summary>기본 UI 폰트. (실행 시 OsFontApplier가 한글 OS 폰트로 교체)</summary>
        private static Font DefaultFont => defaultFont != null ? defaultFont : defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>
        /// 1920x1080 기준으로 크기가 맞춰지는 화면 캔버스를 만든다.
        /// </summary>
        public static Canvas CreateCanvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>
        /// Input System용 입력 모듈을 가진 EventSystem을 만든다.
        /// </summary>
        public static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>
        /// 앵커 범위를 가진 배경 패널을 만든다.
        /// </summary>
        public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        /// <summary>
        /// 자식을 세로로 쌓는 레이아웃을 붙인다.
        /// </summary>
        public static VerticalLayoutGroup AddVerticalLayout(RectTransform rect, float spacing, int padding)
        {
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>
        /// 자식을 가로로 나열하는 줄을 만든다.
        /// </summary>
        public static RectTransform CreateRow(Transform parent, string name, float height, float spacing)
        {
            RectTransform rect = CreateRect(parent, name);
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            SetHeight(rect, height);
            return rect;
        }

        /// <summary>
        /// 텍스트를 만든다.
        /// </summary>
        public static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// 라벨이 있는 버튼을 만든다.
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string label, Color color, out Text labelText)
        {
            RectTransform rect = CreateRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;

            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            labelText = CreateText(rect, "Label", label, 24, TextAnchor.MiddleCenter, TextColor);
            Stretch(labelText.rectTransform);
            return button;
        }

        /// <summary>
        /// 라벨 참조가 필요 없는 버튼을 만든다.
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string label, Color color)
        {
            return CreateButton(parent, name, label, color, out _);
        }

        /// <summary>
        /// 안내 문구가 있는 한 줄 입력칸을 만든다.
        /// </summary>
        public static InputField CreateInputField(Transform parent, string name, string placeholder)
        {
            RectTransform rect = CreateRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.15f, 0.18f, 1f);

            Text placeholderText = CreateText(rect, "Placeholder", placeholder, 22, TextAnchor.MiddleLeft, new Color(0.6f, 0.6f, 0.6f, 1f));
            placeholderText.fontStyle = FontStyle.Italic;
            StretchWithPadding(placeholderText.rectTransform, 14f);

            Text inputText = CreateText(rect, "Text", string.Empty, 22, TextAnchor.MiddleLeft, TextColor);
            inputText.supportRichText = false;
            StretchWithPadding(inputText.rectTransform, 14f);

            InputField input = rect.gameObject.AddComponent<InputField>();
            input.textComponent = inputText;
            input.placeholder = placeholderText;
            input.targetGraphic = image;
            return input;
        }

        /// <summary>
        /// 레이아웃 안에서의 높이를 고정한다.
        /// </summary>
        public static void SetHeight(Component target, float height)
        {
            LayoutElement element = GetLayoutElement(target);
            element.minHeight = height;
            element.preferredHeight = height;
        }

        /// <summary>
        /// 레이아웃 안에서의 너비를 고정한다.
        /// </summary>
        public static void SetWidth(Component target, float width)
        {
            LayoutElement element = GetLayoutElement(target);
            element.minWidth = width;
            element.preferredWidth = width;
        }

        /// <summary>
        /// 레이아웃 안에서 남는 너비를 차지하게 한다.
        /// </summary>
        public static void SetFlexibleWidth(Component target)
        {
            GetLayoutElement(target).flexibleWidth = 1f;
        }

        /// <summary>
        /// 부모 크기에 꽉 차게 한다.
        /// </summary>
        public static void Stretch(RectTransform rect)
        {
            StretchWithPadding(rect, 0f);
        }

        /// <summary>
        /// 부모 크기에서 좌우 여백을 두고 꽉 차게 한다.
        /// </summary>
        public static void StretchWithPadding(RectTransform rect, float horizontalPadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalPadding, 0f);
            rect.offsetMax = new Vector2(-horizontalPadding, 0f);
        }

        /// <summary>
        /// RectTransform을 가진 빈 오브젝트를 만든다.
        /// </summary>
        public static RectTransform CreateRect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        /// <summary>
        /// LayoutElement를 가져오거나 추가한다.
        /// </summary>
        private static LayoutElement GetLayoutElement(Component target)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            return element != null ? element : target.gameObject.AddComponent<LayoutElement>();
        }
    }
}
