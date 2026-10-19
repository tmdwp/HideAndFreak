using FishNet;
using FishNet.Managing.Scened;
using GuardVsMannequin.Localization;
using GuardVsMannequin.Room;
using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 룸 ↔ 게임 씬 전환 중 화면 전체를 덮는 로딩 화면.
    /// 게스트는 호스트가 맵을 다 불러온 뒤에야 로딩 지시를 받으므로, 시작 버튼이 눌린 순간부터 이 화면을 보여 준다.
    /// 표시 여부는 <see cref="RoomState.IsSceneTransitionPending"/>만 보고 정하며, 씬과 무관하게 유지되도록 스스로 만들어진다.
    /// 표시 중에는 <see cref="LoadingPerformanceBoost"/>로 로딩을 가속한다. 화면 모양은 <see cref="SceneLoadingOverlayView"/>가 맡는다.
    /// </summary>
    public class SceneLoadingOverlay : MonoBehaviour
    {
        private SceneLoadingOverlayView view;
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
        /// 화면을 만든다. (처음에는 숨김)
        /// </summary>
        private void Awake()
        {
            view = new SceneLoadingOverlayView(transform);
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
            view.SetVisible(visible);

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
            string title = Loc.Get(room.IsInGame ? "loading.toGame" : "loading.toRoom");

            int elapsed = Mathf.FloorToInt(Time.unscaledTime - shownTime);
            bool waitingForHost = !loadStarted && !InstanceFinder.IsServerStarted;
            string detail = waitingForHost
                ? Loc.Format("loading.waitingHost", elapsed)
                : Loc.Format("loading.progress", Mathf.RoundToInt(loadPercent * 100f), elapsed);

            view.SetTexts(title, detail);
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
    }
}
