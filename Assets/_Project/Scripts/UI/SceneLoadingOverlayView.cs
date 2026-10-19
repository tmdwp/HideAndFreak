using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 로딩 화면의 모양. 검은 배경과 문구 두 줄(제목, 상세)을 코드로 만든다. (씬이나 프리팹에 의존하지 않기 위함)
    /// 언제 보일지, 무슨 문구를 쓸지는 <see cref="SceneLoadingOverlay"/>가 정한다.
    /// </summary>
    public sealed class SceneLoadingOverlayView
    {
        private const int SortingOrder = 30000;

        private readonly GameObject root;
        private readonly Text titleText;
        private readonly Text detailText;

        /// <summary>
        /// 부모 아래에 화면을 만들고 숨겨 둔다.
        /// </summary>
        public SceneLoadingOverlayView(Transform parent)
        {
            root = new GameObject("Canvas", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // 로딩 중 뒤에 있는 버튼이 눌리지 않게 클릭을 막는다.
            root.AddComponent<GraphicRaycaster>();

            var background = CreateChild<Image>("Background");
            Stretch(background.rectTransform, Vector2.zero, Vector2.one);
            background.color = Color.black;
            background.raycastTarget = true;

            Font font = UiFont.Default;
            titleText = CreateText("Title", font, 56, new Vector2(0f, 0.5f), new Vector2(1f, 0.6f));
            detailText = CreateText("Detail", font, 30, new Vector2(0f, 0.42f), new Vector2(1f, 0.5f));
            detailText.color = new Color(1f, 1f, 1f, 0.7f);

            root.SetActive(false);
        }

        /// <summary>
        /// 화면을 보이거나 숨긴다.
        /// </summary>
        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }

        /// <summary>
        /// 제목과 상세 문구를 바꾼다.
        /// </summary>
        public void SetTexts(string title, string detail)
        {
            titleText.text = title;
            detailText.text = detail;
        }

        /// <summary>
        /// 가운데 정렬된 문구를 화면 비율 영역에 만든다.
        /// </summary>
        private Text CreateText(string objectName, Font font, int fontSize, Vector2 anchorMin, Vector2 anchorMax)
        {
            var text = CreateChild<Text>(objectName);
            Stretch(text.rectTransform, anchorMin, anchorMax);
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 캔버스 아래에 UI 컴포넌트가 붙은 자식을 만든다.
        /// </summary>
        private T CreateChild<T>(string objectName) where T : Graphic
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return child.AddComponent<T>();
        }

        /// <summary>
        /// 앵커 영역을 꽉 채우도록 RectTransform을 맞춘다.
        /// </summary>
        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
