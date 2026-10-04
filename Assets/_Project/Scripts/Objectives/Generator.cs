using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GuardVsMannequin.Audio;
using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Objectives
{
    /// <summary>
    /// 마네킹이 좌클릭을 유지해 파괴하는 전원 장치.
    /// 참여 인원 n명일 때 총 속도 = 1.4^(n-1), 참여자가 0명이 되면 진행도가 초기화된다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Generator : NetworkBehaviour, IInteractable
    {
        /// <summary>상호작용 도중 조금 움직여도 끊기지 않도록 허용하는 추가 거리.</summary>
        private const float RangeTolerance = 0.5f;

        private readonly SyncVar<float> progress = new();
        private readonly SyncVar<bool> isDestroyed = new();

        private readonly Dictionary<int, IInteractor> participants = new();
        private readonly List<int> removeBuffer = new();
        private Collider interactCollider;
        private NoiseEmitter noiseEmitter;

        /// <summary>서버에서 파괴가 완료되었을 때.</summary>
        public event Action<Generator> ServerDestroyed;

        /// <summary>파괴 진행도(0~1).</summary>
        public float Progress => progress.Value;

        /// <summary>파괴되었는지 여부. 파괴된 장치는 다시 상호작용할 수 없다.</summary>
        public bool IsDestroyed => isDestroyed.Value;

        /// <summary>현재 참여 인원. (서버 전용)</summary>
        public int ParticipantCount => participants.Count;

        public float InteractRange => GameConstants.InteractRange;

        /// <summary>
        /// 거리 판정에 사용할 충돌체와 소음 구현을 캐시한다.
        /// </summary>
        private void Awake()
        {
            interactCollider = GetComponent<Collider>();
            noiseEmitter = GetComponent<NoiseEmitter>();
        }

        /// <summary>
        /// 조건을 확인하고 참여자로 등록한다.
        /// </summary>
        [Server]
        public bool TryBeginInteract(IInteractor interactor)
        {
            if (isDestroyed.Value || !interactor.CanInteract || !IsInRange(interactor, 0f))
                return false;

            participants[interactor.ClientId] = interactor;
            return true;
        }

        /// <summary>
        /// 참여자 목록에서 제거한다.
        /// </summary>
        [Server]
        public void EndInteract(IInteractor interactor)
        {
            participants.Remove(interactor.ClientId);
        }

        /// <summary>
        /// 이 주체가 아직 참여자로 남아 있는지 확인한다.
        /// </summary>
        [Server]
        public bool IsInteracting(IInteractor interactor)
        {
            return participants.TryGetValue(interactor.ClientId, out IInteractor current) && ReferenceEquals(current, interactor);
        }

        /// <summary>
        /// 서버에서 참여자를 검증하고 진행도를 갱신한다.
        /// </summary>
        private void Update()
        {
            if (!this.IsServerReady() || isDestroyed.Value)
                return;

            RemoveInvalidParticipants();

            int count = participants.Count;
            if (count == 0)
            {
                // 모두 이탈하면 진행도 초기화
                if (progress.Value != 0f)
                    progress.Value = 0f;
                return;
            }

            float destroyTime = GameServices.CurrentSettingsOrDefault.generatorDestroyTime;
            float speed = CalculateTotalSpeed(count);
            float next = progress.Value + speed / destroyTime * Time.deltaTime;

            if (next >= 1f)
                CompleteDestruction();
            else
                progress.Value = next;
        }

        /// <summary>
        /// 참여 인원에 따른 총 파괴 속도(1인 = 1.0)를 계산한다.
        /// </summary>
        public static float CalculateTotalSpeed(int participantCount)
        {
            return participantCount <= 0 ? 0f : Mathf.Pow(GameConstants.CoopDestroySpeedBase, participantCount - 1);
        }

        /// <summary>
        /// 제거되었거나, 상호작용할 수 없게 되었거나, 멀어진 참여자를 뺀다.
        /// </summary>
        private void RemoveInvalidParticipants()
        {
            removeBuffer.Clear();
            foreach (KeyValuePair<int, IInteractor> pair in participants)
            {
                IInteractor interactor = pair.Value;
                bool destroyedObject = interactor is UnityEngine.Object unityObject && unityObject == null;
                if (destroyedObject || !interactor.CanInteract || !IsInRange(interactor, RangeTolerance))
                    removeBuffer.Add(pair.Key);
            }

            foreach (int clientId in removeBuffer)
                participants.Remove(clientId);
        }

        /// <summary>
        /// 참여자가 상호작용 거리 안에 있는지 확인한다. (충돌체 표면 기준)
        /// </summary>
        private bool IsInRange(IInteractor interactor, float tolerance)
        {
            Vector3 closest = interactCollider.ClosestPoint(interactor.Position);
            float range = InteractRange + tolerance;
            return (closest - interactor.Position).sqrMagnitude <= range * range;
        }

        /// <summary>
        /// 파괴를 완료하고, 파괴 소음을 낸 뒤 이벤트를 발생시킨다.
        /// </summary>
        private void CompleteDestruction()
        {
            progress.Value = 1f;
            isDestroyed.Value = true;
            participants.Clear();
            if (noiseEmitter != null)
                noiseEmitter.Emit(NoiseType.GeneratorDestroyed);
            ServerDestroyed?.Invoke(this);
        }
    }
}
