using UnityEngine;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 씬 로딩 중에만 비동기 로딩을 빠르게 하는 설정을 켜고, 끝나면 원래 값으로 되돌린다.
    /// - backgroundLoadingPriority High: 로딩 스레드에 시간을 더 준다. (그동안 프레임은 떨어지지만 로딩 화면이라 상관없음)
    /// - asyncUploadTimeSlice / BufferSize: 텍스처·메시를 GPU로 올리는 프레임당 시간과 버퍼를 늘린다.
    /// </summary>
    public static class LoadingPerformanceBoost
    {
        private const int BoostedUploadTimeSliceMs = 8;
        private const int BoostedUploadBufferMb = 64;

        private static bool isActive;
        private static ThreadPriority savedPriority;
        private static int savedTimeSlice;
        private static int savedBufferSize;

        /// <summary>
        /// 로딩 가속을 켠다. 이미 켜져 있으면 아무것도 하지 않는다.
        /// </summary>
        public static void Begin()
        {
            if (isActive)
                return;
            isActive = true;

            savedPriority = Application.backgroundLoadingPriority;
            savedTimeSlice = QualitySettings.asyncUploadTimeSlice;
            savedBufferSize = QualitySettings.asyncUploadBufferSize;

            Application.backgroundLoadingPriority = ThreadPriority.High;
            QualitySettings.asyncUploadTimeSlice = Mathf.Max(savedTimeSlice, BoostedUploadTimeSliceMs);
            QualitySettings.asyncUploadBufferSize = Mathf.Max(savedBufferSize, BoostedUploadBufferMb);
        }

        /// <summary>
        /// 로딩 가속을 끄고 저장해 둔 값으로 되돌린다.
        /// </summary>
        public static void End()
        {
            if (!isActive)
                return;
            isActive = false;

            Application.backgroundLoadingPriority = savedPriority;
            QualitySettings.asyncUploadTimeSlice = savedTimeSlice;
            QualitySettings.asyncUploadBufferSize = savedBufferSize;
        }
    }
}
