using GuardVsMannequin.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// 현재 게임 버전(<see cref="GameVersion.Current"/>: 빌드 버전 + 네트워크 규약 번호 + 조정 파일 지문)을 표시한다.
    /// 다른 PC와 접속이 안 될 때 버전이 같은지 바로 비교할 수 있게 로비 왼쪽 아래에 둔다.
    /// </summary>
    public class VersionLabel : MonoBehaviour
    {
        [Tooltip("버전을 쓸 텍스트.")]
        [SerializeField] private Text versionText;

        /// <summary>
        /// 시작할 때 버전 문자열을 채운다. (실행 중에는 바뀌지 않음)
        /// </summary>
        private void Start()
        {
            if (versionText != null)
                versionText.text = "v" + GameVersion.Current;
        }
    }
}
