using FishNet.Managing;
using FishNet.Managing.Scened;
using UnityEngine.SceneManagement;

namespace GuardVsMannequin.Mods
{
    /// <summary>
    /// FishNet 씬 처리기 확장. 모든 PC(서버·클라이언트)가 씬을 불러오기 직전에 모드 맵의 번들을 불러오고,
    /// 씬 전환이 끝나면 쓰지 않는 모드 번들을 해제한다. 나머지 동작은 FishNet 기본 처리기와 같다.
    /// </summary>
    public class ModSceneProcessor : DefaultSceneProcessor
    {
        /// <summary>
        /// 네트워크 관리자의 씬 처리기를 이것으로 바꾼다. (이미 바꿨으면 그대로)
        /// </summary>
        public static void Install(NetworkManager networkManager)
        {
            if (networkManager == null || networkManager.GetComponent<ModSceneProcessor>() != null)
                return;

            var processor = networkManager.gameObject.AddComponent<ModSceneProcessor>();
            processor.Initialize(networkManager.SceneManager);
            networkManager.SceneManager.SetSceneProcessor(processor);
        }

        /// <summary>
        /// 모드 맵이면 번들을 먼저 불러온 뒤 씬을 불러온다.
        /// </summary>
        public override void BeginLoadAsync(string sceneName, LoadSceneParameters parameters)
        {
            ModBundleCache.EnsureForScene(sceneName);
            base.BeginLoadAsync(sceneName, parameters);
        }

        /// <summary>
        /// 씬 불러오기가 끝나면(이전 씬은 내려간 뒤) 쓰지 않는 모드 번들을 해제한다.
        /// </summary>
        public override void LoadEnd(LoadQueueData queueData)
        {
            base.LoadEnd(queueData);
            ModBundleCache.UnloadIfUnused();
        }

        /// <summary>
        /// 씬 내리기가 끝나면 쓰지 않는 모드 번들을 해제한다.
        /// </summary>
        public override void UnloadEnd(UnloadQueueData queueData)
        {
            base.UnloadEnd(queueData);
            ModBundleCache.UnloadIfUnused();
        }
    }
}
