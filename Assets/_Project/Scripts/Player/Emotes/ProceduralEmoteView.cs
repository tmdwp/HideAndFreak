using GuardVsMannequin.Core;
using UnityEngine;

namespace GuardVsMannequin.Player.Emotes
{
    /// <summary>
    /// 애니메이션 에셋 없이 본체 트랜스폼을 움직여 감정 표현을 보여주는 대체 구현.
    /// 외형 애니메이터에 감정 표현이 있으면 <see cref="AnimatorEmoteView"/>가 먼저 재생하고 이것은 쓰이지 않는다.
    /// 1: 점프, 2: 회전, 3: 좌우 흔들기, 4: 인사(앞으로 숙이기), 5: 떨기, 6: 웅크리기
    /// </summary>
    public class ProceduralEmoteView : MonoBehaviour, IEmoteView
    {
        [Tooltip("움직일 본체(시각 요소). 루트가 아닌 자식이어야 충돌체/네트워크 위치에 영향을 주지 않는다.")]
        [SerializeField] private Transform body;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 baseLocalScale;
        private int currentEmote;
        private float elapsed;

        /// <summary>
        /// 본체의 기본 자세를 저장한다.
        /// </summary>
        private void Awake()
        {
            baseLocalPosition = body.localPosition;
            baseLocalRotation = body.localRotation;
            baseLocalScale = body.localScale;
        }

        /// <summary>
        /// 감정 표현을 처음부터 재생한다. 애니메이션이 없을 때의 대체 수단이므로 항상 성공한다.
        /// </summary>
        public bool TryPlay(int emoteId)
        {
            currentEmote = emoteId;
            elapsed = 0f;
            return true;
        }

        /// <summary>
        /// 재생 중인 감정 표현의 자세를 계산하고, 끝나면 기본 자세로 돌아간다.
        /// </summary>
        private void Update()
        {
            if (currentEmote == 0)
                return;

            elapsed += Time.deltaTime;
            float t = elapsed / GameTuning.EmoteDuration;
            if (t >= 1f)
            {
                currentEmote = 0;
                ResetPose();
                return;
            }

            ApplyPose(currentEmote, t);
        }

        /// <summary>
        /// 진행률 t(0~1)에 해당하는 자세를 적용한다.
        /// </summary>
        private void ApplyPose(int emoteId, float t)
        {
            float wave = Mathf.Sin(t * Mathf.PI);
            ResetPose();

            switch (emoteId)
            {
                case 1: // 점프 두 번
                    body.localPosition = baseLocalPosition + Vector3.up * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 0.4f;
                    break;
                case 2: // 한 바퀴 회전
                    body.localRotation = baseLocalRotation * Quaternion.Euler(0f, t * 360f, 0f);
                    break;
                case 3: // 좌우 흔들기
                    body.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 20f * wave);
                    break;
                case 4: // 인사
                    body.localRotation = baseLocalRotation * Quaternion.Euler(wave * 45f, 0f, 0f);
                    break;
                case 5: // 떨기
                    body.localPosition = baseLocalPosition + new Vector3(Mathf.Sin(t * 120f), 0f, Mathf.Cos(t * 97f)) * 0.04f * wave;
                    break;
                case 6: // 웅크리기
                    body.localScale = Vector3.Scale(baseLocalScale, new Vector3(1f + wave * 0.2f, 1f - wave * 0.4f, 1f + wave * 0.2f));
                    body.localPosition = baseLocalPosition - Vector3.up * wave * 0.35f;
                    break;
            }
        }

        /// <summary>
        /// 본체를 기본 자세로 되돌린다.
        /// </summary>
        private void ResetPose()
        {
            body.localPosition = baseLocalPosition;
            body.localRotation = baseLocalRotation;
            body.localScale = baseLocalScale;
        }
    }
}
