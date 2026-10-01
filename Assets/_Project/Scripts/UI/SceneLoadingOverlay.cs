using FishNet;
using FishNet.Managing.Scened;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Room;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 룸 ↔ 게임 씬 전환 중 화면 전체를 덮는 로딩 화면.
    /// 게스트는 호스트가 맵을 다 불러온 뒤에야 로딩 지시를 받으므로, 시작 버튼이 눌린 순간부터 이 화면을 보여 준다.
    /// 표시 여부는 <see cref="RoomState.IsSceneTransitionPending"/>만 보고 정하며, 씬과 무관하게 유지되도록 스스로 만들어진다.
    /// 표시 중에는 <see cref="LoadingPerformanceBoost"/>로 로딩을 가속한다.
    /// </summary>
    public class SceneLoadingOverlay : MonoBehaviour
    {
        private const int SortingOrder = 30000;

        private GameObject root;
        private Text titleText;
        private Text detailText;
        private SceneManager subscribedSceneManager;
        private bool isVisible;
        private bool loadStarted;
        private float loadPercent;
        private float shownTime;

        /// <summary>
        /// 게임 시작 시 로딩 화면 오브젝트를 하나 만든다. (씬이 바뀌어도 유지)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInstance()
        {
            var instance = new GameObject(nameof(SceneLoadingOverlay));
            DontDestroyOnLoad(instance);
            instance.AddComponent<SceneLoadingOverlay>();
        }

        /// <summary>
        /// 화면 UI를 만들고 숨겨 둔다.
        /// </summary>
        private void Awake()
        {
            BuildUi();
            root.SetActive(false);
        }

        /// <summary>
        /// 구독과 로딩 가속을 정리한다.
        /// </summary>
        private void OnDestroy()
        {
            Unsubscribe();
            LoadingPerformanceBoost.End();
        }

        /// <summary>
        /// 씬 전환 상태에 맞춰 화면을 켜고 끄고, 표시 중이면 문구를 갱신한다.
        /// </summary>
        private void Update()
        {
            RoomState room = RoomState.Instance;
            bool pending = room != null && room.IsSceneTransitionPending;
            if (pending != isVisible)
                SetVisible(pending);

            if (isVisible)
                RefreshTexts(room);
        }

        /// <summary>
        /// 화면을 켜고 끈다. 켤 때 진행 상태를 초기화하고 FishNet 로딩 이벤트를 구독한다.
        /// </summary>
        private void SetVisible(bool visible)
        {
            isVisible = visible;
            root.SetActive(visible);

            if (visible)
            {
                loadStarted = false;
                loadPercent = 0f;
                shownTime = Time.unscaledTime;
                Subscribe();
                LoadingPerformanceBoost.Begin();
            }
            else
            {
                Unsubscribe();
                LoadingPerformanceBoost.End();
            }
        }

        /// <summary>
        /// 제목(어디로 가는지)과 상세(호스트 대기 또는 진행률·경과 시간)를 갱신한다.
        /// </summary>
        private void RefreshTexts(RoomState room)
        {
            titleText.text = Loc.Get(room.IsInGame ? "loading.toGame" : "loading.toRoom");

            int elapsed = Mathf.FloorToInt(Time.unscaledTime - shownTime);
            bool waitingForHost = !loadStarted && !InstanceFinder.IsServerStarted;
            detailText.text = waitingForHost
                ? Loc.Format("loading.waitingHost", elapsed)
                : Loc.Format("loading.progress", Mathf.RoundToInt(loadPercent * 100f), elapsed);
        }

        /// <summary>
        /// 현재 NetworkManager의 씬 로딩 이벤트를 구독한다.
        /// </summary>
        private void Subscribe()
        {
            Unsubscribe();
            subscribedSceneManager = InstanceFinder.SceneManager;
            if (subscribedSceneManager == null)
                return;
            subscribedSceneManager.OnLoadStart += OnLoadStart;
            subscribedSceneManager.OnLoadPercentChange += OnLoadPercentChange;
        }

        /// <summary>
        /// 씬 로딩 이벤트 구독을 해제한다.
        /// </summary>
        private void Unsubscribe()
        {
            if (subscribedSceneManager == null)
                return;
            subscribedSceneManager.OnLoadStart -= OnLoadStart;
            subscribedSceneManager.OnLoadPercentChange -= OnLoadPercentChange;
            subscribedSceneManager = null;
        }

        /// <summary>
        /// 이 기기에서 씬 로딩이 시작됐다. (게스트는 호스트 대기가 끝난 시점)
        /// </summary>
        private void OnLoadStart(SceneLoadStartEventArgs args)
        {
            loadStarted = true;
        }

        /// <summary>
        /// 로딩 진행률(0~1)을 기록한다.
        /// </summary>
        private void OnLoadPercentChange(SceneLoadPercentEventArgs args)
        {
            loadStarted = true;
            loadPercent = Mathf.Max(loadPercent, args.Percent);
        }

        /// <summary>
        /// 검은 배경과 문구 두 줄로 된 화면을 코드로 만든다. (씬이나 프리팹에 의존하지 않기 위함)
        /// </summary>
        private void BuildUi()
        {
            root = new GameObject("Canvas", typeof(RectTransform));
            root.transform.SetParent(transform, false);

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

            Font font = OsFontApplier.GetOsFont();
            titleText = CreateText("Title", font, 56, new Vector2(0f, 0.5f), new Vector2(1f, 0.6f));
            detailText = CreateText("Detail", font, 30, new Vector2(0f, 0.42f), new Vector2(1f, 0.5f));
            detailText.color = new Color(1f, 1f, 1f, 0.7f);
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
