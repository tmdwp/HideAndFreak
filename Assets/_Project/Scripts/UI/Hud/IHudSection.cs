using GuardVsMannequin.Core;

namespace GuardVsMannequin.UI
{
    /// <summary>
    /// HUD의 한 영역(상단, 하단, 결과 등). <see cref="GameHud"/>가 같은 캔버스 아래의 영역을 모두 찾아 매 프레임 갱신한다.
    /// 새 영역은 이 인터페이스를 구현한 컴포넌트를 캔버스에 붙이기만 하면 된다.
    /// </summary>
    public interface IHudSection
    {
        /// <summary>처음 한 번, 공용 정보를 받는다. (버튼 연결 등)</summary>
        void Bind(HudContext context);

        /// <summary>영역을 숨긴다. (네트워크 준비 전)</summary>
        void Hide();

        /// <summary>현재 게임 상태에 맞게 보이기/숨기기와 내용을 갱신한다.</summary>
        void Refresh(GameState state);
    }
}
