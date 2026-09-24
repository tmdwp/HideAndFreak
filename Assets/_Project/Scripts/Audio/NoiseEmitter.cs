using FishNet.Object;
using UnityEngine;

namespace GuardVsMannequin.Audio
{
    /// <summary>
    /// 오브젝트 위치에서 3D 소음을 재생한다.
    /// 서버가 <see cref="Emit"/>으로 모든 클라이언트에 재생을 지시하고,
    /// 이미 RPC로 전달된 사건(사격 등)은 <see cref="PlayLocal"/>로 추가 전송 없이 재생한다.
    /// </summary>
    public class NoiseEmitter : NetworkBehaviour
    {
        [SerializeField] private NoiseLibrary library;

        private AudioSource audioSource;

        /// <summary>
        /// 3D 오디오 소스를 준비한다.
        /// </summary>
        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
        }

        /// <summary>
        /// 모든 클라이언트에서 소음을 재생한다. (서버 전용)
        /// </summary>
        [Server]
        public void Emit(NoiseType type)
        {
            EmitObserversRpc(type);
        }

        /// <summary>
        /// 서버 지시를 받아 소음을 재생한다.
        /// </summary>
        [ObserversRpc]
        private void EmitObserversRpc(NoiseType type)
        {
            PlayLocal(type);
        }

        /// <summary>
        /// 이 클라이언트에서만 소음을 재생한다.
        /// </summary>
        public void PlayLocal(NoiseType type)
        {
            if (library == null || audioSource == null)
                return;

            NoiseLibrary.Entry entry = library.Get(type);
            audioSource.maxDistance = entry.maxDistance;
            audioSource.PlayOneShot(library.GetClip(type), entry.volume);
        }
    }
}
