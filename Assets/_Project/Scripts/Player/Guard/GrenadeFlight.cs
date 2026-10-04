using System;
using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 날아가는 수류탄 연출 하나. 스스로 궤적을 따라 움직이고, 도착하면 폭발 콜백을 부른 뒤 사라진다.
    /// 던진 가드가 중간에 사라져도(사망) 연출이 끝까지 재생되도록 별도 오브젝트로 둔다.
    /// </summary>
    public class GrenadeFlight : MonoBehaviour
    {
        private Vector3 start;
        private Vector3 velocity;
        private float flightTime;
        private float elapsed;
        private Action<Vector3> exploded;

        /// <summary>
        /// 궤적과 폭발 콜백을 지정한다.
        /// </summary>
        public void Launch(Vector3 startPosition, Vector3 initialVelocity, float duration, Action<Vector3> onExploded)
        {
            start = startPosition;
            velocity = initialVelocity;
            flightTime = Mathf.Max(0f, duration);
            exploded = onExploded;
            transform.position = start;
        }

        /// <summary>
        /// 궤적을 따라 움직이고, 비행 시간이 끝나면 폭발한다.
        /// </summary>
        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Min(elapsed, flightTime);
            transform.position = GrenadeTrajectory.GetPosition(start, velocity, t);
            transform.Rotate(Vector3.right, 720f * Time.deltaTime, Space.Self);

            if (elapsed < flightTime)
                return;

            exploded?.Invoke(transform.position);
            Destroy(gameObject);
        }
    }
}
